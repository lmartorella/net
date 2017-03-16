#include "../pch.h"
#include "rs485.h"
#include "tick.h"
#include "../appio.h"
#include "leds.h"

#ifdef HAS_RS485

#define EN_TRANSMIT 1
#define EN_RECEIVE 0

#define ETH_DEBUG_LINES

static enum {
    // No transmit, receive mode
    STATUS_RECEIVE,
    // Wait tick before transmit, channel still disengaged
    STATUS_WAIT_FOR_ENGAGE,
    // Wait tick before transmit, channel now engaged
    STATUS_WAIT_FOR_START_TRANSMIT,
    // Channel engaged, trasmitting
    STATUS_TRANSMIT,
    // Channel engaged, wait 1m ticks before freeing channel
    STATUS_LAST_TRANSMIT,
    // Channel engaged, wait 1m ticks before freeing channel
    STATUS_WAIT_FOR_TRANSMIT_END
} s_status;

#define ADJUST_PTR(x) while (x >= (s_buffer + RS485_BUF_SIZE)) x-= RS485_BUF_SIZE

static BYTE s_buffer[RS485_BUF_SIZE];
// Pointer of the writing head
static BYTE* s_writePtr;
// Pointer of the reading head (if = write ptr, no bytes avail)
static BYTE* s_readPtr;

// Status of address bit in the serie
bit rs485_lastRc9;
bit rs485_skipData;
// Send a special OVER token to the bus when the transmission ends (if there are 
// data in TX queue)
bit rs485_over;
// When rs485_over is set, close will determine with char to send
bit rs485_close;
// When set after a write operation, remain in TX state when data finishes until next write operation
bit rs485_master;

static bit s_ferr;
static bit s_lastrc9;
static bit s_oerr;

static TICK_TYPE s_lastTick;

// time to wait before engaging the channel (after other station finished to transmit)
#define ENGAGE_CHANNEL_TIMEOUT (TICK_TYPE)(TICKS_PER_BYTE * 1)  
// additional time to wait after channel engaged to start transmit
// Consider that the glitch produced engaging the channel can be observed as a FRAMEERR by other stations
// So use a long time here to avoid FERR to consume valid data
#define START_TRANSMIT_TIMEOUT (TICK_TYPE)(TICKS_PER_BYTE * 3)
// time to wait before releasing the channel = 2 bytes,
// but let's wait an additional byte since USART is free when still transmitting the last byte.
#define DISENGAGE_CHANNEL_TIMEOUT (TICK_TYPE)(TICKS_PER_BYTE * (2 + 1))

static void rs485_startRead();

void rs485_init()
{
    // Enable EUSART2 on PIC18f
    RS485_RCSTA.SPEN = 1;
    RS485_RCSTA.RX9 = 1;
    RS485_TXSTA.SYNC = 0;
    RS485_TXSTA.TX9 = 1;
    
    RS485_INIT_BAUD();
    
    // Enable ports
    RS485_TRIS_RX = 1;
    RS485_TRIS_TX = 0;
    
    // Enable control ports
    RS485_TRIS_EN = 0;
    RS485_PORT_EN = EN_RECEIVE;
    
#ifdef ETH_DEBUG_LINES
    TRISDbits.RD0 = 0;
#endif
    
    rs485_skipData = 0;
    rs485_over = 0;
    rs485_close = 0;
    s_oerr  = 0;
    s_writePtr = s_readPtr = s_buffer;
    s_lastTick = TickGet();
    
    s_status = STATUS_RECEIVE;
    rs485_startRead();
}

#define _rs485_readAvail() ((BYTE)(((BYTE)(s_writePtr - s_readPtr)) % RS485_BUF_SIZE))
#define _rs485_writeAvail() ((BYTE)(((BYTE)(s_readPtr - s_writePtr - 1)) % RS485_BUF_SIZE))

BYTE rs485_readAvail()
{
    if (s_status == STATUS_RECEIVE) {
        return _rs485_readAvail();
    }
    else {
        return 0;
    }
}

