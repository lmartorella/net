#include "hardware/fuses.h"
#include "protocol.h"
#include "appio.h"
#include "persistence.h"
#include <TCPIP Stack/TCPIP.h>

static UDP_SOCKET s_heloSocket;
PROTOCOL_STATUS s_protStatus = 0;

#define HomeProtocolPort 17007

// Send HELO packet to the helo port
void prot_sendHelo()
{
	int i;
	if (!(s_protStatus & PROT_HELO_OPENED))
	{
		s_heloSocket = UDPOpenEx(NULL, UDP_OPEN_NODE_INFO, 0, HomeProtocolPort);
		if (s_heloSocket == INVALID_UDP_SOCKET)
		{
			error("HELO.open");
			return;
		}
		s_protStatus |= PROT_HELO_OPENED;
	}

	// Socket opened
	if (UDPIsPutReady(s_heloSocket) < (16 + 8))
	{
		error("HELO.rdy");
		return;
	}

	UDPPutROMString("HOMEHELO");
	UDPPutROMArray((rom BYTE*)&g_persistentData.deviceId, sizeof(GUID));
	UDPFlush();
}
