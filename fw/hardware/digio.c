#include "../pch.h"
#include "digio.h"
#include "../protocol.h"

#ifdef HAS_DIGIO

static bit outSink_read();
static bit outSink_write();
static bit inSink_write();

const Sink g_outSink = {
    "DOAR",
    &outSink_read,
    &outSink_write
};

const Sink g_inSink = {
    "DIAR",
    &sink_nullFunc,
    &inSink_write,
};

void digio_init()
{
    DIGIO_TRIS_OUT_BIT = 0;
    DIGIO_TRIS_IN_BIT = 1;
}

static bit outSink_write()
{
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
    DIGIO_PORT_OUT_BIT = !!arr;
    return FALSE;
}

// Write bits read as input
static bit inSink_write()
{   
    WORD swCount = 1;
    prot_control_write(&swCount, 2);
    prot_control_write(&swCount, 2);
    BYTE arr = DIGIO_PORT_IN_BIT;
    prot_control_write(&arr, 1);
    return FALSE;
}

#endif
