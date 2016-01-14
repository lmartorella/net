#include "../pch.h"
#include "rs485.h"

#ifdef HAS_RS485

#define RG0_TRANSMIT 1
#define RG0_RECEIVE 0

static BYTE* s_ptr;
static BYTE s_size = 0;

void rs485_init()
{
    // Enable EUSART2 on PIC18f
    RCSTA2bits.SPEN = 1;
    RCSTA2bits.RX9 = 1;
    TXSTA2bits.SYNC = 0;
    TXSTA2bits.TX9 = 1;
    
    // 19200 baud
    TXSTA2bits.BRGH = 1;
    BAUDCON2bits.BRG16 = 0;
    SPBRGH2 = 0;  
    SPBRG2 = 80;  // 25Mhz -> 19290
    
    // Enable ports
    TRISGbits.RG2 = 1;
    TRISGbits.RG1 = 0;
    
    // Enable control ports
    PORTGbits.RG0 = RG0_RECEIVE;
    TRISGbits.RG0 = 0;
    
    // Enable high priority interrupt on transmit
    IPR3bits.TX2IP = 1;
}

void rs485_interrupt()
{
    if (PIR3bits.TX2IF) {
        if (s_size > 0) {
            // Feed more data
            TXREG2 = *(++s_ptr);
            s_size--;
        }
        else {
            // TX2IF cannot be cleared
            PIE3bits.TX2IE = 0;
        }
    }
}

void rs485_poll()
{
    //if (TXSTA2bits.TRMT) {  // Empty TSR reg
        // Disable RS485 driver
        //PORTGbits.RG0 = RG0_RECEIVE;
        // Disable TX port
        //TXSTA2bits.TXEN = 0;
        //PIR3bits.TX2IF = 0;
    //}
}

void rs485_write(BOOL address, void* data, BYTE size)
{ 
    // Disable interrupts, change the data
    PIE3bits.TX2IE = 0;
    s_ptr = data;
    s_size = size - 1;

    // 9-bit address
    TXSTA2bits.TX9D = address;
    // Enable RS485 driver
    PORTGbits.RG0 = RG0_TRANSMIT;
    // Enable UART transmit
    TXSTA2bits.TXEN = 1;

    // Start transmitting
    TXREG2 = *s_ptr;
    PIE3bits.TX2IE = 1;
}

void rs485_read(void* data, WORD size)
{
}

#endif
