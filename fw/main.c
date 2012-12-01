
#include "fuses.h"
#include "cm1602.h"

// The smallest type capable of representing all values in the enumeration type.
enum RESET_REASON
{
	RESET_POWER,  // Power-on reset
	RESET_BROWNOUT,
	RESET_CONFIGMISMATCH,
	RESET_WATCHDOG,
	RESET_STACKFAIL,
	RESET_MCLR
};
static enum RESET_REASON _reason;

// Check RCON and STKPTR register for anormal reset cause
static void storeResetReason(void)
{
	if (!RCONbits.NOT_RI)
	{
		// Software exception. _reset contains SW code.
		return;
	}
	if (!RCONbits.NOT_POR)
	{
		// Normal Power-on startup. Ok.
		RCONbits.NOT_POR = 1;
		_reason = RESET_POWER;
		return;
	}
	if (!RCONbits.NOT_BOR)
	{
		// Brown-out reset. Low voltage.
		RCONbits.NOT_BOR = 1;
		_reason = RESET_BROWNOUT;
		return;
	}
/*
	if (!RCONbits.NOT_CM)
	{
		// Configuration mismatch reset. EEPROM fail.
		RCONbits.NOT_CM = 1;
		_reason = RESET_CONFIGMISMATCH;
		return;
	}
*/
	if (!RCONbits.NOT_TO)
	{
		// Watchdog reset. Loop detected.
		RCONbits.NOT_TO = 1;
		_reason = RESET_WATCHDOG;
		return;
	}
	if (STKPTRbits.STKFUL || STKPTRbits.STKUNF)
	{
		// Stack underrun/overrun reset. 
		STKPTRbits.STKFUL = STKPTRbits.STKUNF = 0;
		_reason = RESET_STACKFAIL;
		return;
	}
	// Else it was reset manually (MCLR)
	_reason = RESET_MCLR;
}

void main()
{
	// Analyze RESET reason
	storeResetReason();

	// Enable all PORTE as output
	PORTE = 0xff;
	TRISE = 0;
	PORTE = 0xff;
	wait30ms();

	// reset display
	cm1602_reset();
	cm1602_clear();
	cm1602_setEntryMode(MODE_INCREMENT | MODE_SHIFTOFF);
	cm1602_enable(ENABLE_DISPLAY | ENABLE_CURSOR | ENABLE_CURSORBLINK);

	cm1602_setDdramAddr(0);
	cm1602_write('H');
	cm1602_write('e');
	cm1602_write('l');
	cm1602_write('l');
	cm1602_write('o');
	cm1602_write(' ');
	cm1602_write('w');
	cm1602_write('o');
	cm1602_write('r');
	cm1602_write('l');
	cm1602_write('d');
	cm1602_write('.');

	cm1602_setDdramAddr(0x40);
	cm1602_write('O');
	cm1602_write('K');
	cm1602_write('.');

	// I'm alive
	while (1) ClrWdt();
	
}