BYTE rs485_writeAvail()
{
    if (s_status != STATUS_RECEIVE) {
        return _rs485_writeAvail();
    }
    else {
        // Can switch to TX and have full buffer
        return RS485_BUF_SIZE - 1;
    }
}

// Feed more data, read at read pointer and then increase
// and re-enable interrupts now
#define writeByte() \
    RS485_TXREG = *(s_readPtr++); \
    ADJUST_PTR(s_readPtr); \
    RS485_PIE_TXIE = 1;

void rs485_interrupt()
{
    // Empty TX buffer. Check for more data
    if (RS485_PIR_TXIF && RS485_PIE_TXIE) {
        do {
            if (_rs485_readAvail() > 0) {
                // Feed more data, read at read pointer and then increase
                writeByte();
            }
            else if (rs485_master) {
                // Disable interrupt but remain in write mode
                RS485_PIE_TXIE = 0;
                break;
            }
            else if (rs485_over) {
                rs485_over = 0;
                // Send OVER byte
                RS485_TXSTA.TX9D = 1;
                RS485_TXREG = rs485_close ? RS485_CCHAR_CLOSE : RS485_CCHAR_OVER;
            } 
            else {
                // NO MORE data to transmit
                // TX2IF cannot be cleared, shut IE 
                RS485_PIE_TXIE = 0;
                // goto first phase of tx end
                s_status = STATUS_LAST_TRANSMIT;
#ifdef ETH_DEBUG_LINES
                PORTDbits.RD0 = 1;
#endif
                break;
            }
        } while (RS485_PIR_TXIF);
    }
    else if (RS485_PIR_RCIF && RS485_PIE_RCIE) {
        // Data received
        do {
            // Check for errors BEFORE reading RCREG
            if (RS485_RCSTA.OERR) {
                s_oerr = 1;
                return;
            }
            s_ferr = RS485_RCSTA.FERR;

            // read data to reset IF and FERR
            s_lastrc9 = RS485_RCSTA.RX9D;
            BYTE data = RS485_RCREG;
            
            // if s_ferr don't disengage read, only set the flag, in order to not lose next bytes
            // Only read data (0) if enabled
            if (!s_ferr && (s_lastrc9 || !rs485_skipData)) {
                rs485_lastRc9 = s_lastrc9;
                *(s_writePtr++) = data;
                ADJUST_PTR(s_writePtr);
            }
        } while (RS485_PIR_RCIF);
    }
}

/**
 * Poll as much as possible (internal timered)
 */
void rs485_poll()
{
    if (s_oerr) {
        fatal("U.OER");
    }
    
    TICK_TYPE elapsed = TickGet() - s_lastTick;
    switch (s_status){
        case STATUS_WAIT_FOR_ENGAGE:
            if (elapsed >= ENGAGE_CHANNEL_TIMEOUT) {
                // Engage
                s_status = STATUS_WAIT_FOR_START_TRANSMIT;
                // Enable RS485 driver
                RS485_PORT_EN = EN_TRANSMIT;
                s_lastTick = TickGet();
            }
            break;
        case STATUS_WAIT_FOR_START_TRANSMIT:
            if (elapsed >= START_TRANSMIT_TIMEOUT) {
                // Transmit
                s_status = STATUS_TRANSMIT;
                if (_rs485_readAvail() > 0) {
                    // Feed first byte
                    writeByte();
                } else {
                    // Enable interrupts now to eventually change state
                    RS485_PIE_TXIE = 1;
                }
            }
            break;
        case STATUS_LAST_TRANSMIT:
            s_lastTick = TickGet();
            s_status = STATUS_WAIT_FOR_TRANSMIT_END;
            break;
        case STATUS_WAIT_FOR_TRANSMIT_END:
            if (elapsed >= DISENGAGE_CHANNEL_TIMEOUT) {
#ifdef ETH_DEBUG_LINES
                PORTDbits.RD0 = 0;
#endif
                // Detach TX line
                s_status = STATUS_RECEIVE;
                rs485_startRead();
            }
            break;
    }
}

