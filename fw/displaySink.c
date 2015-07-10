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

//#define DISPLAY_SINK_PORT (SINK_DISPLAY_TYPE + BASE_SINK_PORT)
const rom Sink g_displaySink = { "LINE",
                                 &createDisplaySink,
                                 &destroyDisplaySink,
                                 &pollDisplaySink };

// The TCP client socket of display listener
//static TCP_SOCKET s_listenerSocket = INVALID_SOCKET;

static void createDisplaySink()
{
}

static void destroyDisplaySink()
{
}

extern WORD _ringStart, _ringEnd, _streamSize;

static void pollDisplaySink()
{
    /*
	unsigned short s;
	if (!TCPIsConnected(s_listenerSocket))
	{
		return;
	}

	s = TCPIsGetReady(s_listenerSocket);
	if (s > sizeof(unsigned short))
	{
		char buffer[16];
		TCPGetArray(s_listenerSocket, (BYTE*)&s, sizeof(unsigned short));
		if (s > 15)
		{
			s = 15;
		}
		TCPGetArray(s_listenerSocket, (BYTE*)buffer, s);
		buffer[s] = '\0';
		// Write it
		printlnUp(buffer);

                // ACK
                s = 0;
                if (TCPPutArray(s_listenerSocket, (BYTE*)&s, 2) != 2)
                {
                    fatal("DSP_SND");
                }

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

                TCPFlush(s_listenerSocket);
		TCPDiscard(s_listenerSocket);
	}
     * */
}

#endif