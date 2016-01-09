#include "pch.h"
#include "protocol.h"
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

static void sysSink_write()
{
    // Write reset reason
    FOURCC code;
    memcpy(&code, &ResetCode, sizeof(FOURCC));
    prot_control_write(&code, sizeof(FOURCC));
    prot_control_writeW(_reason);
    
    if (sys_isResetReasonExc())
    {
        memcpy(&code, &ExceptionText, sizeof(FOURCC));
        prot_control_write(&code, sizeof(FOURCC));
        
        char *exc = sys_getLastFatal();
        WORD l = strlen(exc);
        prot_control_writeW(l);
        prot_control_write(exc, l);
    }

    memcpy(&code, &EndOfMetadataText, sizeof(FOURCC));
    prot_control_write(&code, sizeof(FOURCC));
}
