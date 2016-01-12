#include "../pch.h"
#include "rs485.h"

#ifdef HAS_RS485

#define RG0_TRANSMIT 1
#define RG0_RECEIVE 0

static char s_toSend = 0;

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
    
    // Enable high priority interrupt on transmit
    //IPR3bits.TX2IP = 1;
}

void rs485_interrupt()
{
    //if (PIR3bits.TX2IF) {
        // Disable RS485 driver
        PORTGbits.RG0 = RG0_RECEIVE;
        // Disable TX port
        TXSTA2bits.TXEN = 0;
        PIR3bits.TX2IF = 0;
    //}
}

void rs485_poll()
{
    if (TXSTA2bits.TRMT) {  // Empty TSR reg
        // Disable RS485 driver
        PORTGbits.RG0 = RG0_RECEIVE;
        // Disable TX port
        //TXSTA2bits.TXEN = 0;
        //PIR3bits.TX2IF = 0;
    }
}

void rs485_write(void* data, WORD size)
{
    // Disable interrupt
    //PIE3bits.TX2IE = 0;

    // Enable RS485 driver
    PORTGbits.RG0 = RG0_TRANSMIT;
    // Enable UART transmit
    TXSTA2bits.TXEN = 1;
    // Transmit U
    TXREG2 = s_toSend + '0';
    s_toSend = (s_toSend + 1) % 10;
    
    // Now interrupt is set
    //PIR3bits.TX2IF = 0;
    // Reenable interrupt
    //PIE3bits.TX2IE = 1;
    // Transmit dummy
    //TXREG2 = 0x81;
    // This will raise a real interrupt at the end of the first byte
}

void rs485_read(void* data, WORD size)
{
}

#endif
