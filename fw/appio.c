#include "pch.h"
#include "appio.h"
#include "hardware/cm1602.h"

// The pointer is pointing to ROM space that will not be reset
// otherwise after the RESET the variable content can be lost.
persistent const char* s_lastErr;

#ifdef HAS_CM1602
static void _clr(BYTE addr)
{
	char i;
	cm1602_setDdramAddr(addr);
	for (i = 0; i < 16; i++)
	{
		cm1602_write(' ');
	}
	cm1602_setDdramAddr(addr);
}
#endif

void clearln()
{
#ifdef HAS_CM1602
	_clr(0x40);
#endif
}

void clearlnUp()
{
#ifdef HAS_CM1602
	_clr(0x00);
#endif
}

#ifdef HAS_CM1602
static void _print(const char* str, BYTE addr)
{
	_clr(addr);
	cm1602_writeStr(str);
	CLRWDT();
}
#endif

void println(const char* str)
{
#ifdef HAS_CM1602
	_print(str, 0x40);	
#endif
}

void printlnUp(const char* str)
{
#ifdef HAS_CM1602
	_print(str, 0x00);	
#endif
}

void printch(char ch)
{
#ifdef HAS_CM1602
    cm1602_write(ch);
#endif
}

#ifndef SHORT_FATAL
// Long (callable) version of fatal
void fatal(const char* str)
{
    s_lastErr = str;
    wait30ms();
    RESET();
}
#endif
