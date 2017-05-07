#include "pch.h"
#include "protocol.h"
#include "appio.h"
#include "displaySink.h"
#include "audioSink.h"
#include "hardware/cm1602.h"
#include "ip_client.h"
#include "persistence.h"

#if defined(_IS_ETH_CARD) || defined(HAS_IP)
#include "Compiler.h"
#include "TCPIPStack/TCPIP.h"
APP_CONFIG AppConfig;
#endif

#ifdef HAS_IP
#define SERVER_CONTROL_UDP_PORT 17007
#define CLIENT_TCP_PORT 20000

// UDP broadcast socket
static UDP_SOCKET s_heloSocket;  
// TCP lister control socket
static TCP_SOCKET s_controlSocket;
static bit s_started = FALSE;

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

void prot_control_close()
{
}

// Close the control port listener
void prot_control_abort()
{
    // Returns in listening state
    TCPDiscard(s_controlSocket);
    TCPDisconnect(s_controlSocket);
}

bit prot_control_readW(WORD* w)
{
    WORD l = TCPIsGetReady(s_controlSocket);
	if (l < 2) 
		return FALSE;
	TCPGetArray(s_controlSocket, (BYTE*)w, sizeof(WORD));
	return TRUE;
}

bit prot_control_read(void* data, WORD size)
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

// Flush and OVER to other party. TCP is full duplex, so OK to only flush
void prot_control_over()
{
    TCPFlush(s_controlSocket);
}

bit prot_control_isConnected()
{
    return s_started && TCPIsConnected(s_controlSocket);
}

WORD prot_control_readAvail()
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
		fatal("SOC.opn1");
	}

    // Open the sever TCP channel
	s_controlSocket = TCPOpen(0, TCP_OPEN_SERVER, CLIENT_TCP_PORT, TCP_PURPOSE_GENERIC_TCP_SERVER);
	if (s_controlSocket == INVALID_SOCKET)
	{
		fatal("SOC.opn2");
	}
}

/*
	Manage slow timer (heartbeats)
*/
void ip_prot_slowTimer()
{
    char buffer[16];
    int dhcpOk;

    dhcpOk = DHCPIsBound(0) != 0;

    if (dhcpOk != s_started)
    {
            if (dhcpOk)
            {
                    unsigned char* p = (unsigned char*)(&AppConfig.MyIPAddr);
                    sprintf(buffer, "%d.%d.%d.%d", (int)p[0], (int)p[1], (int)p[2], (int)p[3]);
                    cm1602_setDdramAddr(0x0);
                    cm1602_writeStr(buffer);
                    s_started = TRUE;
            }
            else
            {
                    s_started = FALSE;
                    fatal("DHCP.nok");
            }
    }
    if (s_started)
    {
        // Ping server every second
        sendHelo();
    }
}

static void sendHelo()
{
	// Still no HOME? Ping HELO
	if (UDPIsPutReady(s_heloSocket) < sizeof(HOME_REQUEST))
	{
		fatal("HELO.rdy");
	}

	UDPPutString("HOME");
	UDPPutString(prot_registered ? (bus_hasDirtyChildren ? "CCHN" : "HTBT") : "HEL4");
	UDPPutArray((BYTE*)(&pers_data.deviceId), sizeof(GUID));
	UDPPutW(CLIENT_TCP_PORT);
    if (prot_registered && bus_hasDirtyChildren) {
        UDPPutW(BUFFER_MASK_SIZE);
        UDPPutArray(bus_dirtyChildren, BUFFER_MASK_SIZE);
    }
	UDPFlush();   
}

#endif // HAS_IP
