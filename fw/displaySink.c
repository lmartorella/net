#include "pch.h"
#include "displaySink.h"
#include "appio.h"
#include "protocol.h"
#include "hardware/cm1602.h"
//#include "TCPIPStack/TCPIP.h"
//#include "hardware/ip.h"

#ifdef HAS_CM1602

static BOOL readHandler();
static void writeHandler();

const Sink g_displaySink = { { "LINE" },
                             &readHandler,
                             &writeHandler
};

static BOOL readHandler()
{
    // Only single packet messages are supported
    WORD length, p = 0;
    BYTE buf[16];
    prot_control_readW(&length);
    if (length > 15)
    {
        p = length - 15;
        length = 15;
    }
    prot_control_read(buf, length);
    while (p > 0)
    {
        prot_control_read(buf + 15, 1);
        p--;
    }
    buf[length] = '\0';
    // Write it
    printlnUp(buf);
    return FALSE;
}

static void writeHandler()
{
    // Num of lines
   prot_control_writeW(1);
    // Num of columns
   prot_control_writeW(15);
}

#endif