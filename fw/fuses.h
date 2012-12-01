#ifndef _FUSES_INCLUDE_
#define _FUSES_INCLUDE_

#include <p18f87j60.h>

#define CM1602_IF_PORT 		PORTE
#define CM1602_IF_MODE 		4
#define CM1602_IF_NIBBLE 	'high'
#define CM1602_IF_BIT_RW 	PORTEbits.RE2
#define CM1602_IF_BIT_RS 	PORTEbits.RE0
#define CM1602_IF_BIT_EN 	PORTEbits.RE3
#define CM1602_LINE_COUNT 	2
#define CM1602_FONT_HEIGHT 	7

extern void wait2ms(void);
extern void wait30ms(void);
extern void wait40us(void);

#endif
