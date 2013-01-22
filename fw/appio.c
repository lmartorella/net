#include "hardware/fuses.h"
#include "appio.h"
#include "hardware/cm1602.h"
#include <string.h>

static char s_lastErr[8];

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

static void _print(const rom char* str, BYTE addr)
{
	_clr(addr);
	cm1602_writeStr(str);
	ClrWdt();
}

static void _printr(const ram char* str, BYTE addr)
{
	_clr(addr);
	cm1602_writeStrRam(str);
	ClrWdt();
}

void println(const rom char* str)
{
	_print(str, 0x40);	
}

void printlnr(const ram char* str)
{
	_printr(str, 0x40);	
}

void printlnUp(const rom char* str)
{
	_print(str, 0x00);	
}

void fatal(const rom char* str)
{
	strncpypgm2ram(s_lastErr, str, sizeof(s_lastErr));
	Reset();
}

const ram char* getLastFatal()
{
	return s_lastErr;
}
