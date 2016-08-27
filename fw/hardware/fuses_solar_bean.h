#ifndef _FUSES_INCLUDE_
#define _FUSES_INCLUDE_

#define SYSTEM_CLOCK 4000000ul
#define _XTAL_FREQ SYSTEM_CLOCK
#define PRIO_TYPE

#undef HAS_CM1602
#undef HAS_VS1011
#undef HAS_SPI
#undef HAS_SPI_RAM
#undef HAS_IP
#undef HAS_IO
#undef HAS_LED
#undef HAS_DHT11
#undef HAS_DIGIO

#define HAS_MAX232_SOFTWARE
#define RS232_RX_TRIS TRISBbits.TRISB1
#define RS232_TX_TRIS TRISBbits.TRISB0
#define RS232_RX_PORT PORTBbits.RB1
#define RS232_TX_PORT PORTBbits.RB0
// Timer for SW RS232: TMR1
// Timer on, internal timer, 1:256 prescalar
// (!T0CS | !PSA , PS2:PS0)
#define RS232_TCON T1CON
#define RS232_TCON_ON 0x01
#define RS232_TCON_OFF 0x00
#define RS232_TCON_HREG TMR1H
#define RS232_TCON_LREG TMR1L
#define RS232_TCON_IF PIR1bits.TMR1IF
#define RS232_TCON_HVALUE 0xff
#define RS232_TCON_LVALUE (0xff - ((SYSTEM_CLOCK/4)/9600))  // 104
#define RS232_TCON_HVALUE_HALF 0xff
#define RS232_TCON_LVALUE_HALF (0xff - ((SYSTEM_CLOCK/4)/9600/2))  // 52
#define RS232_TCON_HVALUE_TIMEOUT (0xff - ((SYSTEM_CLOCK/4)/20/256))  // 0.05s, high
#define RS232_TCON_LVALUE_TIMEOUT 0

// ******
// RS485: use USART1 on 16F628 (PORTB)
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
#define RS485_BAUD 19200
#define RS485_INIT_BAUD() \
    TXSTAbits.BRGH = 1;\
    SPBRG = 12

// *****
// Tick timer source. Uses TMR0 (8-bit prescales to 1:256), that resolve from 0.25ms to 16.7secs
// *****
#define TICK_TMR TMR0
#define TICK_TCON OPTION_REG

// Timer on, internal timer, 1:256 prescalar
// (!T0CS | !PSA , PS2:PS0)
#define TICK_TCON_1DATA (0x07)
#define TICK_TCON_0DATA (0x28)

#define TICK_INTCON_IF INTCONbits.T0IF
#define TICK_INTCON_IE INTCONbits.T0IE
#define TICK_CLOCK_BASE (SYSTEM_CLOCK / 4)
#define TICK_PRESCALER 256
#define TICK_TYPE WORD

// Reset the device with fatal error
extern persistent BYTE g_exceptionPtr;
#define fatal(msg) { g_exceptionPtr = (BYTE)msg; RESET(); }


#endif




