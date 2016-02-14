#include "pch.h"
#include "protocol.h"
#include "appio.h"
#include "displaySink.h"

static BOOL sysSink_read();
static BOOL sysSink_write();

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
int AllSinksSize = 
#ifdef HAS_CM1602
    2
#else
    1
#endif
;

static BOOL sysSink_read()
{
    // No read implemented so far
    fatal("SYS.WR");
    return TRUE;
}

enum RESET_REASON _reason;

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
const FOURCC ExceptionText = "EXCM";
const FOURCC EndOfMetadataText = "EOMD";

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

static BOOL sysSink_write()
{
    // Write reset reason
    prot_control_write(&ResetCode, sizeof(FOURCC));
    prot_control_writeW(_reason);
    
    if (sys_isResetReasonExc())
    {
        prot_control_write(&ExceptionText, sizeof(FOURCC));
        
        const char *exc = s_lastErr;
        WORD l = strlen(exc);
        prot_control_writeW(l);
        prot_control_write(exc, l);
    }

    prot_control_write(&EndOfMetadataText, sizeof(FOURCC));
    // Finish
    return FALSE;
}
