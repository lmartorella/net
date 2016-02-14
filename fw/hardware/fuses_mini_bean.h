#ifndef _FUSES_INCLUDE_
#define _FUSES_INCLUDE_

#define SYSTEM_CLOCK 4000000ull
#define PRIO_TYPE

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
#define RS485_IPR IPR3bits
#define RS485_PIR_TXIF PIR1bits.TXIF
#define RS485_PIR_RCIF PIR1bits.RCIF
#define RS485_PIE_TXIE PIE1bits.TXIE
#define RS485_PIE_RCIE PIE1bits.RCIE
#define RS485_TRIS_TX TRISBbits.TRISB2
#define RS485_TRIS_RX TRISBbits.TRISB1
#define RS485_TRIS_EN TRISBbits.TRISB3
#define RS485_PORT_EN PORTBbits.RB3
// 19200 baud
#define RS485_INIT_19K_BAUD() \
    TXSTAbits.BRGH = 1;\
    SPBRG = 12

// *****
// Tick timer source. Uses TMR1 (16-bit but prescales to 1:8)
// *****
#define TICK_TMRH TMR1H
#define TICK_TMRL TMR1L
#define TICK_TCON T1CON
// Timer on, internal timer, 1:8 prescalar
// (T1CKPS1 | T1CKPS0 | TMR1ON)
#define TICK_TCON_DATA (0x31)
#define TICK_INTCON_IF PIR1bits.TMR1IF
#define TICK_INTCON_IE PIE1bits.TMR1IE
#define TICK_CLOCK_BASE (SYSTEM_CLOCK/4ull)
#define TICK_PRESCALER 8

#endif


