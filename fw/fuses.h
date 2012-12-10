#ifndef _FUSES_INCLUDE_
#define _FUSES_INCLUDE_

#include <p18f87j60.h>
#include "utilities.h"

// ******* 
// DISPLAY
// ******* 
#define CM1602_PORT 		E
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
#define MEM_PORT	 C
#define MEM_BANK0_CS RC6
#define MEM_BANK1_CS RC2
#define MEM_BANK2_CS RC7
#define MEM_BANK3_CS RC1

// ******* 
// MP3
// ******* 
#define VS1011_PORT  B
#define VS1011_RESET RB4
#define VS1011_DREQ  RB5
#define VS1011_SDATA RB2
#define VS1011_DCLK  RB3
#define VS1011_BSYNC RB0
#define VS1011_CS	 RB1


#endif
