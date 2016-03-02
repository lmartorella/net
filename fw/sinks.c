#include "pch.h"
#include "protocol.h"
#include "appio.h"
#include "displaySink.h"
#include "hardware/dht11.h"

static bit sysSink_read();
static bit sysSink_write();

static const Sink s_sysSink = {
    "SYS ",
    &sysSink_read,
    &sysSink_write
};

const Sink* AllSinks[] = { 
    &s_sysSink,
#ifdef HAS_CM1602
    &g_displaySink, 
#endif
#ifdef HAS_DHT11
    &g_tempSink 
#endif
};

int AllSinksSize = 
#if defined(HAS_CM1602) || defined(HAS_DHT11)
    2
#else
    1
#endif
;

bit sink_nullFunc()
{
    // No data
    return FALSE;
}

static bit sysSink_read()
{
    // No read implemented so far
    fatal("SYS.WR");
    return TRUE;
}

const FOURCC ResetCode = "REST";
const FOURCC ExceptionText = "EXCM";
const FOURCC EndOfMetadataText = "EOMD";

static bit sysSink_write()
{
    WORD l = g_resetReason;
    // Write reset reason
    prot_control_write(&ResetCode, sizeof(FOURCC));
    prot_control_writeW(l);
    
    if (g_resetReason == RESET_EXC)
    {
        prot_control_write(&ExceptionText, sizeof(FOURCC));
        
        const char *exc = g_lastException;
        l = strlen(exc);
        prot_control_writeW(l);
        prot_control_write(exc, l);
    }

    prot_control_write(&EndOfMetadataText, sizeof(FOURCC));
    // Finish
    return FALSE;
}
