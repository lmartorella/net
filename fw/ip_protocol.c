#include "ip_protocol.h"
#include "appio.h"
#include "displaySink.h"
#include "audioSink.h"
#include "persistence.h"
#include "hardware/fuses.h"
#include "hardware/cm1602.h"
#include "TCPIPStack/TCPIP.h"
#include "Compiler.h"

#ifdef HAS_IP

// UDP broadcast socket
static UDP_SOCKET s_heloSocket;
static TCP_SOCKET s_controlSocket;

static BOOL s_registered = FALSE;
static BOOL s_dhcpOk = FALSE;

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

static void sendHelo(void);
static void pollControlPort(void);

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
        pollControlPort();
    }
}

// Close the control port listener
static void ip_control_close()
{
    // Returns in listening state
    TCPDiscard(s_controlSocket);
    TCPDisconnect(s_controlSocket);
}

static void CLOS_command()
{
	ip_control_close();
}

static BOOL ip_control_readW(WORD* w)
{
    WORD l = TCPIsGetReady(s_controlSocket);
	if (l < 2) 
		return FALSE;
	TCPGetArray(s_controlSocket, (BYTE*)w, sizeof(WORD));
	return TRUE;
}

static BOOL ip_control_read(void* data, WORD size)
{
    WORD l = TCPIsGetReady(s_controlSocket);
	if (l < size)
		return FALSE;
	TCPGetArray(s_controlSocket, (BYTE*)data, size);
	return TRUE;
}

static void ip_control_writeW(WORD w)
{
    TCPPutArray(s_controlSocket, (BYTE*)&w, sizeof(WORD));
}

static void ip_control_write(void* data, WORD size)
{
    TCPPutArray(s_controlSocket, (BYTE*)&data, size);
}

static void ip_control_writeROM(ROM void* data, WORD size)
{
    TCPPutROMArray(s_controlSocket, (ROM BYTE*)&data, size);
}

static void ip_control_flush()
{
    TCPFlush(s_controlSocket);
}

static void SELE_command()
{
    // Select subnode. Ignores it.
    WORD w;
    if (!ip_control_readW(&w)) {
        fatal("SELE.undr");
    }
    // Select subnode.
    // TODO: Ignore when no subnodes
}

static void CHIL_command()
{
    // Only 1 children: me
    ip_control_writeW(1);
    ip_control_write(&g_persistentData.deviceId, sizeof(GUID));
    ip_control_flush();
}

const rom Sink* AllSinks[] = { &g_displaySink };
#define AllSinksSize 1

static void SINK_command()
{
    int i = AllSinksSize;
    ip_control_writeW(i);

    for (; i >= 0; i--)
    {
        const Sink* sink = AllSinks[i]; 
        // Put device ID
        ip_control_writeROM(&sink->fourCc, 4);
    }
    ip_control_flush();
}

static void GUID_command()
{
    GUID guid;
    PersistentData persistence;
    
    if (!ip_control_read(&guid, sizeof(GUID))) {
        fatal("GUID.undr");
    }

    boot_getUserData(&persistence);
    persistence.deviceId = guid;
    // Have new GUID! Program it.
    boot_updateUserData(&persistence);
}

// TODO: Limitation: both the command and its data should be in the read buffer
// at the same time
static void parseCommandAndData()
{
    char msg[4];
    ip_control_read(&msg, sizeof(msg));
	switch (msg[0])
	{
		case 'C':
			if (strncmp(msg + 1, "LOS", 3) == 0) {
				CLOS_command();
                return;
			} 
			else if (strncmp(msg + 1, "HIL", 3) == 0) {
				CHIL_command();
                return;
			} 
			break;
		case 'S':
			if (strncmp(msg + 1, "ELE", 3) == 0) {
				SELE_command();
                return;
			} 
			else if (strncmp(msg + 1, "INK", 3) == 0) {
				SINK_command();
                return;
			} 
			break;
		default:
			if (strncmp(msg, "GUID", 4) == 0) {
				GUID_command();
                return;
			} 
    }
    fatal("CMD.unkn");
}

static BOOL ip_control_isListening()
{
    return TCPIsConnected(s_controlSocket);
}

static WORD ip_control_getDataSize()
{
    return TCPIsGetReady(s_controlSocket);
}

static void pollControlPort()
{
    unsigned short s;
    if (!ip_control_isListening())
	{
		return;
	}

    s = ip_control_getDataSize();
	if (s >= 4) // Minimum msg size
	{
		// This can even peek only one command.
		// Until not closed by server, or CLOS command sent, the channel can stay open.
        parseCommandAndData();
    }
    // Otherwise wait for data
}

/*
	Manage slow timer (state transitions)
*/
void ip_prot_slowTimer()
{
    char buffer[16];
    int dhcpOk;
    println("");

    dhcpOk = DHCPIsBound(0) != 0;

    if (dhcpOk != s_dhcpOk)
    {
            if (dhcpOk)
            {
                    unsigned char* p = (unsigned char*)(&AppConfig.MyIPAddr);
                    sprintf(buffer, "%d.%d.%d.%d", (int)p[0], (int)p[1], (int)p[2], (int)p[3]);
                    cm1602_setDdramAddr(0x0);
                    cm1602_writeStr(buffer);
                    s_dhcpOk = TRUE;
            }
            else
            {
                    s_dhcpOk = FALSE;
                    fatal("DHCP.nok");
            }
    }
    if (s_dhcpOk)
    {
        //char buffer[16];
        // Ping server every second
        sendHelo();

        //sprintf(buffer, "STA:%x,%s", (int)s_protState, s_errMsg);
        //println(buffer);
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
	UDPPutString(s_registered ? "HTBT" : "HEL3");
	UDPPutArray((BYTE*)(&g_persistentData.deviceId), sizeof(GUID));
	UDPPutW(CLIENT_TCP_PORT);
	UDPFlush();
}

#endif // HAS_IP
