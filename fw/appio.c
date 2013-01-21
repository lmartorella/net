#include "hardware/fuses.h"
#include "appio.h"
#include "hardware/cm1602.h"

static void _clr(void)
{
	char i;
	for (i = 0; i < 16; i++)
	{
		cm1602_write(' ');
	}
}

static void _print(const rom char* str, BYTE addr)
{
	cm1602_setDdramAddr(addr);
	_clr();
	cm1602_setDdramAddr(addr);
	cm1602_writeStr(str);
	ClrWdt();
}

void println(const rom char* str)
{
	_print(str, 0x40);	
}

void printlnUp(const rom char* str)
{
	_print(str, 0x00);	
}

void error(const rom char* str)
{
	println("E:");
	cm1602_writeStr(str);
}
