#include "pch.h"
#include "protocol.h"
#include "appio.h"
#include "displaySink.h"
#include "audioSink.h"
#include "hardware/cm1602.h"
#include "ip_client.h"
#include "persistence.h"

#ifdef HAS_IP
#include "Compiler.h"
#include "TCPIPStack/TCPIP.h"

#define SERVER_CONTROL_UDP_PORT 17007
#define CLIENT_TCP_PORT 20000

// UDP broadcast socket
static UDP_SOCKET s_heloSocket;  
// TCP lister control socket
static TCP_SOCKET s_controlSocket;
BOOL prot_started = FALSE;

APP_CONFIG AppConfig;

/*
	HOME request
*/
__PACK typedef struct
{
	char preamble[4];
	char messageType[4];
	GUID device;
	WORD controlPort;
} HOME_REQUEST;

static void sendHelo();
static void pollControlPort();

// Close the control port listener
void prot_control_close()
{
    // Returns in listening state
    TCPDiscard(s_controlSocket);
    TCPDisconnect(s_controlSocket);
}

BOOL prot_control_readW(WORD* w)
{
    WORD l = TCPIsGetReady(s_controlSocket);
	if (l < 2) 
		return FALSE;
	TCPGetArray(s_controlSocket, (BYTE*)w, sizeof(WORD));
	return TRUE;
}

BOOL prot_control_read(void* data, WORD size)
{
    WORD l = TCPIsGetReady(s_controlSocket);
	if (l < size)
		return FALSE;
	TCPGetArray(s_controlSocket, (BYTE*)data, size);
	return TRUE;
}

void prot_control_writeW(WORD w)
{
    TCPPutArray(s_controlSocket, (BYTE*)&w, sizeof(WORD));
}

void prot_control_write(const void* data, WORD size)
{
    // If I remove & from here, ip_control_read stop working!!
    TCPPutArray(s_controlSocket, (const BYTE*)data, size);
}

void prot_control_flush()
{
    TCPFlush(s_controlSocket);
}

BOOL prot_control_isListening()
{
    return TCPIsConnected(s_controlSocket);
}

WORD prot_control_getDataSize()
{
    return TCPIsGetReady(s_controlSocket);
}

void ip_prot_init()
{
    println("IP/DHCP");
    memset(&AppConfig, 0, sizeof(AppConfig));
    AppConfig.Flags.bIsDHCPEnabled = 1;
    AppConfig.MyMACAddr.v[0] = MY_DEFAULT_MAC_BYTE1;
    AppConfig.MyMACAddr.v[1] = MY_DEFAULT_MAC_BYTE2;
    AppConfig.MyMACAddr.v[2] = MY_DEFAULT_MAC_BYTE3;
    AppConfig.MyMACAddr.v[3] = MY_DEFAULT_MAC_BYTE4;
    AppConfig.MyMACAddr.v[4] = MY_DEFAULT_MAC_BYTE5;
    AppConfig.MyMACAddr.v[5] = MY_DEFAULT_MAC_BYTE6;

    // Init ETH loop data
    StackInit();  

	s_heloSocket = UDPOpenEx(NULL, UDP_OPEN_NODE_INFO, 0, SERVER_CONTROL_UDP_PORT);
	if (s_heloSocket == INVALID_UDP_SOCKET)
	{
		fatal("SOCK.opn1");
	}

    // Open the sever TCP channel
	s_controlSocket = TCPOpen(0, TCP_OPEN_SERVER, CLIENT_TCP_PORT, TCP_PURPOSE_GENERIC_TCP_SERVER);
	if (s_controlSocket == INVALID_SOCKET)
	{
		fatal("SOCK.opn2");
	}
}

/*
	Manage slow timer (state transitions)
*/
void prot_slowTimer()
{
    char buffer[16];
    int dhcpOk;

    dhcpOk = DHCPIsBound(0) != 0;

    if (dhcpOk != prot_started)
    {
            if (dhcpOk)
            {
                    unsigned char* p = (unsigned char*)(&AppConfig.MyIPAddr);
                    sprintf(buffer, "%d.%d.%d.%d", (int)p[0], (int)p[1], (int)p[2], (int)p[3]);
                    cm1602_setDdramAddr(0x0);
                    cm1602_writeStr(buffer);
                    prot_started = TRUE;
            }
            else
            {
                    prot_started = FALSE;
                    fatal("DHCP.nok");
            }
    }
    if (prot_started)
    {
        // Ping server every second
        sendHelo();
    }
}

static void sendHelo()
{
    PersistentData persistence;
    boot_getUserData(&persistence);

	// Still no HOME? Ping HELO
	if (UDPIsPutReady(s_heloSocket) < sizeof(HOME_REQUEST))
	{
		fatal("HELO.rdy");
	}

	UDPPutString("HOME");
	UDPPutString(prot_registered ? "HTBT" : "HEL3");
	UDPPutArray((BYTE*)(&persistence.deviceId), sizeof(GUID));
	UDPPutW(CLIENT_TCP_PORT);
	UDPFlush();   
}

#endif // HAS_IP
