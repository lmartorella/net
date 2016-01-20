#include "../pch.h"
#include "rs485.h"

#ifdef HAS_RS485

#define EN_TRANSMIT 1
#define EN_RECEIVE 0

#define CLICKS_TO_

typedef enum {
    STATUS_IDLE,
    STATUS_WAIT_FOR_TRANSMIT,
    STATUS_TRANSMIT,
    STATUS_WAIT_FOR_TRANSMIT_END,
    STATUS_RECEIVE,
    STATUS_RECEIVE_OERR,
    STATUS_RECEIVE_FERR,
} RS485_STATUS;

static BYTE* s_ptr;
static BYTE s_size = 0;
static BYTE s_buffer[64];
static RS485_STATUS s_status;
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
}

void rs485_interrupt()
{
    // Empty TX buffer?
    if (RS485_PIR_TXIF && s_status != STATUS_RECEIVE) {
        if (s_size > 0) {
            // Feed more data
            RS485_TXREG = *(++s_ptr);
            s_size--;
        }
        else {
            // TX2IF cannot be cleared
            RS485_PIE_TXIE = 0;
            s_status = STATUS_WAIT_FOR_TRANSMIT_END;
        }
    }
    else if (s_status == STATUS_RECEIVE && RS485_PIR_RCIF) {
        while (RS485_PIR_RCIF) {
            if (RS485_RCSTA.OERR) {
                s_status = STATUS_RECEIVE_OERR;
                break;
            }
            if (RS485_RCSTA.FERR) {
                s_status = STATUS_RECEIVE_FERR;
                break;
            }
            s_rc9 = RS485_RCSTA.RX9D;
            *(s_ptr++) = RS485_RCREG;
            s_size++;
            return;
        }
        RS485_RCSTA.CREN = 0;
        RS485_PIE_RCIE = 0;
    }
}

// Poll at 10ms
void rs485_poll()
{
    switch (s_status){
        case STATUS_WAIT_FOR_TRANSMIT:
            s_status = STATUS_TRANSMIT;
            // Start transmitting
            RS485_TXREG = *s_ptr;
            RS485_PIE_TXIE = 1;
            break;
        case STATUS_WAIT_FOR_TRANSMIT_END:
            // Detach TX line
            RS485_PORT_EN = EN_RECEIVE;
            break;
    }
}

void rs485_write(BOOL address, void* data, int size)
{ 
    RS485_RCSTA.CREN = 0;
    RS485_TXSTA.TXEN = 0;
    RS485_PIE_TXIE = 0;
    RS485_PIE_RCIE = 0;

    memcpy(s_buffer, data, size);
    
    // Disable interrupts, change the data
    RS485_PIE_TXIE = 0;
    s_ptr = s_buffer;
    s_size = size - 1;

    // 9-bit address
    RS485_TXSTA.TX9D = address;
    // Enable RS485 driver
    RS485_PORT_EN = EN_TRANSMIT;
    // Enable UART transmit
    RS485_TXSTA.TXEN = 1;
    
    // Schedule trasmitting
    s_status = STATUS_WAIT_FOR_TRANSMIT;
}

void rs485_startRead()
{
    RS485_TXSTA.TXEN = 0;
    RS485_RCSTA.CREN = 0;
    RS485_PIE_TXIE = 0;
    RS485_PIE_RCIE = 0;

    // Disable RS485 driver
    RS485_PORT_EN = EN_RECEIVE;
    s_status = STATUS_RECEIVE;
    s_ptr = s_buffer;
    s_size = 0;

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

BYTE* rs485_read(int* size, BOOL* rc9)
{
    // Disable RX interrupts
    RS485_PIE_RCIE = 0;
    if (s_status == STATUS_RECEIVE) {
        *rc9 = s_rc9;
        *size = s_size;
        s_size = 0;
        s_ptr = s_buffer;
    }
    else {
        *size = 0;        
    }
    // Re-enabled interrupts
    RS485_PIE_RCIE = 1;
    return (*size > 0) ? s_buffer : NULL;
}

#endif
