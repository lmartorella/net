#include "pch.h"
#include "protocol.h"
#include "appio.h"
#include "displaySink.h"
#include "halfduplex.h"
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
    
#if defined(HAS_CM1602) && !(defined(HAS_MAX232_SOFTWARE) || defined(HAS_FAKE_RS232))
    &g_displaySink, 
#endif
    
#ifdef HAS_DHT11
    &g_tempSink,
#endif

#if defined(HAS_MAX232_SOFTWARE) || defined(HAS_FAKE_RS232)
    &g_halfDuplexSink 
#endif
};

int AllSinksSize =
#if defined(HAS_MAX232_SOFTWARE) || defined(HAS_FAKE_RS232)
    2
#elif defined(HAS_DIGIO) && defined(HAS_CM1602) && defined(HAS_DHT11)
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
#ifdef HAS_BUS_SERVER
const TWOCC BusMasterStats = "BM";
#endif

enum SYSSINK_CMD {
    SYSSINK_CMD_RESET = 1,
    SYSSINK_CMD_CLRRST = 2,
};

static bit sysSink_receive()
{
    if (prot_control_readAvail() < 1) {
        // Wait cmd
        return 1;
    }
    BYTE cmd;
    prot_control_read(&cmd, 1);
    switch (cmd) {
        case SYSSINK_CMD_RESET:
            // Reset device
            fatal("RST");
            break;
        case SYSSINK_CMD_CLRRST:
            // Reset reset reason
            g_resetReason = RESET_NONE;
            break;
    }
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

    #ifdef HAS_BUS_SERVER
    prot_control_write(&BusMasterStats, sizeof(TWOCC));
    prot_control_write(&g_busStats, sizeof(BUS_MASTER_STATS));
    memset(&g_busStats, 0, sizeof(BUS_MASTER_STATS));
    #endif

    prot_control_write(&EndOfMetadataText, sizeof(TWOCC));
    // Finish
    return FALSE;
}
