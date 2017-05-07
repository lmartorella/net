#include "../pch.h"
#include "rs485.h"
#include "tick.h"
#include "../appio.h"
#include "leds.h"

#ifdef HAS_RS485

#define EN_TRANSMIT 1
#define EN_RECEIVE 0

#undef ETH_DEBUG_LINES

RS485_STATE rs485_state;

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
static bit s_lastTx;

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
    s_lastTx = 0;
    s_writePtr = s_readPtr = s_buffer;
    s_lastTick = TickGet();
    
    rs485_state = RS485_LINE_RX;
    rs485_startRead();
}

#define _rs485_readAvail() ((BYTE)(((BYTE)(s_writePtr - s_readPtr)) % RS485_BUF_SIZE))
#define _rs485_writeAvail() ((BYTE)(((BYTE)(s_readPtr - s_writePtr - 1)) % RS485_BUF_SIZE))

BYTE rs485_readAvail()
{
    if (rs485_state == RS485_LINE_RX) {
        return _rs485_readAvail();
    }
    else {
        return 0;
    }
}

BYTE rs485_writeAvail()
{
    if (rs485_state != RS485_LINE_RX) {
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
            CLRWDT();
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
                s_lastTx = 1;
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
            CLRWDT();
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
    CLRWDT();
    if (s_oerr) {
        fatal("U.OER");
    }
    
    TICK_TYPE elapsed = TickGet() - s_lastTick;
    switch (rs485_state){
        case RS485_LINE_TX:
            if (s_lastTx) {
                s_lastTick = TickGet();
                rs485_state = RS485_LINE_TX_DISENGAGE;
                s_lastTx = 0;
            }
            break;
        case RS485_LINE_WAIT_FOR_ENGAGE:
            if (elapsed >= ENGAGE_CHANNEL_TIMEOUT) {
                // Engage
                rs485_state = RS485_LINE_WAIT_FOR_START_TRANSMIT;
                // Enable RS485 driver
                RS485_PORT_EN = EN_TRANSMIT;
                s_lastTick = TickGet();
            }
            break;
        case RS485_LINE_WAIT_FOR_START_TRANSMIT:
            if (elapsed >= START_TRANSMIT_TIMEOUT) {
                // Transmit
                rs485_state = RS485_LINE_TX;
                s_lastTx = 0;
                if (_rs485_readAvail() > 0) {
                    // Feed first byte
                    writeByte();
                } else {
                    // Enable interrupts now to eventually change state
                    RS485_PIE_TXIE = 1;
                }
            }
            break;
        case RS485_LINE_TX_DISENGAGE:
            if (elapsed >= DISENGAGE_CHANNEL_TIMEOUT) {
#ifdef ETH_DEBUG_LINES
                PORTDbits.RD0 = 0;
#endif
                // Detach TX line
                rs485_state = RS485_LINE_RX;
                rs485_startRead();
            }
            break;
    }
}

void rs485_write(BOOL address, const BYTE* data, BYTE size)
{ 
    rs485_over = rs485_close = rs485_master = 0;

    // Reset reader, if in progress
    if (rs485_state == RS485_LINE_RX) {
        // Truncate reading
        RS485_RCSTA.CREN = 0;
        RS485_PIE_RCIE = 0;
        s_readPtr = s_writePtr = s_buffer;

        // Enable UART transmit. This will trigger the TXIF, but don't enable it now.
        RS485_TXSTA.TXEN = 1;

        rs485_state = RS485_LINE_WAIT_FOR_ENGAGE;
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
        CLRWDT();
    }

    // 9-bit address
    RS485_TXSTA.TX9D = address;

    // Schedule trasmitting, if not yet ready
    switch (rs485_state) {
        case RS485_LINE_TX_DISENGAGE:
            // Re-convert it to tx
            rs485_state = RS485_LINE_WAIT_FOR_START_TRANSMIT;
            s_lastTick = TickGet();
            break;
        case RS485_LINE_TX:
            // Was in transmit state: reenable TX feed interrupt
            RS485_PIE_TXIE = 1;
            break;
            
        //case RS485_LINE_TX:
        //case RS485_LINE_WAIT_FOR_START_TRANSMIT:
        //case RS485_LINE_WAIT_FOR_ENGAGE:
            // Already tx, ok
        //    break;
    }
}

static void rs485_startRead()
{
    CLRWDT();
    if (rs485_state != RS485_LINE_RX) {
        // Break all
        rs485_state = RS485_LINE_TX_DISENGAGE;
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
    rs485_state = RS485_LINE_RX;

    // Reset circular buffer
    s_readPtr = s_writePtr = s_buffer;

    // Enable UART receiver
    RS485_PIE_RCIE = 1;
    RS485_RCSTA.CREN = 1;
}

void rs485_waitDisengageTime() {
    if (rs485_state == RS485_LINE_RX) { 
        // Disable rx receiver
        RS485_PIE_RCIE = 0;
        RS485_RCSTA.CREN = 0;
        
        rs485_state = RS485_LINE_TX_DISENGAGE;
        s_lastTick = TickGet();
    }
}

bit rs485_read(BYTE* data, BYTE size)
{
    if (rs485_state != RS485_LINE_RX) { 
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
                CLRWDT();
            }
        }

        // Re-enabled interrupts
        RS485_PIE_RCIE = 1;
        return ret;
    }   
}

#endif
