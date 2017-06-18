#ifndef FUSES_MICRO_BEAN_H
#define	FUSES_MICRO_BEAN_H

#define SYSTEM_CLOCK 4000000ul
#define _XTAL_FREQ SYSTEM_CLOCK
#define PRIO_TYPE

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

#define HAS_LED
#define LED_PORTBIT PORTAbits.RA7
#define LED_TRISBIT TRISAbits.TRISA7


#endif	/* FUSES_MICRO_BEAN_H */