void rs485_write(BOOL address, const BYTE* data, BYTE size)
{ 
    rs485_over = rs485_close = rs485_master = 0;

    // Reset reader, if in progress
    if (s_status == STATUS_RECEIVE) {
        // Truncate reading
        RS485_RCSTA.CREN = 0;
        RS485_PIE_RCIE = 0;
        s_readPtr = s_writePtr = s_buffer;

        // Enable UART transmit. This will trigger the TXIF, but don't enable it now.
        RS485_TXSTA.TXEN = 1;

        s_status = STATUS_WAIT_FOR_ENGAGE;
        s_lastTick = TickGet();
    }

    // Disable interrupts
    RS485_PIE_TXIE = 0;

    if (size > _rs485_writeAvail()) {
        // Overflow error
        fatal("U.ov");
    }
    
    // Copy to buffer
    while (size > 0) {
        *(s_writePtr++) = *(data++);
        ADJUST_PTR(s_writePtr);
        size--;
    }

    // 9-bit address
    RS485_TXSTA.TX9D = address;

    // Schedule trasmitting, if not yet ready
    switch (s_status) {
        case STATUS_WAIT_FOR_TRANSMIT_END:
            // Re-convert it to tx
            s_status = STATUS_WAIT_FOR_START_TRANSMIT;
            s_lastTick = TickGet();
            break;
        case STATUS_TRANSMIT:
            // Was in transmit state: reenable TX feed interrupt
            RS485_PIE_TXIE = 1;
            break;
            
        //case STATUS_TRANSMIT:
        //case STATUS_WAIT_FOR_START_TRANSMIT:
        //case STATUS_WAIT_FOR_ENGAGE:
            // Already tx, ok
        //    break;
    }
}

static void rs485_startRead()
{
    if (s_status != STATUS_RECEIVE) {
        // Break all
        s_status = STATUS_WAIT_FOR_TRANSMIT_END;
        s_lastTick = TickGet();
        return;
    }
    
    // Disable writing (and reset OERR)
    RS485_TXSTA.TXEN = 0;
    RS485_RCSTA.CREN = 0;
    RS485_PIE_TXIE = 0;
    RS485_PIE_RCIE = 0;

    // Disable RS485 driver
    RS485_PORT_EN = EN_RECEIVE;
    s_status = STATUS_RECEIVE;

    // Reset circular buffer
    s_readPtr = s_writePtr = s_buffer;

    // Enable UART receiver
    RS485_PIE_RCIE = 1;
    RS485_RCSTA.CREN = 1;
}

RS485_STATE rs485_getState() {
    switch (s_status) {
        case STATUS_RECEIVE:
            return RS485_LINE_RX;
        case STATUS_WAIT_FOR_TRANSMIT_END:
            return RS485_LINE_TX_DISENGAGE;
        //case STATUS_TRANSMIT:
        //case STATUS_WAIT_FOR_ENGAGE:
        //case STATUS_WAIT_FOR_START_TRANSMIT:
        default:
            return RS485_LINE_TX_DATA;
    }
}

void rs485_waitDisengageTime() {
    if (s_status == STATUS_RECEIVE) { 
        // Disable rx receiver
        RS485_PIE_RCIE = 0;
        RS485_RCSTA.CREN = 0;
        
        s_status = STATUS_WAIT_FOR_TRANSMIT_END;
        s_lastTick = TickGet();
    }
}

bit rs485_read(BYTE* data, BYTE size)
{
    if (s_status != STATUS_RECEIVE) { 
        rs485_startRead();
        return FALSE;
    }
    else {
        static bit ret = FALSE;
        // Disable RX interrupts
        RS485_PIE_RCIE = 0;

        // Active? Read immediately.
        if (_rs485_readAvail() >= size) {
            ret = TRUE;
            while (size > 0) {
                *(data++) = *(s_readPtr++);
                ADJUST_PTR(s_readPtr);
                size--;
            }
        }

        // Re-enabled interrupts
        RS485_PIE_RCIE = 1;
        return ret;
    }   
}

#endif
