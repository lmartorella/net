#include "hardware/fuses.h"
#include "protocol.h"
#include "hardware/ip.h"
#include "appio.h"
#include "displaySink.h"

static BOOL sysSink_read();
static void sysSink_write();

static const Sink s_sysSink = {
    "SYS ",
    &sysSink_read,
    &sysSink_write
};

const Sink* AllSinks[] = { 
    &s_sysSink,
#ifdef HAS_CM1602
    &g_displaySink 
#endif
};
int AllSinksSize = (sizeof(AllSinks) / sizeof(const Sink* const));


static BOOL sysSink_read()
{
    // No read implemented so far
    fatal("SYS.WR");
    return TRUE;
}

// The smallest type capable of representing all values in the enumeration type.
enum RESET_REASON
{
	RESET_POWER = 0,  // Power-on reset
	RESET_BROWNOUT,
	RESET_CONFIGMISMATCH,
	RESET_WATCHDOG,
	RESET_STACKFAIL,
	RESET_MCLR,
	RESET_EXC
};
static enum RESET_REASON _reason;

#ifdef HAS_CM1602
static const char* g_reasonMsgs[] = { 
				"POR",
				"BOR",
				"CFG",
				"WDT",
				"STK",
				"RST",
				"EXC:"  };
#endif

const FOURCC ResetCode = "REST";
const FOURCC ExceptionText = "EXCT";
const FOURCC EndOfMetadataText = "EOMD";

// Check RCON and STKPTR register for anormal reset cause
void sys_storeResetReason()
{
	if (!RCONbits.NOT_RI)
	{
		// Software exception. 
		// Obtain last reason from appio.h 
		_reason = RESET_EXC;
	}
	else if (!RCONbits.NOT_POR)
	{
		// Normal Power-on startup. Ok.
		_reason = RESET_POWER;
	}
	else if (!RCONbits.NOT_BOR)
	{
		// Brown-out reset. Low voltage.
		_reason = RESET_BROWNOUT;
	}
/*
	else if (!RCONbits.NOT_CM)
	{
		// Configuration mismatch reset. EEPROM fail.
		_reason = RESET_CONFIGMISMATCH;
	}
*/
	else if (!RCONbits.NOT_TO)
	{
		// Watchdog reset. Loop detected.
		_reason = RESET_WATCHDOG;
	}
	else if (STKPTRbits.STKFUL || STKPTRbits.STKUNF)
	{
		// Stack underrun/overrun reset. 
		_reason = RESET_STACKFAIL;
	}
	else
	{
		// Else it was reset manually (MCLR)
		_reason = RESET_MCLR;
	}
	RCON = RCON | 0x33;	// reset all flags
	STKPTRbits.STKFUL = STKPTRbits.STKUNF = 0;
}

#ifdef HAS_CM1602
const char* sys_getResetReasonStr()
{
    // Includes the final \0
    return g_reasonMsgs[_reason];
}
#endif

BOOL sys_isResetReasonExc()
{
    return _reason == RESET_EXC;
}

static void sysSink_write()
{
    // Write reset reason
    FOURCC code;
    memcpy(&code, &ResetCode, sizeof(FOURCC));
    ip_control_write(&code, sizeof(FOURCC));
    ip_control_writeW(_reason);
    
    if (sys_isResetReasonExc())
    {
        memcpy(&code, &ExceptionText, sizeof(FOURCC));
        ip_control_write(&code, sizeof(FOURCC));
        
        char *exc = sys_getLastFatal();
        WORD l = strlen(exc);
        ip_control_writeW(l);
        ip_control_write(exc, l);
    }

    memcpy(&code, &EndOfMetadataText, sizeof(FOURCC));
    ip_control_write(&code, sizeof(FOURCC));
}
