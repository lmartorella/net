
#include <p18f87j60.h>
#include "fuses.h"

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

	// I'm alive
	while (1) ClrWdt();
	
}