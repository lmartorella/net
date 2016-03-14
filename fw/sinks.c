#include "pch.h"
#include "protocol.h"
#include "appio.h"
#include "displaySink.h"
#include "hardware/dht11.h"

static bit sysSink_read();
static bit sysSink_write();
static bit outSink_read();
static bit outSink_write();
static bit inSink_write();

static const Sink s_sysSink = {
    "SYS ",
    &sink_nullFunc,
    &sysSink_write
};

static const Sink s_outSink = {
    "DOAR",
    &outSink_read,
    &outSink_write
};

static const Sink s_inSink = {
    "DIAR",
    &sink_nullFunc,
    &inSink_write,
};

const Sink* AllSinks[] = { 
    &s_sysSink,
    &s_outSink,
    &s_inSink,
#ifdef HAS_CM1602
    &g_displaySink, 
#endif
#ifdef HAS_DHT11
    &g_tempSink 
#endif
};

int AllSinksSize = 
#if defined(HAS_CM1602) || defined(HAS_DHT11)
    4
#else
    3
#endif
;

bit sink_nullFunc()
{
    // No data
    return FALSE;
}
const TWOCC ResetCode = "RS";
const TWOCC ExceptionText = "EX";
const TWOCC EndOfMetadataText = "EN";

static bit sysSink_write()
{
    WORD l = g_resetReason;
    // Write reset reason
    prot_control_write(&ResetCode, sizeof(TWOCC));
    prot_control_writeW(l);
    
    if (g_resetReason == RESET_EXC)
    {
        prot_control_write(&ExceptionText, sizeof(TWOCC));
        
        const char *exc = g_lastException;
        l = strlen(exc);
        prot_control_writeW(l);
        prot_control_write(exc, l);
    }

    prot_control_write(&EndOfMetadataText, sizeof(TWOCC));
    // Finish
    return FALSE;
}

#define TRIS_IN_BIT TRISAbits.TRISA1
#define PORT_IN_BIT PORTAbits.RA1
#define TRIS_OUT_BIT TRISAbits.TRISA0
#define PORT_OUT_BIT PORTAbits.RA0

static bit outSink_write()
{
    TRIS_OUT_BIT = 0;

    // One port
    WORD b = 1;
    prot_control_write(&b, sizeof(WORD));
    return FALSE;
}

// Read bits to set as output
static bit outSink_read()
{
    if (prot_control_readAvail() < 5) {
        // Need more data
        return TRUE;
    }
    WORD swCount;
    BYTE arr;
    prot_control_read(&swCount, 2);
    prot_control_read(&swCount, 2);
    prot_control_read(&arr, 1);
    PORT_OUT_BIT = !!arr;
    return FALSE;
}

// Write bits read as input
static bit inSink_write()
{
    TRIS_IN_BIT = 1;
    
    WORD swCount = 1;
    prot_control_write(&swCount, 2);
    prot_control_write(&swCount, 2);
    BYTE arr = PORT_IN_BIT || 1;
    prot_control_write(&arr, 1);
    return FALSE;
}
