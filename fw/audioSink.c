#include "hardware/fuses.h"
#include "audioSink.h"
#include "appio.h"
#include "hardware/vs1011e.h"
#include "hardware/spiram.h"
#include "hardware/spi.h"
#include <string.h>
#include "TCPIPStack/TCPIP.h"

static void createAudioSink(void);
static void destroyAudioSink(void);
static void pollAudioSink(void);
static UINT16 _ringStart, _ringEnd;
#define RING_SIZE 0x8000      // The lower 32Kb
#define RING_MASK 0x7FFF      // The lower 32Kb

inline static UINT16 queueSize()
{
    //return ((_ringEnd - _ringStart) + RING_SIZE) % RING_SIZE;
    return (_ringEnd - _ringStart) & (RING_MASK);
}

inline static UINT16 freeSize()
{
    return RING_SIZE - queueSize();
}

static union
{
    struct
    {
        unsigned disableTcpGet :1;
        unsigned disableRamWrite :1;
        unsigned isWaitingForCommand :1;
        unsigned sdiCopyEnabled :1;
    };
    BYTE b;
} s_flags;

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
        _ringStart = _ringEnd = 0l;
        s_flags.b = 0;
        s_flags.isWaitingForCommand = 1;
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
    AUDIO_ENABLE_SDI = 4,   // Enable stream data to SDI MP3 channel

    AUDIO_TEST_1 = 100,      // Test, don't copy data in ext mem but get it from TCP
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
    UINT16 elapsedMs;
    UINT16 calls;
    UINT16 freeSize;
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

void audio_pollMp3Player();

static void _processData()
{
    _dequeueCallCount++;
    static BYTE buffer[1500];

    // If no room, leave the socket unread
    if (_streamSize > 0 && freeSize() >= sizeof(buffer))
    {
        // Only dequeue a buffer of data, quick poll to leave room for MP3 poller
        WORD len = TCPIsGetReady(s_listenerSocket);
        if (len > 0)
        {
            // Allocate bytes and transfer it to the external RAM ring buffer
            WORD toCopy = len > sizeof(buffer) ? sizeof(buffer) : len;

            if (s_flags.disableTcpGet)
            {
                TCPDiscard(s_listenerSocket);
                toCopy = 128;    // cannot load the sram_write_8 with more than 255 bytes!
            }
            else
            {
                // Fetch data from eth RAM
                TCPGetArray(s_listenerSocket, buffer, toCopy);
            }
            audio_pollMp3Player();

            if (!s_flags.disableRamWrite)
            {
                // Copy data to Ext RAM
                // Calc free space (only valid when _ringEnd > 0)
                UINT16 space = RING_SIZE - _ringEnd;
                if (_ringEnd > 0 && toCopy > space)
                {
                    // No room for a single block copy.
                    // Copy in two steps
                    sram_write(buffer, (UINT32)_ringEnd, space);
                    _ringEnd = 0;
                    audio_pollMp3Player();
                    sram_write(buffer + space, (UINT32)0, toCopy - space);
                    _ringEnd = toCopy - space;
                }
                else
                {
                    // Copy in one go
                    sram_write(buffer, _ringEnd, toCopy);
                    //_ringEnd = (_ringEnd + l) % RING_SIZE;
                    // It will cropped to 16 bit
                    _ringEnd += toCopy;
                    _ringEnd %= RING_MASK;
                }

                audio_pollMp3Player();
            }

            _streamSize -= toCopy;
        }
    }

    if (_streamSize == 0)
    {
        // Only do it if no other operation was done above
        AUDIO_STREAM_RESPONSE response;
        response.elapsedMs = TickConvertToMilliseconds(TickGet() - _startTime);
        response.res = AUDIO_RES_OK;
        response.calls = _dequeueCallCount;
        response.freeSize = freeSize();

        // ACK
        s_flags.isWaitingForCommand = 1;
        
        if (TCPPutArray(s_listenerSocket, (BYTE*)&response, sizeof(AUDIO_STREAM_RESPONSE)) != sizeof(AUDIO_STREAM_RESPONSE))
        {
            fatal("MP3_ACK");
        }

        audio_pollMp3Player();

        TCPFlush(s_listenerSocket);
        //TCPDiscard(s_listenerSocket);

        audio_pollMp3Player();
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
        if (s_flags.isWaitingForCommand)
        {
            if (s >= sizeof(AUDIO_COMMAND))
            {
                    AUDIO_COMMAND cmd;
                    TCPGetArray(s_listenerSocket, (BYTE*)&cmd, sizeof(AUDIO_COMMAND));
                    AUDIO_RESPONSE response = AUDIO_RES_OK;
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
                        case AUDIO_ENABLE_SDI:
                            s_flags.sdiCopyEnabled = 1;
                            break;
                        case AUDIO_TEST_1:
                            s_flags.disableRamWrite = 1;
                            s_flags.disableTcpGet = 0;
                            goto audioStream;
                        case AUDIO_TEST_2:
                            s_flags.disableRamWrite = 0;
                            s_flags.disableTcpGet = 1;
                            goto audioStream;
                        case AUDIO_TEST_3:
                            s_flags.disableTcpGet = s_flags.disableRamWrite = 1;
                            goto audioStream;
                        case AUDIO_STREAM:
                            s_flags.disableTcpGet = s_flags.disableRamWrite = 0;
audioStream:
                            response = _startStream();
                            if (response == AUDIO_RES_OK)
                            {
                                s_flags.isWaitingForCommand = 0;
                            }
                            break;
                    }

                    if (s_flags.isWaitingForCommand)
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


void audio_pollMp3Player()
{
    static BYTE buffer[32];

    if (s_flags.sdiCopyEnabled)
    {
        UINT16 len = queueSize();
        while (vs1011_isWaitingData() && len >= sizeof(buffer))
        {
            // Only valid when _ringStart > 0
            UINT16 avail = RING_SIZE - _ringStart;
            if (_ringStart > 0 && sizeof(buffer) > avail)
            {
                sram_read(buffer, (UINT32)_ringStart, avail);
                sram_read(buffer + avail, (UINT32)0, sizeof(buffer) - avail);
            }
            else
            {
                // Pull 32 bytes of data
                sram_read(buffer, _ringStart, sizeof(buffer));
            }
            
            //_ringStart = (_ringStart + toCopy) % RING_SIZE;
            _ringStart += sizeof(buffer);
            _ringStart %= RING_MASK;
            
            len -= sizeof(buffer);
            
            // Flush data to MP3
            vs1011_streamData(buffer, sizeof(buffer));
        }
    }
}