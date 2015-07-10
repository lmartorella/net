#include "ip_protocol.h"
#include "appio.h"
#include "displaySink.h"
#include "audioSink.h"
#include "persistence.h"
#include "hardware/cm1602.h"
#include "hardware/fuses.h"
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

/*
	Peer descriptor item
__PACK typedef struct
{
	WORD deviceId;
	WORD deviceCaps;
	WORD devicePort;
} PEER_DESCRIPTOR;
*/

/*
	REGISTER message
__PACK typedef struct
{
	char preamble[4];
	WORD peerCount;
	//PEER_DESCRIPTOR peerDescs[0];
} SERVER_REGISTER;
*/

/*
typedef enum
{
    RGST_OK = 0,
    RGST_UNKNOWN_MESSAGE = 1,
    RGST_ERRCODE_NEWGUID = 2,
    RGST_UNKNOWN_SINKTYPE = 3,
    RGST_UNKNOWN_ADDRESS = 4
} RGST_ERRCODE_t;
*/
/*
	REGISTER response
__PACK typedef struct
{
    union
    {
	RGST_ERRCODE_t errCode;
        UINT16 val;
    };
} SERVER_REGISTER_RESPONSE;
__PACK typedef struct
{
	GUID newGuid;
} SERVER_REGISTER_NEWGUID_RESPONSE;
*/

static void sendHelo(void);
static void pollControlPort(void);
//static void waitForRegisterConnection(void);
//static void waitForRegisterResponse(void);

//static char s_errMsg[6] = { 0 };

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
/*        unsigned int i;
        switch (s_protState)
        {
        case STATE_HELO:
            checkHelo();
            break;
        case STATE_REGISTER_CONNECTING:
            waitForRegisterConnection();
            break;
        case STATE_REGISTER_ACK:
            waitForRegisterResponse();
            break;
        case STATE_REGISTERED:
        case STATE_REGISTERED_NEW_GUID:
            for (i = 0; i < AllSinksCount; i++)
            {
                AllSinks[i]->pollHandler();
            }
        }
 * */
    }
}

static void CLOS_command()
{
    // Returns in listening state
    TCPDiscard(s_controlSocket);
    TCPDisconnect(s_controlSocket);
}

static void CHIL_command()
{
    // Only 1 children: me
    TCPPutW(s_controlSocket, 1);
	TCPPutArray(s_controlSocket, (BYTE*)(&g_persistentData.deviceId), sizeof(GUID));
    TCPFlush(s_controlSocket);
}

static void peekCommand()
{
    char msg[4];
    TCPGetArray(s_controlSocket, (BYTE*)&msg, sizeof(msg));
    if (strncmp(msg, "CLOS", 4) == 0) {
        CLOS_command();
    } 
    else if (strncmp(msg, "CHIL", 4) == 0) {
        CHIL_command();
    } 
    else {
        fatal("CMD.unkn");
    }
}

static void pollControlPort()
{
    unsigned short s;
    if (!TCPIsConnected(s_controlSocket))
	{
		return;
	}

    s = TCPIsGetReady(s_controlSocket);
	if (s >= 4)
	{
        peekCommand();
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

/*
static void checkHelo()
{
	// Socket opened
	// Check if HOME socket answered
	WORD l = UDPIsGetReady(s_homeSocket);
	if (l >= sizeof(HOME_RESPONSE))
	{
		HOME_RESPONSE response;

		UDPGetArray((BYTE*)&response, sizeof(HOME_RESPONSE));
		UDPDiscard();

		// Have HOME
		if (memcmp(response.preamble, "HOMEHERE", 8) == 0)
		{
			// Now contact the server!
			s_homeIp = response.homeIp;
			s_homePort = response.homePort;

			UDPClose(s_heloSocket);
			UDPClose(s_homeSocket);

			// Open the client TCP channel
			s_serverSocket = TCPOpen(s_homeIp.Val, TCP_OPEN_IP_ADDRESS, s_homePort, TCP_PURPOSE_GENERIC_TCP_CLIENT);
			if (INVALID_SOCKET == s_serverSocket)
			{
				fatal("SRV.opn1");
			}
			s_protState = STATE_REGISTER_CONNECTING;
			return;
		}
		else
		{
			fatal("HELO.rcv");
		}
	}
}
 */

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

/*
static void waitForRegisterConnection()
{
	if (TCPIsConnected(s_serverSocket))
	{
		unsigned int i;
		// Connected? Then Register.
		if (TCPIsPutReady(s_serverSocket) < sizeof(SERVER_REGISTER))
		{
			fatal("SRV.rdy");
		}

		// Preamble
		TCPPutArray(s_serverSocket, (BYTE*)"RGST", 4);

		// Put device count (2, DISPLAY and FLASHER)
                i = AllSinksCount;
		TCPPutArray(s_serverSocket, (BYTE*)&i, sizeof(unsigned int));

		for (i = 0; i < AllSinksCount; i++)
		{
			const Sink* sink = AllSinks[i]; 
			unsigned int port = BASE_SINK_PORT + sink->deviceId;
			// Put device ID
			TCPPutROMArray(s_serverSocket, (BYTE*)&sink->deviceId, sizeof(unsigned int));
			// Put device CAPS
			TCPPutROMArray(s_serverSocket, (BYTE*)&sink->caps, sizeof(unsigned int));
			// Put device CAPS
			TCPPutArray(s_serverSocket, (BYTE*)&port, sizeof(unsigned int));

			// start sink
			sink->createHandler();
		}

		TCPFlush(s_serverSocket);

		s_protState = STATE_REGISTER_ACK;
	}
}

static void waitForRegisterResponse()
{
	// Wait for data
	WORD size = TCPIsGetReady(s_serverSocket);
	if (size >= sizeof(SERVER_REGISTER_RESPONSE))
	{
		SERVER_REGISTER_RESPONSE response;
		TCPGetArray(s_serverSocket, (BYTE*)&response, sizeof(response));

		switch (response.errCode)
		{
                case RGST_OK:
                    s_protState = STATE_REGISTERED;
                    break;
                case RGST_ERRCODE_NEWGUID:
                    if (size < sizeof(SERVER_REGISTER_RESPONSE) + sizeof(SERVER_REGISTER_NEWGUID_RESPONSE))
                    {
                        fatal("RgRespSz");
                    }
                    // Read the GUID response
                    SERVER_REGISTER_NEWGUID_RESPONSE data;
                    PersistentData persistence;
                    TCPGetArray(s_serverSocket, (BYTE*)&data, sizeof(data));

                    boot_getUserData(&persistence);
                    persistence.deviceId = data.newGuid;
                    // Have new GUID! Program it.
                    boot_updateUserData(&persistence);

                    s_protState = STATE_REGISTERED_NEW_GUID;
                    break;
                default:
                    sprintf(s_errMsg, "RG:%x", (int)response.errCode);
                    break;
                }

		TCPClose(s_serverSocket);
	}
}
 */

#endif // HAS_IP
