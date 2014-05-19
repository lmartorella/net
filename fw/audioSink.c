#include "hardware/fuses.h"
#include "audioSink.h"
#include "appio.h"
#include "hardware/vs1011e.h"
#include "hardware/spiram.h"
#include <string.h>
#include "TCPIPStack/TCPIP.h"

static void createAudioSink(void);
static void destroyAudioSink(void);
static void pollAudioSink(void);
static BOOL _isWaitingForCommand;
static BYTE TEMP_BUFFER[250];       // Less than 256, to call sram_write_8
static UINT32 _ringStart, _ringEnd;
#define RING_SIZE 0x20000       // All 128Kb!

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
	s_listenerSocket = TCPOpen(0, TCP_OPEN_SERVER, AUDIO_SINK_PORT, TCP_PURPOSE_STREAM_TCP_SERVER);
	if (s_listenerSocket == INVALID_SOCKET)
	{
		fatal("MP3_SRV");
	}
        _isWaitingForCommand = TRUE;
        _ringStart = _ringEnd = 0;
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
    AUDIO_TEST_SINE = 2,    // Start a sine test (reset to quit)
    AUDIO_STREAM = 3,       // Send data packet to SDI channel

    AUDIO_TEST_1 = 100,      // Test, don't copy data in mem but get it from TCP
    AUDIO_TEST_2 = 101,      // Test, don't fetch data from TCP but flush it. Simulate a ram copy
    AUDIO_TEST_3 = 102      // TEST_1 + TEST_2
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

typedef struct
{
    WORD packetSize;
} AUDIO_STREAM_DATA;

typedef struct
{
    AUDIO_RESPONSE res;
    WORD elapsedMs;
    WORD calls;
} AUDIO_STREAM_RESPONSE;

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

static WORD _streamSize;
static void _processData();
static DWORD _startTime;
static int _dequeueCallCount;

static AUDIO_RESPONSE _startStream()
{
    // Read packet size
    _startTime = TickGet();
    _dequeueCallCount = 0;
    AUDIO_STREAM_DATA msg;
    if (TCPGetArray(s_listenerSocket, (BYTE*)&msg, sizeof(AUDIO_STREAM_DATA)) != sizeof(AUDIO_STREAM_DATA))
    {
        return AUDIO_RES_SOCKET_ERR;
    }
    _streamSize = msg.packetSize;

    // Now read _streamSize BYTES to RAM
    _processData();
    return AUDIO_RES_OK;
}

static BOOL _disableTcpGet;
static BOOL _disableRamWrite;

static void _processData()
{
    _dequeueCallCount++;
    WORD len = TCPIsGetReady(s_listenerSocket);
    while (len > 0 && _streamSize > 0)
    {
        // Allocate bytes and transfer it to the external RAM ring buffer
        BYTE l = len > sizeof(TEMP_BUFFER) ? sizeof(TEMP_BUFFER) : len;
        if ((_ringEnd + l) > RING_SIZE)
        {
            l = RING_SIZE - _ringEnd;
        }

        if (_disableTcpGet)
        {
            TCPDiscard(s_listenerSocket);
            l = 255;    // cannot load the sram_write_8 with more than 255 bytes!
        }
        else
        {
            TCPGetArray(s_listenerSocket, TEMP_BUFFER, l);
        }

        if (_disableRamWrite)
        {
        }
        else
        {
            // Copy data to Ext RAM
            di();
            sram_write_8(TEMP_BUFFER, _ringEnd, (BYTE)l);
            _ringEnd = (_ringEnd + l) % RING_SIZE;
            ei();
        }
        
        len -= l;
        _streamSize -= l;
    }

    if (_streamSize == 0)
    {
        AUDIO_STREAM_RESPONSE response;
        response.elapsedMs = TickConvertToMilliseconds(TickGet() - _startTime);
        response.res = AUDIO_RES_OK;
        response.calls = _dequeueCallCount;

        // ACK
        _isWaitingForCommand = TRUE;
        
        if (TCPPutArray(s_listenerSocket, (BYTE*)&response, sizeof(AUDIO_STREAM_RESPONSE)) != sizeof(AUDIO_STREAM_RESPONSE))
        {
            fatal("MP3_ACK");
        }
        
        TCPFlush(s_listenerSocket);
        TCPDiscard(s_listenerSocket);
    }
}

static void pollAudioSink()
{
	unsigned short s;
	if (!TCPIsConnected(s_listenerSocket))
	{
            return;
	}

	s = TCPIsGetReady(s_listenerSocket);
        if (_isWaitingForCommand)
        {
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
                        case AUDIO_TEST_1:
                            _disableRamWrite = TRUE;
                            _disableTcpGet = FALSE;
                            goto audioStream;
                        case AUDIO_TEST_2:
                            _disableRamWrite = FALSE;
                            _disableTcpGet = TRUE;
                            goto audioStream;
                        case AUDIO_TEST_3:
                            _disableTcpGet = _disableRamWrite = TRUE;
                            goto audioStream;
                        case AUDIO_STREAM:
                            _disableTcpGet = _disableRamWrite = FALSE;
audioStream:
                            response = _startStream();
                            if (response == AUDIO_RES_OK)
                            {
                                _isWaitingForCommand = FALSE;
                            }
                            break;
                    }

                    if (_isWaitingForCommand)
                    {
                        // ACK
                        if (TCPPutArray(s_listenerSocket, (BYTE*)&response, sizeof(AUDIO_RESPONSE)) != sizeof(AUDIO_RESPONSE))
                        {
                            fatal("MP3_SND");
                        }
                        TCPFlush(s_listenerSocket);
                        TCPDiscard(s_listenerSocket);
                    }
            }
        }
        else
        {
            _processData();
        }
}
