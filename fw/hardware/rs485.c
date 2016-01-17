#include "../pch.h"
#include "rs485.h"

#ifdef HAS_RS485

#define EN_TRANSMIT 1
#define EN_RECEIVE 0

typedef enum {
    STATUS_IDLE,
    STATUS_WAIT_FOR_TRANSMIT1,
    STATUS_WAIT_FOR_TRANSMIT2,
    STATUS_TRANSMIT,
    STATUS_WAIT_FOR_TRANSMIT_END1,
    STATUS_WAIT_FOR_TRANSMIT_END2,
    STATUS_RECEIVE,
    STATUS_RECEIVE_ERROR,
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
    
    // 19200 baud
    RS485_TXSTA.BRGH = 1;
    RS485_BAUDCON.BRG16 = 0;
    RS485_SPBRGH = 0;  
    RS485_SPBRG = 80;  // 25Mhz -> 19290
    
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
    if (RS485_PIR.TX2IF && s_status != STATUS_RECEIVE) {
        if (s_size > 0) {
            // Feed more data
            RS485_TXREG = *(++s_ptr);
            s_size--;
        }
        else {
            // TX2IF cannot be cleared
            RS485_PIE.TX2IE = 0;
            s_status = STATUS_WAIT_FOR_TRANSMIT_END1;
        }
    }
    else if (RS485_PIR.RC2IF) {
        if (RS485_RCSTA.OERR || RS485_RCSTA.FERR) {
            s_status = STATUS_RECEIVE_ERROR;
            RS485_RCSTA.CREN = 0;
        }
        else {
            *(++s_ptr) = RS485_RCREG;
            s_rc9 = RS485_RCSTA.RX9D;
            s_size++;
        }
    }
}

// Poll at ~23KHz 
void rs485_poll()
{
    switch (s_status){
        case STATUS_WAIT_FOR_TRANSMIT1:
            s_status = STATUS_WAIT_FOR_TRANSMIT2;
            break;
        case STATUS_WAIT_FOR_TRANSMIT2:
            s_status = STATUS_TRANSMIT;
            // Start transmitting
            RS485_TXREG = *s_ptr;
            RS485_PIE.TX2IE = 1;
            break;
        case STATUS_WAIT_FOR_TRANSMIT_END1:
            s_status = STATUS_WAIT_FOR_TRANSMIT_END2;
            break;
        case STATUS_WAIT_FOR_TRANSMIT_END2:
            // Detach TX line
            RS485_PORT_EN = EN_RECEIVE;
            break;
    }
}

void rs485_write(BOOL address, void* data, BYTE size)
{ 
    RS485_RCSTA.CREN = 0;
    
    memcpy(s_buffer, data, size);
    
    // Disable interrupts, change the data
    RS485_PIE.TX2IE = 0;
    s_ptr = s_buffer;
    s_size = size - 1;

    // 9-bit address
    RS485_TXSTA.TX9D = address;
    // Enable RS485 driver
    RS485_PORT_EN = EN_TRANSMIT;
    // Enable UART transmit
    RS485_TXSTA.TXEN = 1;
    
    // Schedule trasmitting
    s_status = STATUS_WAIT_FOR_TRANSMIT1;
}

BYTE rs485_dataAvail(BOOL* rc9)
{
    *rc9 = s_rc9;
    return (s_status == STATUS_RECEIVE) ? s_size : 0;
}

void rs485_startRead()
{
    RS485_TXSTA.TXEN = 0;

    // Disable RS485 driver
    RS485_PORT_EN = EN_RECEIVE;
    s_status = STATUS_RECEIVE;
    s_ptr = s_buffer;
    s_size = 0;

    // Enable UART receiver
    RS485_PIE.TX2IE = 1;
    RS485_RCSTA.CREN = 1;
    RS485_PIE.RC2IE = 0;
}

BYTE* rs485_readBuffer()
{
    return s_buffer;
}

#endif
