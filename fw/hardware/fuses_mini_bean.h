#ifndef _FUSES_INCLUDE_
#define _FUSES_INCLUDE_

#define SYSTEM_CLOCK 4000000ul
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
// Tick timer source. Uses TMR0 (8-bit prescales to 1:256), that resolve from 0.25ms to 16.7secs
// *****
#define TICK_TMR TMR0
#define TICK_TCON INTCON

// Timer on, internal timer, 1:256 prescalar
// (!T0CS | !PSA , PS2:PS0)
#define TICK_TCON_1DATA (0x07)
#define TICK_TCON_0DATA (0x28)

#define TICK_INTCON_IF INTCONbits.T0IF
#define TICK_INTCON_IE INTCONbits.T0IE
#define TICK_CLOCK_BASE (SYSTEM_CLOCK / 4)
#define TICK_PRESCALER 256

#define TICK_TYPE WORD

#define SHORT_FATAL

#define HAS_LED
#define LED_PORTBIT PORTBbits.RB0
#define LED_TRISBIT TRISBbits.TRISB0

#endif


