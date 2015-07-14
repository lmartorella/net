#include "protocol.h"
#include "appio.h"
#include "displaySink.h"
#include "audioSink.h"
#include "persistence.h"
#include "hardware/fuses.h"
#include "hardware/cm1602.h"
#include "hardware/ip.h"
#include "Compiler.h"
#include "TCPIPStack/TCPIP.h"

#ifdef HAS_IP

static BOOL s_registered = FALSE;

const rom Sink* AllSinks[] = { &g_displaySink };
#define AllSinksSize 1


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

static void CLOS_command()
{
	ip_control_close();
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

static BYTE buf[SINK_BUFFER_SIZE];

static void READ_command()
{
    // Get sink# and msg size
    WORD sink, length;

    if (!ip_control_readW(&sink) || !ip_control_readW(&length) || !ip_control_read(buf, length))
    {
        fatal("READ.undr");
    }
    // Address sink
    AllSinks[sink]->readHandler(buf, length);
}

static void WRIT_command()
{
    WORD sink, length;

    if (!ip_control_readW(&sink))
    {
        fatal("WRIT.undr");
    }
    // Address sink
    length = AllSinks[sink]->writeHandler(buf);
    ip_control_writeW(length);
    ip_control_write(buf, length);
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
        case 'R':
			if (strncmp(msg + 1, "EAD", 3) == 0) {
				READ_command();
                return;
			} 
            break;
        case 'W':
			if (strncmp(msg + 1, "RIT", 3) == 0) {
				WRIT_command();
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

void prot_poll()
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
void prot_slowTimer()
{
    //char buffer[16];
    // Ping server every second
    sendHelo();

    //sprintf(buffer, "STA:%x,%s", (int)s_protState, s_errMsg);
    //println(buffer);
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
