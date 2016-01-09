#include "rs485.h"

#ifdef HAS_RS485

// TODO: In progress
void rs485_reset()
{
    // Enable EUSART2
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
    PORTGbits.RG0 = 1;
    PORTGbits.RG3 = 1;
    TRISGbits.RG0 = 0;
    TRISGbits.RG3 = 0;
}

#endif
