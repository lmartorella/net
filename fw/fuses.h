#ifndef _FUSES_INCLUDE_
#define _FUSES_INCLUDE_

#include <p18f87j60.h>
#include "utilities.h"

// ******* 
// DISPLAY
// ******* 
#define CM1602_PORT 		PORTE
#define CM1602_TRIS 		TRISE
#define CM1602_PORTBITS		PORTEbits
#define CM1602_IF_MODE 		4
#define CM1602_IF_NIBBLE 	'high'
#define CM1602_IF_BIT_RW 	PORTEbits.RE2
#define CM1602_IF_BIT_RS 	PORTEbits.RE0
#define CM1602_IF_BIT_EN 	PORTEbits.RE3
#define CM1602_LINE_COUNT 	2
#define CM1602_FONT_HEIGHT 	7

// ******* 
// MEMORY
// ******* 
#define MEM_PORT	 PORTC
#define MEM_PORTBITS PORTCbits
#define MEM_TRISBITS TRISCbits
#define MEM_BANK0_CS RC6
#define MEM_BANK1_CS RC2
#define MEM_BANK2_CS RC7
#define MEM_BANK3_CS RC1

// ******* 
// MP3
// ******* 
#define VS1011_PORT  	PORTB
#define VS1011_PORTBITS PORTBbits
#define VS1011_TRISBITS TRISBbits
#define VS1011_RESET 	RB4
#define VS1011_DREQ  	RB5
#define VS1011_SDATA 	RB2
#define VS1011_DCLK  	RB3
#define VS1011_BSYNC 	RB0
#define VS1011_CS	 	RB1


#endif
