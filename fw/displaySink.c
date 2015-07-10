#include "hardware/fuses.h"
#include "displaySink.h"
#include "appio.h"
#include "hardware/cm1602.h"
#include <string.h>
#include "TCPIPStack/TCPIP.h"

#ifdef HAS_CM1602

static void readHandler(BYTE* data, WORD length);
static WORD writeHandler(BYTE* data);

const rom Sink g_displaySink = { "LINE",
                                 &readHandler,
                                 &writeHandler
};

static void readHandler(BYTE* data, WORD length)
{
    if (length > 15)
    {
        length = 15;
    }
    data[length] = '\0';
    // Write it
    printlnUp(data);
}

static WORD writeHandler(BYTE* data)
{
    // Num of lines
    ((WORD*)data)[0] = 1;
    // Num of columns
    ((WORD*)data)[1] = 15;
    return sizeof(WORD) * 2;
}

#endif