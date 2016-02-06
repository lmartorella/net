#include "pch.h"
#include "protocol.h"
#include "appio.h"
#include "persistence.h"
#include "bus.h"

#ifdef HAS_IP
#include "Compiler.h"
#include "TCPIPStack/TCPIP.h"
#include "ip_client.h"
#endif

static signed char s_inReadSink = -1;
static signed char s_inWriteSink = -1;
static int s_commandToRun = -1;
BOOL prot_registered = FALSE;

#ifdef HAS_BUS_SERVER
static BOOL s_socketConnected = FALSE;
#endif
#ifdef HAS_BUS_CLIENT
static BOOL g_rc9;
#endif

void prot_init()
{
#ifdef HAS_IP
    ip_prot_init();
#endif
#ifdef HAS_BUS_SERVER
    bus_init();
#endif
}

static void CLOS_command()
{
	prot_control_close();
}

static void SELE_command()
{
    prot_registered = TRUE;
 
    // Select subnode. 
    WORD w;
    if (!prot_control_readW(&w)) {
        fatal("SELE.undr");
    }
    
    // Select subnode.
    // Simply ignore when no subnodes
#ifdef HAS_BUS_SERVER
    // Otherwise connect the socket
    bus_connectSocket(w);
    s_socketConnected = TRUE;
#endif
}

// 0 bytes to receive
static void CHIL_command()
{
    PersistentData persistence;
    boot_getUserData(&persistence);
    
    // Only 1 children: me
    prot_control_writeW(1);
    prot_control_write(&persistence.deviceId, sizeof(GUID));
    prot_control_flush();
}

// 0 bytes to receive
static void SINK_command()
{
    prot_control_writeW(AllSinksSize);
    for (int i = 0; i < AllSinksSize; i++) {
        // Put device ID
        prot_control_write(&AllSinks[i]->fourCc, sizeof(FOURCC));
    }
    prot_control_flush();
}

// 16 bytes to receive
static void GUID_command()
{
    GUID guid;
    if (!prot_control_read(&guid, sizeof(GUID))) {
        fatal("GUID.undr");
    }

    PersistentData persistence;
    boot_getUserData(&persistence);
    persistence.deviceId = guid;
    // Have new GUID! Program it.
    boot_updateUserData(&persistence);   
}

// 2 bytes to receive
static void READ_command()
{
    WORD sinkId;
    if (!prot_control_readW(&sinkId))
    {
        fatal("READ.undr");
    }   
    s_inWriteSink = sinkId;
}

// 2 bytes to receive
static void WRIT_command()
{
    WORD sinkId;
    if (!prot_control_readW(&sinkId))
    {
        fatal("WRIT.undr");
    }
    s_inReadSink = sinkId;
}

const struct {
    char cmd[4];
    void (*fptr)();
    char readSize;
} s_table[] = {
    { 
        "READ", READ_command, 2
    },
    { 
        "WRIT", WRIT_command, 2
    },
    { 
        "CLOS", CLOS_command, 0
    },
    { 
        "SELE", SELE_command, 2
    },
    { 
        "SINK", SINK_command, 0
    },
    { 
        "CHIL", CHIL_command, 0
    },
    { 
        "GUID", GUID_command, 16
    }
};
#define COMMAND_COUNT 7


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
    
#ifdef HAS_BUS_SERVER
    // General poll
    bus_poll();
#endif
    
    if (!prot_started || !prot_control_isListening()) {
        return;
    }

#ifdef HAS_BUS_SERVER
    // Socket connected?
    BUS_SOCKET_STATE busState = bus_isSocketConnected(); 
    if (busState == BUS_SOCKET_CONNECTED) {
        // TCP is still polled by bus
        return;
    }
    else if (busState == BUS_SOCKET_TIMEOUT) {
        // drop the connection        
        prot_control_close();
    }
#endif

    if (s_inReadSink >= 0) {
        // Tolerates empty rx buffer
        if (!AllSinks[s_inReadSink]->readHandler()) {
            s_inReadSink = -1;
        }
        return;
    }
    if (s_inWriteSink >= 0) {
        // Address sink
        if (!AllSinks[s_inWriteSink]->writeHandler()){
            s_inWriteSink = -1;
        }
        prot_control_flush();
        return;
    }

    WORD s = prot_control_readAvail();
    if (s_commandToRun >= 0) {
        if (s >= s_table[s_commandToRun].readSize) {
            s_table[s_commandToRun].fptr();
            s_commandToRun = -1;
        }
        return;
    }

    // So decode message then
    if (s >= 4) // Minimum msg size
    {
        // This can even peek only one command.
        // Until not closed by server, or CLOS command sent, the channel can stay open.

        // TODO: Limitation: both the command and its data should be in the read buffer
        // at the same time
        FOURCC msg;
        prot_control_read(&msg, sizeof(FOURCC));
        for (BYTE i = 0; i < COMMAND_COUNT; i++) {
            if (memcmp(msg.str, s_table[i].cmd, 4) == 0) {
                s_commandToRun = i;
                return;
            }
        }
        fatal("CMD.unkn");
    }
    // Otherwise wait for data
}
