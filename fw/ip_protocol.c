#include "pch.h"
#include "protocol.h"
#include "appio.h"
#include "displaySink.h"
#include "audioSink.h"
#include "persistence.h"
#include "hardware/cm1602.h"
#include "hardware/rs485.h"
#include "ip_protocol.h"

#ifdef HAS_IP
#include "Compiler.h"
#include "TCPIPStack/TCPIP.h"

static BOOL s_registered = FALSE;
static BYTE s_slowDemux = 0;

static DWORD s_1sec;
static DWORD s_10msec;
#define TICKS_1S ((DWORD)TICK_SECOND)
#define TICKS_10MS (DWORD)(TICKS_1S / 100)

// UDP broadcast socket
static UDP_SOCKET s_heloSocket;  
// TCP lister control socket
static TCP_SOCKET s_controlSocket;
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

static void sendHelo(BOOL isHeartbeat);
static void pollControlPort(void);
static int s_inReadSink = -1;

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
    // If I remove & from here, ip_control_read stop working!!
    TCPPutArray(s_controlSocket, (BYTE*)data, size);
}

static void ip_control_flush()
{
    TCPFlush(s_controlSocket);
}

static BOOL ip_control_isListening()
{
    return TCPIsConnected(s_controlSocket);
}

static WORD ip_control_getDataSize()
{
    return TCPIsGetReady(s_controlSocket);
}

static void SELE_command()
{
    s_registered = TRUE;
 
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
    GUID guid;
    memcpy(&guid, &g_persistentData.deviceId, sizeof(GUID));
    
    // Only 1 children: me
    ip_control_writeW(1);
    ip_control_write(&guid, sizeof(GUID));
    ip_control_flush();
}

static void SINK_command()
{
    FOURCC fourcc;
    int i = AllSinksSize;
    ip_control_writeW(i);
    while (i > 0)
    {
        i--;
        const Sink* sink = AllSinks[i]; 
        memcpy(&fourcc, &sink->fourCc, sizeof(FOURCC));
        
        // Put device ID
        ip_control_write(&fourcc, sizeof(FOURCC));
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

static void READ_command()
{
    WORD sink;
    if (!ip_control_readW(&sink))
    {
        fatal("READ.undr");
    }

    // Address sink
    AllSinks[sink]->writeHandler();
    ip_control_flush();
}

static void readSink()
{
    if (!AllSinks[s_inReadSink]->readHandler())
    {
        s_inReadSink = -1;
    }
}

static void WRIT_command()
{
    // Get sink# and msg size
    if (!ip_control_readW((WORD*)&s_inReadSink))
    {
        fatal("WRIT.undr");
    }
    // Address sink
    readSink();
}

// TODO: Limitation: both the command and its data should be in the read buffer
// at the same time
static void parseCommandAndData()
{
    FOURCC msg;
    ip_control_read(&msg, sizeof(FOURCC));
	switch (msg.str[0])
	{
		case 'C':
			if (strncmp(msg.str + 1, "LOS", 3) == 0) {
				CLOS_command();
                return;
			} 
			else if (strncmp(msg.str + 1, "HIL", 3) == 0) {
				CHIL_command();
                return;
			} 
			break;
		case 'S':
			if (strncmp(msg.str + 1, "ELE", 3) == 0) {
				SELE_command();
                return;
			} 
			else if (strncmp(msg.str + 1, "INK", 3) == 0) {
				SINK_command();
                return;
			} 
			break;
        case 'R':
			if (strncmp(msg.str + 1, "EAD", 3) == 0) {
				READ_command();
                return;
			} 
            break;
        case 'W':
			if (strncmp(msg.str + 1, "RIT", 3) == 0) {
				WRIT_command();
                return;
			} 
            break;
		default:
			if (strncmp(msg.str, "GUID", 4) == 0) {
				GUID_command();
                return;
			} 
    }
    fatal("CMD.unkn");
}

void ip_prot_timer()
{
    // Update ETH module timers at ~23Khz freq
     TickUpdate();
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

    // Init ETH Ticks on timer0 (low prio) module
    TickInit();
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

    // Align 1sec to now()
    s_1sec = s_10msec = TickGet();
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
        unsigned short s;
        if (!ip_control_isListening())
        {
            return;
        }

        s = ip_control_getDataSize();
        if (s_inReadSink >= 0) 
        {
            readSink();
        } 
        else if (s >= 4) // Minimum msg size
        {
            // This can even peek only one command.
            // Until not closed by server, or CLOS command sent, the channel can stay open.
            parseCommandAndData();
        }
        // Otherwise wait for data
    }
}

/*
	Manage slow timer (state transitions)
*/
static void slowTimer()
{
    if (!s_registered) 
    {
        // Ping server every second
        sendHelo(FALSE);
    }
    else 
    {
        s_slowDemux++;
        // Ping server every 4 seconds
        if ((s_slowDemux % 4) == 0)
        {
            sendHelo(TRUE);
        }
    }
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
        slowTimer();
    }
}

TIMER_RES ip_timers_check()
{
    TIMER_RES res;
    res.v = 0;
    DWORD now = TickGet();
    if ((now - s_1sec) >= TICKS_1S)
    {
        s_1sec = now;
        res.timer_1s = 1;
    }
    if ((now - s_10msec) >= TICKS_10MS)
    {
        s_10msec = now;
        res.timer_10ms = 1;
    }
    return res;
}

static void sendHelo(BOOL isHeartbeat)
{
	// Still no HOME? Ping HELO
	if (UDPIsPutReady(s_heloSocket) < sizeof(HOME_REQUEST))
	{
		fatal("HELO.rdy");
	}

	UDPPutString("HOME");
	UDPPutString(isHeartbeat ? "HTBT" : "HEL3");
	UDPPutArray((BYTE*)(&g_persistentData.deviceId), sizeof(GUID));
	UDPPutW(CLIENT_TCP_PORT);
	UDPFlush();
    
    rs485_write(1, "Hello world!", 12);
}

BOOL prot_control_readW(WORD* w)
{
    return ip_control_readW(w);
}

BOOL prot_control_read(void* data, WORD size)
{
    return ip_control_read(data, size);
}

void prot_control_writeW(WORD w)
{
    ip_control_writeW(w);
}

void prot_control_write(void* data, WORD size)
{
    ip_control_write(data, size);
}

#endif // HAS_IP
