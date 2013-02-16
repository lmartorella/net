#include "hardware/fuses.h"
#include "appio.h"
#include "hardware/cm1602.h"
#include <string.h>
#include <TCPIP Stack/TCPIP.h>

static char s_lastErr[8];

static void createDisplaySink(void);
static void destroyDisplaySink(void);
static void pollDisplaySink(void);

#define DISPLAY_SINK_PORT (SINK_DISPLAY_TYPE + BASE_SINK_PORT)
const rom Sink g_displaySink = { 
							 SINK_DISPLAY_TYPE,
                             0, 
                             DISPLAY_SINK_PORT,
                             &createDisplaySink,
                             &destroyDisplaySink,
                             &pollDisplaySink
						 };

// The TCP client socket of display listener
static TCP_SOCKET s_listenerSocket = INVALID_SOCKET;

static void _clr(BYTE addr)
{
	char i;
	cm1602_setDdramAddr(addr);
	for (i = 0; i < 16; i++)
	{
		cm1602_write(' ');
	}
	cm1602_setDdramAddr(addr);
}

static void _print(const rom char* str, BYTE addr)
{
	_clr(addr);
	cm1602_writeStr(str);
	ClrWdt();
}

static void _printr(const ram char* str, BYTE addr)
{
	_clr(addr);
	cm1602_writeStrRam(str);
	ClrWdt();
}

void println(const rom char* str)
{
	_print(str, 0x40);	
}

void printlnr(const ram char* str)
{
	_printr(str, 0x40);	
}

void printlnUp(const rom char* str)
{
	_print(str, 0x00);	
}

void printlnrUp(const ram char* str)
{
	_printr(str, 0x00);	
}

void fatal(const rom char* str)
{
	strncpypgm2ram(s_lastErr, str, sizeof(s_lastErr));
	Reset();
}

const ram char* getLastFatal()
{
	return s_lastErr;
}

static void createDisplaySink()
{
	// Open the sever TCP channel
	s_listenerSocket = TCPOpen(0, TCP_OPEN_SERVER, DISPLAY_SINK_PORT, TCP_PURPOSE_GENERIC_TCP_SERVER);
	if (s_listenerSocket == INVALID_SOCKET)
	{
		fatal("DSP_SRV");
	}
}

static void destroyDisplaySink()
{
	if (s_listenerSocket != INVALID_SOCKET)
	{
		TCPClose(s_listenerSocket);
	}
}

static void pollDisplaySink()
{
	WORD s;
	if (!TCPIsConnected(s_listenerSocket))
	{
		return;
	}

	s = TCPIsGetReady(s_listenerSocket);
	if (s > sizeof(unsigned short))
	{
		char buffer[16];
		if (s > 15) 
			s = 15;
		TCPGetArray(s_listenerSocket, (BYTE*)buffer, s);
		buffer[s] = '\0';
		TCPDiscard(s_listenerSocket);
		// Write it
		printlnrUp(buffer);
	}
}
