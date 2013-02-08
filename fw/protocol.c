#include "protocol.h"
#include "appio.h"
#include "persistence.h"
#include "hardware/cm1602.h"
#include "hardware/fuses.h"
#include <TCPIP Stack/TCPIP.h>

// Protocol Engine State
typedef enum PROTOCOL_STATE_enum
{	
	// Reset
	STATE_NOT_INITIALIZED,
	// HELO mode: send UDP packets and waits for HOME response
	STATE_HELO,
	// Helo closed, now registering to the server
	STATE_REGISTER_CONNECTING,
	// Registering sent, waiting for ACK
	STATE_REGISTER_ACK,
	// OK, registered
	STATE_REGISTERED,
	// OK, registered
	STATE_REGISTERED_NEW_GUID
} PROTOCOL_STATE;

static PROTOCOL_STATE s_protState = STATE_NOT_INITIALIZED;

// UDP broadcast socket
#define HeloProtocolPort 17007
static UDP_SOCKET s_heloSocket;
// UDP home listen socket
#define HomeProtocolPort 17008
static UDP_SOCKET s_homeSocket;

// The home IP
static IP_ADDR s_homeIp;
// The home Port
static WORD s_homePort;
// The TCP client socket with home
TCP_SOCKET s_serverSocket;

/*
	HOME Response packet
*/
typedef struct __attribute__((__packed__))
{
	char preamble[8];
	IP_ADDR homeIp;
	WORD homePort;
} HOME_RESPONSE;

/*
	HOME request
*/
typedef struct __attribute__((__packed__))
{
	char preamble[8];
	GUID device;
} HOME_REQUEST;

/*
	Peer descriptor item
*/
typedef struct __attribute__((__packed__))
{
	WORD deviceId;
	WORD deviceCaps;
	WORD devicePort;
} PEER_DESCRIPTOR;

/*
	REGISTER message
*/
typedef struct __attribute__((__packed__))
{
	char preamble[4];
	WORD peerCount;
	//PEER_DESCRIPTOR peerDescs[0];
} SERVER_REGISTER;

typedef UINT16 RGST_ERRCODE;
#define RGST_ERRCODE_NEWGUID 2

/*
	REGISTER response
*/
typedef struct __attribute__((__packed__))
{
	RGST_ERRCODE errCode;
} SERVER_REGISTER_RESPONSE;
typedef struct __attribute__((__packed__))
{
	GUID newGuid;
} SERVER_REGISTER_NEWGUID_RESPONSE;

static void createUdpSockets(void);
static void checkHelo(void);
static void sendHelo(void);
static void waitForRegisterConnection(void);
static void waitForRegisterResponse(void);

/*
	Manage POLLs (read buffers)
*/
void prot_poll()
{
	switch (s_protState)
	{
	case STATE_NOT_INITIALIZED:
		createUdpSockets();
		break;
	case STATE_HELO:
		checkHelo();
		break;
	case STATE_REGISTER_CONNECTING:
		waitForRegisterConnection();
		break;
	case STATE_REGISTER_ACK:
		waitForRegisterResponse();
		break;
	}
}

/*
	Manage slow timer (state transitions)
*/
void prot_slowTimer()
{
	char buffer[16];

	switch (s_protState)
	{
	case STATE_HELO:
		sendHelo();
		break;
	}

	sprintf(buffer, "STA:%x", (int)s_protState);
	printlnr(buffer);
}

static void createUdpSockets()
{
	s_heloSocket = UDPOpenEx(NULL, UDP_OPEN_NODE_INFO, 0, HeloProtocolPort);
	if (s_heloSocket == INVALID_UDP_SOCKET)
	{
		fatal("HELO.opn1");
	}
	s_homeSocket = UDPOpenEx(NULL, UDP_OPEN_SERVER, HomeProtocolPort, 0);
	if (s_homeSocket == INVALID_UDP_SOCKET)
	{
		fatal("HELO.opn2");
	}
	s_protState = STATE_HELO;
}

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
		if (strncmppgm2ram(response.preamble, "HOMEHERE", 8) == 0)
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

static void sendHelo()
{
	// Still no HOME? Ping HELO
	if (UDPIsPutReady(s_heloSocket) < sizeof(HOME_REQUEST))
	{
		fatal("HELO.rdy");
	}

	UDPPutROMString("HOMEHELO");
	UDPPutROMArray((rom BYTE*)&g_persistentData.deviceId, sizeof(GUID));
	UDPFlush();
}

static void waitForRegisterConnection()
{
	if (TCPIsConnected(s_serverSocket))
	{
		// Connected? Then Register.
		if (TCPIsPutReady(s_serverSocket) < sizeof(SERVER_REGISTER))
		{
			fatal("SRV.rdy");
		}

		// Preamble
		TCPPutROMArray(s_serverSocket, (rom BYTE*)"RGST", 4);
		// Put device count (0)
		TCPPut(s_serverSocket, 0);
		TCPPut(s_serverSocket, 0);
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

		if (response.errCode == RGST_ERRCODE_NEWGUID)
		{
			// Read the GUID response
			SERVER_REGISTER_NEWGUID_RESPONSE data;
			PersistentData persistence;
			TCPGetArray(s_serverSocket, (BYTE*)&data, sizeof(data));
			
			boot_getUserData(&persistence);
			persistence.deviceId = data.newGuid;
			// Have new GUID! Program it.
			boot_updateUserData(&persistence);

			s_protState = STATE_REGISTERED_NEW_GUID;
		}
		else
		{
			s_protState = STATE_REGISTERED;
		}

		TCPClose(s_serverSocket);
	}
}
