#include "../pch.h"
#include "rs485.h"
#include "tick.h"
#include "../appio.h"

#ifdef HAS_RS485

#define EN_TRANSMIT 1
#define EN_RECEIVE 0

static enum {
    // No trasmit, no receive, channel free
    STATUS_IDLE,
    // Wait 1ms tick before transmit, channel engaged
    STATUS_WAIT_FOR_TRANSMIT,
    // Channel engaged, trasmitting
    STATUS_TRANSMIT,
    // Channel engaged, wait 1m ticks before freeing channel
    STATUS_WAIT_FOR_TRANSMIT_END,
    // Receive mode
    STATUS_RECEIVE,
    // ERROR in receive, frame error
    STATUS_RECEIVE_FERR,
} s_status;

// Circular buffer of 64 bytes
#define BUFFER_SIZE 32

#define ADJUST_PTR(x) while (x >= (s_buffer + BUFFER_SIZE)) x-= BUFFER_SIZE

static BYTE s_buffer[BUFFER_SIZE];
// Pointer of the writing head
static BYTE* s_writePtr;
// Pointer of the reading head (if = write ptr, no bytes avail)
static BYTE* s_readPtr;

// Status of address bit in the serie
static BOOL s_rc9;

static TICK_TYPE s_lastTick;

// Transmit timeout should be greater than transmit end
// so at the end of transmit the master should have the time to
// switch to rx before slave start transmit
#define WAIT_FOR_TRANSMIT_TIMEOUT (TICKS_PER_SECOND / 500)  // 2ms
#define WAIT_FOR_TRANSMIT_END_TIMEOUT (TICKS_PER_SECOND / 1000)  // 1ms

void rs485_init()
{
    // Enable EUSART2 on PIC18f
    RS485_RCSTA.SPEN = 1;
    RS485_RCSTA.RX9 = 1;
    RS485_TXSTA.SYNC = 0;
    RS485_TXSTA.TX9 = 1;
    
    RS485_INIT_19K_BAUD();
    
    // Enable ports
    RS485_TRIS_RX = 1;
    RS485_TRIS_TX = 0;
    
    // Enable control ports
    RS485_PORT_EN = EN_RECEIVE;
    RS485_TRIS_EN = 0;
      
    s_status = STATUS_IDLE;
    s_writePtr = s_readPtr = s_buffer;
    s_lastTick = TickGet();
}

WORD rs485_readAvail()
{
    return (WORD)((s_writePtr - s_readPtr) % BUFFER_SIZE);
}

WORD rs485_writeAvail()
{
    return (WORD)((s_readPtr - s_writePtr - 1) % BUFFER_SIZE);
}

static void writeByte()
{
    // Feed more data, read at read pointer and then increase
    RS485_TXREG = *(s_readPtr++);
    ADJUST_PTR(s_readPtr);
}

static void readByte()
{
    // read data
    s_rc9 = RS485_RCSTA.RX9D;
    *(s_writePtr++) = RS485_RCREG;
    ADJUST_PTR(s_writePtr);
}

void rs485_interrupt()
{
    // Empty TX buffer. Check for more data
    if (RS485_PIR_TXIF && RS485_PIE_TXIE) {
        do {
            if (rs485_readAvail() > 0) {
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
                fatal("UART.OERR");
            }
            if (RS485_RCSTA.FERR) {
                s_status = STATUS_RECEIVE_FERR;
                goto error;
            }
            readByte();
        } while (RS485_PIR_RCIF);
        return;

        // Error, disable reading and interrupt
error:
        RS485_RCSTA.CREN = 0;
        RS485_PIE_RCIE = 0;
    }
}

/**
 * Poll as much as possible (internal timered)
 */
void rs485_poll()
{
    switch (s_status){
        case STATUS_WAIT_FOR_TRANSMIT:
            if (TickGet() > (TICK_TYPE)(s_lastTick + WAIT_FOR_TRANSMIT_TIMEOUT)) {
                // Transmit
                s_status = STATUS_TRANSMIT;
                // Feed first byte
                writeByte();
                RS485_PIE_TXIE = 1;
            }
            break;
        case STATUS_WAIT_FOR_TRANSMIT_END:
            if (TickGet() > (TICK_TYPE)(s_lastTick + WAIT_FOR_TRANSMIT_END_TIMEOUT)) {
                s_status = STATUS_IDLE;
                // Detach TX line
                RS485_PORT_EN = EN_RECEIVE;
            }
            break;
    }
}

void rs485_write(BOOL address, const BYTE* data, int size)
{ 
    // Truncate reading
    RS485_RCSTA.CREN = 0;
    RS485_PIE_RCIE = 0;

    // Disable interrupts
    RS485_PIE_TXIE = 0;

    if (size > rs485_writeAvail()) {
        // Overflow error
        fatal("RS485.ov");
    }
    
    // Copy to buffer
    while (size > 0) {
        *(s_writePtr++) = *(data++);
        ADJUST_PTR(s_writePtr);
        size--;
    }

    // 9-bit address
    RS485_TXSTA.TX9D = address;
    // Enable RS485 driver
    RS485_PORT_EN = EN_TRANSMIT;
    // Enable UART transmit
    RS485_TXSTA.TXEN = 1;
    
    // Schedule trasmitting, if not yet ready
    switch (s_status) {
        case STATUS_TRANSMIT:
        case STATUS_WAIT_FOR_TRANSMIT:
            // Already tx, ok
            break;
        case STATUS_WAIT_FOR_TRANSMIT_END:
        default:
            // Convert it to tx
            s_status = STATUS_WAIT_FOR_TRANSMIT;
            s_lastTick = TickGet();
            break;
    }
}

static void rs485_startRead()
{
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
            return RS485_FRAME_ERR;
        case STATUS_RECEIVE:
        case STATUS_IDLE:
            return RS485_LINE_RX;
        default:
            return RS485_LINE_TX;
    }
}

BOOL rs485_read(BYTE* data, int size, BOOL* rc9)
{
    BOOL ret = FALSE;

    if (s_status != STATUS_RECEIVE) { 
        rs485_startRead();
    }
    else {
        // Disable RX interrupts
        RS485_PIE_RCIE = 0;

        // Active? Read immediately.
        *rc9 = s_rc9;
        if (rs485_readAvail() >= size) {
            ret = TRUE;
            while (size > 0) {
                *(data++) = *(s_readPtr++);
                ADJUST_PTR(s_readPtr);
                size--;
            }
        }

        // Re-enabled interrupts
        RS485_PIE_RCIE = 1;
    }   
          
    return ret;
}

#endif
