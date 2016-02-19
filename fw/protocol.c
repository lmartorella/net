#include "pch.h"
#include "protocol.h"
#include "appio.h"
#include "persistence.h"
#include "bus.h"
#include "hardware/tick.h"

#ifdef HAS_IP
#include "Compiler.h"
#include "TCPIPStack/TCPIP.h"
#include "ip_client.h"

static TICK_TYPE s_slowTimer;
#endif

static signed char s_inReadSink;
static signed char s_inWriteSink;
static int s_commandToRun;
BOOL prot_registered;

#ifdef HAS_BUS_SERVER
static BOOL s_dirtyChildren;
#endif

void prot_init()
{
#ifdef HAS_IP
    ip_prot_init();

    // Align 1sec to now()
    s_slowTimer = TickGet();
#endif

#ifdef HAS_BUS
    bus_init();
#endif
    
#ifdef HAS_BUS_SERVER
    s_dirtyChildren = FALSE;
#endif
    
    prot_registered = FALSE;
    s_commandToRun = -1;
    s_inWriteSink = s_inReadSink = -1;
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
    if (w > 0)
    {
        // Otherwise connect the socket
        bus_connectSocket(w - 1);
    }
#endif
}

// 0 bytes to receive
static void CHIL_command()
{
    // Fetch my GUID
    PersistentData persistence;
    boot_getUserData(&persistence);
 
#ifdef HAS_BUS_SERVER
    // Propagate the request to all children to fetch their GUIDs
    prot_control_writeW(1 + bus_getAliveCountAndResetDirty());
#else    
    // Only 1 children: me
    prot_control_writeW(1);
#endif
    
    // Send ONLY mine guid. Other GUIDS should be fetched using SELE first.
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
    // General poll
    bus_poll();
   
#ifdef HAS_IP
    // Do ETH stuff
    StackTask();
    // This tasks invokes each of the core stack application tasks
    StackApplications();

    if (TickGet() > (TICK_TYPE)(s_slowTimer + TICKS_PER_SECOND))
    {
        s_slowTimer = TickGet();
        ip_prot_slowTimer(s_dirtyChildren);
    }
#endif
    
    if (!prot_control_isConnected()) {
#ifdef HAS_BUS_SERVER
        bus_disconnectSocket();
#endif
        return;
    }

#ifdef HAS_BUS_SERVER
    // Socket connected?
    BUS_STATE busState = bus_getState(); 
    switch (busState) {
        case BUS_STATE_SOCKET_CONNECTED:
            // TCP is still polled by bus
            return;
        case BUS_STATE_SOCKET_TIMEOUT:
            // drop the connection        
            prot_control_close();
            break;
        case BUS_STATE_DIRTY_CHILDREN:
            s_dirtyChildren = TRUE;
            break;
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
