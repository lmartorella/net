#include "hardware/fuses.h"
#include "appio.h"
#include "hardware/cm1602.h"
#include <string.h>

// The pointer is pointing to ROM space, otherwise after the RESET
// the volatile content can be lost.
static persistent const char* s_lastErr;

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

void clearln()
{
	_clr(0x40);
}

void clearlnUp()
{
	_clr(0x00);
}

static void _print(const char* str, BYTE addr)
{
	_clr(addr);
	cm1602_writeStr(str);
	ClrWdt();
}

void println(const char* str)
{
	_print(str, 0x40);	
}

void printlnUp(const char* str)
{
	_print(str, 0x00);	
}

void printch(char ch)
{
	cm1602_write(ch);
}

void fatal(const char* str)
{
    s_lastErr = str;
    wait30ms();
    RESET();
}

const char* getLastFatal()
{
    return s_lastErr;
}

