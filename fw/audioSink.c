#include "hardware/fuses.h"
#include "audioSink.h"
#include "appio.h"
#include "hardware/vs1011e.h"
#include <string.h>
#include "TCPIPStack/TCPIP.h"

static void createAudioSink(void);
static void destroyAudioSink(void);
static void pollAudioSink(void);

#define AUDIO_SINK_PORT (SINK_AUDIO_TYPE + BASE_SINK_PORT)
const rom Sink g_audioSink = { SINK_AUDIO_TYPE,
                                 0,
                                 AUDIO_SINK_PORT,
                                 &createAudioSink,
                                 &destroyAudioSink,
                                 &pollAudioSink };

// The TCP client socket of display listener
static TCP_SOCKET s_listenerSocket = INVALID_SOCKET;

static void createAudioSink()
{
	// Open the sever TCP channel
	s_listenerSocket = TCPOpen(0, TCP_OPEN_SERVER, AUDIO_SINK_PORT, TCP_PURPOSE_GENERIC_TCP_SERVER);
	if (s_listenerSocket == INVALID_SOCKET)
	{
		fatal("MP3_SRV");
	}
}

static void destroyAudioSink()
{
	if (s_listenerSocket != INVALID_SOCKET)
	{
		TCPClose(s_listenerSocket);
	}
}

typedef enum
{
    AUDIO_INIT = 0,         // Reset the Audio HW
    AUDIO_SET_VOLUME = 1,   // Change volume
    AUDIO_TEST_SINE = 2     // Start a sine test (reset to quit)
} AUDIO_COMMAND;  // 8-bit integer

typedef enum
{
    AUDIO_RES_OK = 0,
    AUDIO_RES_HW_FAIL = 1,      // MP3 hw fail (wrong model, internal test failed...)
    AUDIO_RES_SOCKET_ERR = 2,   // Missing data/buffer underrun on TCP/IP socket
} AUDIO_RESPONSE;  // 8-bit integer

typedef struct
{
    BYTE leftAttenuation;   // in db, 0 means full audio, 255 mute
    BYTE rightAttenuation;  // in db, 0 means full audio, 255 mute
} AUDIO_SET_VOLUME_DATA;

typedef struct
{
    UINT frequency;         // in Hz, using 48Khz sampling rate, will be rounded to next 375Hz step
} AUDIO_SINETEST_DATA;

static AUDIO_RESPONSE _initAudio()
{
    VS1011_MODEL r = vs1011_reset(FALSE);
    return (r != VS1011_MODEL_VS1011E) ? AUDIO_RES_HW_FAIL : AUDIO_RES_OK;
}

static AUDIO_RESPONSE _setVolume()
{
    AUDIO_SET_VOLUME_DATA msg;
    if (TCPGetArray(s_listenerSocket, (BYTE*)&msg, sizeof(AUDIO_SET_VOLUME_DATA)) != sizeof(AUDIO_SET_VOLUME_DATA))
    {
        return AUDIO_RES_SOCKET_ERR;
    }

    vs1011_volume(msg.leftAttenuation, msg.rightAttenuation);
    return AUDIO_RES_OK;
}

static AUDIO_RESPONSE _sineTest()
{
    AUDIO_SINETEST_DATA msg;
    if (TCPGetArray(s_listenerSocket, (BYTE*)&msg, sizeof(AUDIO_SINETEST_DATA)) != sizeof(AUDIO_SINETEST_DATA))
    {
        return AUDIO_RES_SOCKET_ERR;
    }

    vs1011_sineTest(msg.frequency);
    return AUDIO_RES_OK;
}

static void pollAudioSink()
{
	unsigned short s;
	if (!TCPIsConnected(s_listenerSocket))
	{
		return;
	}

	s = TCPIsGetReady(s_listenerSocket);
	if (s >= sizeof(AUDIO_COMMAND))
	{
		AUDIO_COMMAND cmd;
		TCPGetArray(s_listenerSocket, (BYTE*)&cmd, sizeof(AUDIO_COMMAND));
                AUDIO_RESPONSE response;
                switch (cmd)
                {
                    case AUDIO_INIT:
                        response = _initAudio();
                        break;
                    case AUDIO_SET_VOLUME:
                        response = _setVolume();
                        break;
                    case AUDIO_TEST_SINE:
                        response = _sineTest();
                        break;
                }

                // ACK
                if (TCPPutArray(s_listenerSocket, (BYTE*)&response, sizeof(AUDIO_RESPONSE)) != sizeof(AUDIO_RESPONSE))
                {
                    fatal("MP3_SND");
                }
                TCPFlush(s_listenerSocket);
		TCPDiscard(s_listenerSocket);
	}
}
