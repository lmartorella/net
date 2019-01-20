#include "../pch.h"
#include "digio.h"
#include "../protocol.h"

#ifdef HAS_DIGIO

// Only support 1 bit for IN and 1 for OUT (can even be the same)

void digio_init()
{
    // First enable input bits
    DIGIO_TRIS_IN_BIT = 1;
    // Then the output. So if the same port is configured as I/O it will work
    DIGIO_TRIS_OUT_BIT = 0;
}

bit digio_out_write()
{
    // One port
    WORD b = 1;
    // Number of switch = 1
    prot_control_write(&b, sizeof(WORD));
    return FALSE;
}

// Read bits to set as output
bit digio_out_read()
{
    if (prot_control_readAvail() < 5) {
        // Need more data
        return TRUE;
    }
    WORD swCount;
    BYTE arr;
    // Number of switches sent (expect 1)
    prot_control_read(&swCount, 2);
    // Number of bytes sent (expect 1)
    prot_control_read(&swCount, 2);
    // The byte: the bit 0 is data
    prot_control_read(&arr, 1);
    DIGIO_PORT_OUT_BIT = !!arr;
    return FALSE;
}

// Write bits read as input
bit digio_in_write()
{   
    WORD swCount = 1;
    // Number of switches sent (1)
    prot_control_write(&swCount, 2);
    // Number of bytes sent (1)
    prot_control_write(&swCount, 2);
    BYTE arr = DIGIO_PORT_IN_BIT;
    // The byte: the bit 0 is data
    prot_control_write(&arr, 1);
    return FALSE;
}

#endif
