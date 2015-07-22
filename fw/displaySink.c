#include "hardware/fuses.h"
#include "displaySink.h"
#include "appio.h"
#include "hardware/cm1602.h"
#include <string.h>
#include "TCPIPStack/TCPIP.h"

#ifdef HAS_CM1602

static void readHandler(void* data, WORD length);
static WORD writeHandler(void* data);

const rom Sink g_displaySink = { { 'L', 'I', 'N', 'E' },
                                 &readHandler,
                                 &writeHandler
};

static void readHandler(void* data, WORD length)
{
    if (length > 15)
    {
        length = 15;
    }
    ((BYTE*)data)[length] = '\0';
    // Write it
    printlnUp(data);
}

static WORD writeHandler(void* data)
{
    // Num of lines
    ((WORD*)data)[0] = 1;
    // Num of columns
    ((WORD*)data)[1] = 15;
    return sizeof(WORD) * 2;
}

#endif