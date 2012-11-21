#ifndef _FUSES_INCLUDE_
#define _FUSES_INCLUDE_

#include <p18f87j60.h>

#define CM1602_IF_PORT PORTA
#define CM1602_IF_MODE 4
#define CM1602_IF_NIBBLE 'low'
#define CM1602_IF_BIT_RW PORTBbits.RB0
#define CM1602_IF_BIT_RS PORTBbits.RB1
#define CM1602_IF_BIT_EN PORTBbits.RB2
#define CM1602_LINE_COUNT 2
#define CM1602_FONT_HEIGHT 7

void waitms(unsigned char);
void waitus(unsigned char);

#endif
