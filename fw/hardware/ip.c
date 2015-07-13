#include "fuses.h"
#include "ip.h"
#include "../ip_protocol.h"
#include "../TCPIPStack/TCPIP.h"
#include "../appio.h"

#ifdef HAS_IP

// UDP broadcast socket
UDP_SOCKET s_heloSocket;  // TODO: static
// TCP lister control socket
static TCP_SOCKET s_controlSocket;
static BOOL s_dhcpOk = FALSE;

APP_CONFIG AppConfig;

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

    // Start IP
    DHCPInit(0);
    DHCPEnable(0);

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
	Manage POLLs (read buffers)
*/
void ip_prot_poll()
{
    // Do ETH stuff
    StackTask();
    // This tasks invokes each of the core stack application tasks
    StackApplications();
    if (s_dhcpOk)
    {
        prot_pollControlPort();
    }
}

// Close the control port listener
void ip_control_close()
{
    // Returns in listening state
    TCPDiscard(s_controlSocket);
    TCPDisconnect(s_controlSocket);
}

BOOL ip_control_readW(WORD* w)
{
    WORD l = TCPIsGetReady(s_controlSocket);
	if (l < 2) 
		return FALSE;
	TCPGetArray(s_controlSocket, (BYTE*)w, sizeof(WORD));
	return TRUE;
}

BOOL ip_control_read(void* data, WORD size)
{
    WORD l = TCPIsGetReady(s_controlSocket);
	if (l < size)
		return FALSE;
	TCPGetArray(s_controlSocket, (BYTE*)data, size);
	return TRUE;
}

void ip_control_writeW(WORD w)
{
    TCPPutArray(s_controlSocket, (BYTE*)&w, sizeof(WORD));
}

void ip_control_write(void* data, WORD size)
{
    TCPPutArray(s_controlSocket, (BYTE*)&data, size);
}

void ip_control_writeROM(ROM void* data, WORD size)
{
    TCPPutROMArray(s_controlSocket, (ROM BYTE*)&data, size);
}

void ip_control_flush()
{
    TCPFlush(s_controlSocket);
}

BOOL ip_control_isListening()
{
    return TCPIsConnected(s_controlSocket);
}

WORD ip_control_getDataSize()
{
    return TCPIsGetReady(s_controlSocket);
}

#endif