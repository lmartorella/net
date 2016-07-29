#include "pch.h"
#include "protocol.h"
#include "appio.h"
#include "displaySink.h"
#include "hardware/dht11.h"
#include "hardware/digio.h"

static bit sysSink_receive();
static bit sysSink_transmit();

static const Sink s_sysSink = {
    "SYS ",
    &sysSink_receive,
    &sysSink_transmit
};

const Sink* AllSinks[] = { 
    &s_sysSink,
#ifdef HAS_DIGIO
    &g_outSink,
    &g_inSink,
#endif
    
#ifdef HAS_CM1602
    &g_displaySink, 
#endif
    
#ifdef HAS_DHT11
    &g_tempSink 
#endif
};

int AllSinksSize =
#if defined(HAS_DIGIO) && defined(HAS_CM1602) && defined(HAS_DHT11)
    5
#elif (defined(HAS_DIGIO) && defined(HAS_CM1602)) || (defined(HAS_DIGIO) && defined(HAS_DHT11))
    4
#elif defined(HAS_DIGIO)
    3
#elif defined(HAS_CM1602) && defined(HAS_DHT11)
    3
#elif defined(HAS_CM1602) || defined(HAS_DHT11)
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
const TWOCC ResetCode = "RS";
const TWOCC ExceptionText = "EX";
const TWOCC EndOfMetadataText = "EN";

static bit sysSink_receive()
{
    // Reset reset reason
    g_resetReason = RESET_NONE;
    // No more data
    return FALSE;
}

static bit sysSink_transmit()
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
