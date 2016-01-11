#include "../pch.h"
#include "rs485.h"

#ifdef HAS_RS485

#define RG0_TRANSMIT 1
#define RG0_RECEIVE 0

void rs485_init()
{
    // Enable EUSART2 on PIC18f
    RCSTA2bits.SPEN = 1;
    RCSTA2bits.RX9 = 0;
    TXSTA2bits.SYNC = 0;
    
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
}

void rs485_write(void* data, WORD size)
{
    PORTGbits.RG0 = RG0_TRANSMIT;
    TXSTA2bits.TXEN = 1;
    TXREG2 = 0x55;
    // TODO clear transmit when finished
}

void rs485_read(void* data, WORD size)
{
}

#endif
