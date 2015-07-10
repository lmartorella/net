#include "hardware/fuses.h"
#include "displaySink.h"
#include "appio.h"
#include "hardware/cm1602.h"
#include <string.h>
#include "TCPIPStack/TCPIP.h"

#ifdef HAS_CM1602

static void createDisplaySink(void);
static void destroyDisplaySink(void);
static void pollDisplaySink(void);

const rom Sink g_displaySink = { "LINE",
                                 //&createDisplaySink,
                                 //&destroyDisplaySink,
                                 //&pollDisplaySink 
};

//extern WORD _ringStart, _ringEnd, _streamSize;

static void readData()
{
    char buffer[16];
    WORD s;
    Read((BYTE*)&s, sizeof(WORD));
    if (s > 15)
    {
        s = 15;
    }
    Read((BYTE*)buffer, s);
    buffer[s] = '\0';
    // Write it
    printlnUp(buffer);

            // TMP
            //s = _ringStart;
            //if (TCPPutArray(s_listenerSocket, (BYTE*)&s, 2) != 2)
            //{
            //    fatal("DSP_SND");
            //}
            //s = _ringEnd;
            //if (TCPPutArray(s_listenerSocket, (BYTE*)&s, 2) != 2)
            //{
            //    fatal("DSP_SND");
            //}
            //s = _streamSize;
            //if (TCPPutArray(s_listenerSocket, (BYTE*)&s, 2) != 2)
            //{
            //    fatal("DSP_SND");
            //}
}

#endif