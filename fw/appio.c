#include "pch.h"
#include "appio.h"
#include "hardware/cm1602.h"
#include "hardware/leds.h"

const char* g_lastException;
RESET_REASON g_resetReason;

#ifdef HAS_CM1602
static const char* g_resetReasonMsgs[] = { 
				"POR",
				"BOR",
				"CFG",
				"WDT",
				"STK",
				"RST",
				"EXC:"  };
#endif

void appio_init()
{
#ifdef HAS_CM1602
    static const char* msg1 = "Hi world! ";

    // reset display
    cm1602_reset();
    cm1602_clear();
    cm1602_setEntryMode(MODE_INCREMENT | MODE_SHIFTOFF);
    cm1602_enable(ENABLE_DISPLAY | ENABLE_CURSOR | ENABLE_CURSORBLINK);

    cm1602_setDdramAddr(0);
    cm1602_writeStr(msg1);
    cm1602_writeStr(g_resetReasonMsgs[g_resetReason]);
    if (sys_isResetReasonExc())
    {
        cm1602_setDdramAddr(0x40);
        cm1602_writeStr(g_lastException);
    }

    wait1s();
#endif

#ifdef HAS_LED
    LED_TRISBIT = 0;
    led_off();
#endif
}

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
#ifdef HAS_LED
    led_off();
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
#ifdef HAS_LED
    led_on();
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
#ifdef HAS_LED
    led_on();
#endif
}

BOOL sys_isResetReasonExc()
{
    return g_resetReason == RESET_EXC;
}
