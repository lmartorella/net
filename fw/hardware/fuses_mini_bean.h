#ifndef _FUSES_INCLUDE_
#define _FUSES_INCLUDE_

//#include <pic16f628a.h>

#define SYSTEM_CLOCK 4000000
#define PERIPHERAL_CLOCK (SYSTEM_CLOCK/4)

#undef HAS_CM1602
#undef HAS_VS1011
#undef HAS_SPI
#undef HAS_SPI_RAM
#undef HAS_IP
#define HAS_IO

// ******
// RS485: use USART2 on 18F87J60 (PORTG)
// ******
#define HAS_RS485
#define RS485_RCSTA RCSTAbits
#define RS485_TXSTA TXSTAbits
#define RS485_TXREG TXREG
#define RS485_RCREG RCREG
#define RS485_BAUDCON BAUDCONbits
#define RS485_SPBRGH SPBRGH
#define RS485_SPBRG SPBRG
#define RS485_IPR IPR3bits
#define RS485_PIR PIR3bits
#define RS485_PIE PIE3bits
#define RS485_TRIS_TX TRISBbits.RB2
#define RS485_TRIS_RX TRISBbits.RB1
#define RS485_TRIS_EN TRISBbits.RB3
#define RS485_PORT_EN PORTBbits.RB3


#endif


