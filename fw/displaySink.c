#include "hardware/fuses.h"
#include "displaySink.h"
#include "appio.h"
#include "hardware/cm1602.h"
#include <string.h>
#include "TCPIPStack/TCPIP.h"
#include "hardware/ip.h"

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
    ip_control_readW(&length);
    if (length > 15)
    {
        p = length - 15;
        length = 15;
    }
    ip_control_read(buf, length);
    while (p > 0)
    {
        ip_control_read(buf + 15, 1);
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
    ip_control_writeW(1);
    // Num of columns
    ip_control_writeW(15);
}

#endif