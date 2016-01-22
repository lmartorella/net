#include "pch.h"
#include "protocol.h"
#include "appio.h"
#include "persistence.h"

#ifdef HAS_IP
#include "Compiler.h"
#include "TCPIPStack/TCPIP.h"
#endif

static int s_inReadSink = -1;
BOOL prot_registered = FALSE;

static void CLOS_command()
{
	prot_control_close();
}

static void SELE_command()
{
    prot_registered = TRUE;
 
    // Select subnode. Ignores it.
    WORD w;
    if (!prot_control_readW(&w)) {
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
    prot_control_writeW(1);
    prot_control_write(&guid, sizeof(GUID));
    prot_control_flush();
}

static void SINK_command()
{
    FOURCC fourcc;
    int i = AllSinksSize;
    prot_control_writeW(i);
    while (i > 0)
    {
        i--;
        const Sink* sink = AllSinks[i]; 
        memcpy(&fourcc, &sink->fourCc, sizeof(FOURCC));
        
        // Put device ID
        prot_control_write(&fourcc, sizeof(FOURCC));
    }
    prot_control_flush();
}

static void GUID_command()
{
    GUID guid;
    PersistentData persistence;
    
    if (!prot_control_read(&guid, sizeof(GUID))) {
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
    if (!prot_control_readW(&sink))
    {
        fatal("READ.undr");
    }

    // Address sink
    AllSinks[sink]->writeHandler();
    prot_control_flush();
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
    if (!prot_control_readW((WORD*)&s_inReadSink))
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
    prot_control_read(&msg, sizeof(FOURCC));
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

/*
	Manage POLLs (read buffers)
*/
void prot_poll()
{
#ifdef HAS_IP
    // Do ETH stuff
    StackTask();
    // This tasks invokes each of the core stack application tasks
    StackApplications();
#endif
    
    if (prot_started)
    {
        unsigned short s;
        if (!prot_control_isListening())
        {
            return;
        }

        s = prot_control_getDataSize();
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
