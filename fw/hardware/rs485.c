#include "../pch.h"
#include "rs485.h"
#include "tick.h"
#include "../appio.h"
#include "leds.h"

#ifdef HAS_RS485

#define EN_TRANSMIT 1
#define EN_RECEIVE 0

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
    STATUS_WAIT_FOR_TRANSMIT_END,
    // ERROR in receive, frame error
    STATUS_RECEIVE_FERR,
} s_status;

// Circular buffer of 32 (0x20) bytes
#define BUFFER_SIZE 32

#define ADJUST_PTR(x) while (x >= (s_buffer + BUFFER_SIZE)) x-= BUFFER_SIZE

static BYTE s_buffer[BUFFER_SIZE];
// Pointer of the writing head
static BYTE* s_writePtr;
// Pointer of the reading head (if = write ptr, no bytes avail)
static BYTE* s_readPtr;

// Status of address bit in the serie
bit rs485_lastRc9;
bit rs485_skipData;
static bit s_ferr;

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
      
    rs485_skipData = FALSE;
    s_writePtr = s_readPtr = s_buffer;
    s_lastTick = TickGet();
    
    s_status = STATUS_RECEIVE;
    rs485_startRead();
}

static BYTE _rs485_readAvail()
{
    return (BYTE)(((BYTE)(s_writePtr - s_readPtr)) % BUFFER_SIZE);
}

static BYTE _rs485_writeAvail()
{
    return (BYTE)(((BYTE)(s_readPtr - s_writePtr - 1)) % BUFFER_SIZE);
}

BYTE rs485_readAvail()
{
    if (s_status == STATUS_RECEIVE) {
        return (BYTE)(((BYTE)(s_writePtr - s_readPtr)) % BUFFER_SIZE);
    }
    else {
        return 0;
    }
}

BYTE rs485_writeAvail()
{
    if (s_status == STATUS_TRANSMIT) {
        return (BYTE)(((BYTE)(s_readPtr - s_writePtr - 1)) % BUFFER_SIZE);
    }
    else {
        return 0;
    }
}

static void writeByte()
{   
    // Feed more data, read at read pointer and then increase
    RS485_TXREG = *(s_readPtr++);
    ADJUST_PTR(s_readPtr);
}

void rs485_interrupt()
{
    // Empty TX buffer. Check for more data
    if (RS485_PIR_TXIF && RS485_PIE_TXIE) {
        do {
            if (_rs485_readAvail() > 0) {
                // Feed more data, read at read pointer and then increase
                writeByte();
            }
            else {
                // NO MORE data to transmit
                // TX2IF cannot be cleared, shut IE 
                RS485_PIE_TXIE = 0;
                // goto first phase of tx end
                s_status = STATUS_WAIT_FOR_TRANSMIT_END;
                s_lastTick = TickGet();
                break;
            }
        } while (RS485_PIR_TXIF);
    }
    else if (RS485_PIR_RCIF && RS485_PIE_RCIE) {
        // Data received
        do {
            // Check for errors BEFORE reading RCREG
            if (RS485_RCSTA.OERR) {
                fatal("U.OER");
            }
            s_ferr = RS485_RCSTA.FERR;

            // read data to reset IF and FERR
            BOOL lastrc9 = RS485_RCSTA.RX9D;
            BYTE data = RS485_RCREG;
            
            if (s_ferr) {
                s_status = STATUS_RECEIVE_FERR;
                // Don't disengage read, only set the flag, in order to not lose next bytes
            }
            // Only read data (0) if enabled
            else if (lastrc9 || !rs485_skipData) {
                rs485_lastRc9 = lastrc9;
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
                // Feed first byte
                writeByte();
                // Enable interrupts now
                RS485_PIE_TXIE = 1;
            }
            break;
        case STATUS_WAIT_FOR_TRANSMIT_END:
            if (elapsed >= DISENGAGE_CHANNEL_TIMEOUT) {
                // Detach TX line
                s_status = STATUS_RECEIVE;
                rs485_startRead();
            }
            break;
    }
}

void rs485_write(BOOL address, const BYTE* data, BYTE size)
{ 
    // Reset reader, if in progress
    switch (s_status) {
        case STATUS_RECEIVE:
        case STATUS_RECEIVE_FERR:
            // Truncate reading
            RS485_RCSTA.CREN = 0;
            RS485_PIE_RCIE = 0;
            s_readPtr = s_writePtr = s_buffer;

            // Enable UART transmit. This will trigger the TXIF, but don't enable it now.
            RS485_TXSTA.TXEN = 1;

            s_status = STATUS_WAIT_FOR_ENGAGE;
            s_lastTick = TickGet();
            break;
    }

    BOOL ie = RS485_PIE_TXIE;
    
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

    // Re-enable if it was
    RS485_PIE_TXIE = ie;

    // Schedule trasmitting, if not yet ready
    switch (s_status) {
        case STATUS_WAIT_FOR_TRANSMIT_END:
            // Re-convert it to tx
            s_status = STATUS_WAIT_FOR_START_TRANSMIT;
            s_lastTick = TickGet();
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
    
    // Disable writing
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
        case STATUS_RECEIVE_FERR:
            return RS485_LINE_RX_FRAME_ERR;
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

RS485_STATE rs485_clearFerr() {
    if (s_status == STATUS_RECEIVE_FERR) {
        s_status = STATUS_RECEIVE;
    }
    return s_status;
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
