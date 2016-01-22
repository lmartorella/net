#include "../pch.h"
#include "rs485.h"
#include "../appio.h"

#ifdef HAS_RS485

#define EN_TRANSMIT 1
#define EN_RECEIVE 0

static enum {
    // No trasmit, no receive, channel free
    STATUS_IDLE,
    // Wait 2 1ms tick before transmit, channel engaged
    STATUS_WAIT_FOR_TRANSMIT1,
    // Wait 1ms tick before transmit, channel engaged
    STATUS_WAIT_FOR_TRANSMIT2,
    // Channel engaged, trasmitting
    STATUS_TRANSMIT,
    // Channel engaged, wait 2 1m ticks before freeing channel
    STATUS_WAIT_FOR_TRANSMIT_END1,
    // Channel engaged, wait 1m ticks before freeing channel
    STATUS_WAIT_FOR_TRANSMIT_END2,
    // Receive mode
    STATUS_RECEIVE,
    // ERROR in receive, overrun of PIC 2 bytes buffer
    STATUS_RECEIVE_OERR,
    // ERROR in receive, frame error
    STATUS_RECEIVE_FERR,
} s_status;

// Circular buffer of 64 bytes
#define BUFFER_SIZE 64
#define BUFFER_SIZE_MASK 0x3f

static BYTE s_buffer[BUFFER_SIZE];
// Pointer of the writing head
static BYTE* s_writePtr;
// Pointer of the reading head (if = write ptr, no bytes avail)
static BYTE* s_readPtr;

// Status of address bit in the serie
static BOOL s_rc9;

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
}

#define getBufferSize() ((s_writePtr - s_readPtr) & BUFFER_SIZE_MASK)
#define moduloPtr(ptr) while (ptr >= s_buffer + BUFFER_SIZE) ptr -= BUFFER_SIZE;

void rs485_interrupt()
{
    // Empty TX buffer. Check for more data
    if (RS485_PIR_TXIF) {
        do {
            if (getBufferSize() > 0) {
                // Feed more data, read at read pointer and then increase
                RS485_TXREG = *(s_readPtr++);
                moduloPtr(s_readPtr);
            }
            else {
                // NO MORE data to transmit
                // TX2IF cannot be cleared, shut IE 
                RS485_PIE_TXIE = 0;
                // goto first phase of tx end
                s_status = STATUS_WAIT_FOR_TRANSMIT_END1;
            }
        } while (RS485_PIR_TXIF);
    }
    else if (RS485_PIR_RCIF) {
        // Data received
        do {
            // Check for errors BEFORE reading RCREG
            if (RS485_RCSTA.OERR) {
                s_status = STATUS_RECEIVE_OERR;
                break;
            }
            if (RS485_RCSTA.FERR) {
                s_status = STATUS_RECEIVE_FERR;
                break;
            }
            // read data
            s_rc9 = RS485_RCSTA.RX9D;
            *(s_writePtr++) = RS485_RCREG;
            moduloPtr(s_writePtr);
            return;
        } while (RS485_PIR_RCIF);

        // Error, disable reading and interrupt
        RS485_RCSTA.CREN = 0;
        RS485_PIE_RCIE = 0;
    }
}

// Poll at 1ms
void rs485_poll()
{
    switch (s_status){
        case STATUS_WAIT_FOR_TRANSMIT1:
            // Wait another full slot
            s_status = STATUS_WAIT_FOR_TRANSMIT2;
            break;
        case STATUS_WAIT_FOR_TRANSMIT2:
            // Transmit
            s_status = STATUS_TRANSMIT;
            // Feed first byte
            RS485_TXREG = *(s_readPtr++);
            moduloPtr(s_readPtr);
            RS485_PIE_TXIE = 1;
            break;
        case STATUS_WAIT_FOR_TRANSMIT_END1:
            // Wait another full slot
            s_status = STATUS_WAIT_FOR_TRANSMIT_END2;
            break;
        case STATUS_WAIT_FOR_TRANSMIT_END2:
            s_status = STATUS_IDLE;
            // Detach TX line
            RS485_PORT_EN = EN_RECEIVE;
            break;
    }
}

void rs485_write(BOOL address, void* data, int size)
{ 
    int ov;
    
    // Truncate reading
    RS485_RCSTA.CREN = 0;
    RS485_PIE_RCIE = 0;

    // Disable interrupts
    RS485_PIE_TXIE = 0;

    if (size > getBufferSize()) {
        // Overflow error
        fatal("RS485.ov");
    }
    
    // Copy to buffer
    ov = (s_writePtr + size) - (s_buffer + BUFFER_SIZE - 1); 
    if (ov > 0) {
        // 2-part copy
        memcpy(s_writePtr, data, size - ov);
        memcpy(s_buffer, data + size - ov, ov);
        s_writePtr = s_buffer + ov - 1;
    }
    else {
        // 1-part copy
        memcpy(s_writePtr, data, size);
        s_writePtr += size;
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
        case STATUS_WAIT_FOR_TRANSMIT1:
        case STATUS_WAIT_FOR_TRANSMIT2:
            // Already tx, ok
            break;
        case STATUS_WAIT_FOR_TRANSMIT_END1:
        case STATUS_WAIT_FOR_TRANSMIT_END2:
            // Convert it to tx
            s_status = STATUS_WAIT_FOR_TRANSMIT2;
            break;
        default:
            // Do all the cycles
            s_status = STATUS_WAIT_FOR_TRANSMIT1;
    }
}

void rs485_startRead()
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

RS485_ERR rs485_getError() {
    switch (s_status) {
        case STATUS_RECEIVE_OERR:
            return RS485_OVERRUN_ERR;
        case STATUS_RECEIVE_FERR:
            return RS485_FRAME_ERR;
        default:
            return RS485_NO_ERR;
    }
}

BOOL rs485_read(void* data, int size, BOOL* rc9)
{
    BOOL ret = FALSE;
            
    // Disable RX interrupts
    RS485_PIE_RCIE = 0;
    
    if (s_status == STATUS_RECEIVE) {
        *rc9 = s_rc9;
        if (getBufferSize() >= size) {        
            // Copy to buffer
            int ov = (s_readPtr + size) - (s_buffer + BUFFER_SIZE - 1); 
            if (ov > 0) {
                // 2-part copy
                memcpy(data, s_readPtr, size - ov);
                memcpy(data + size - ov, s_buffer, ov);
                s_readPtr = s_buffer + ov - 1;
            }
            else {
                // 1-part copy
                memcpy(data, s_readPtr, size);
                s_readPtr += size;
            }
        }
    }
    
    // Re-enabled interrupts
    RS485_PIE_RCIE = 1;
    return ret;
}

#endif
