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

#ifdef HAS_BUS_SERVER
bit prot_registered;
#endif

void prot_init()
{
#ifdef HAS_IP
    ip_prot_init();

    // Align 1sec to now()
    s_slowTimer = TickGet();
#endif

#ifdef HAS_RS485
    bus_init();
#endif
    
#ifdef HAS_BUS_SERVER
    prot_registered = FALSE;
#endif
    
    s_commandToRun = -1;
    s_inWriteSink = s_inReadSink = -1;
}

static void CLOS_command()
{
#ifdef DEBUGMODE
    printch('l');
#endif
    prot_control_close();
}

static void SELE_command()
{
#ifdef DEBUGMODE
    printch('s');
#endif
    // Select subnode. 
    WORD w;
    if (!prot_control_readW(&w)) {
        fatal("SE.u");
    }
    
    // Select subnode.
    // Simply ignore when no subnodes
#ifdef HAS_BUS_SERVER
    if (w > 0)
    {
        // Otherwise connect the socket
        bus_connectSocket(w - 1);
    }
    prot_registered = TRUE;
#endif
}

// 0 bytes to receive
static void CHIL_command()
{
#ifdef DEBUGMODE
    printch('c');
#endif
    // Fetch my GUID
    union {
        PersistentData persistence;
        WORD count;
    } buffer;
    
    boot_getUserData(&buffer.persistence);

    // Send ONLY mine guid. Other GUIDS should be fetched using SELE first.
    prot_control_write(&buffer.persistence.deviceId, sizeof(GUID));
    
#ifdef HAS_BUS_SERVER
    // Propagate the request to all children to fetch their GUIDs
    buffer.count = bus_getChildrenMaskSize();
    prot_control_writeW(buffer.count);
    prot_control_write(bus_getChildrenMask(), buffer.count);

    bus_dirtyChildren = FALSE;
#else    
    // No children
    buffer.count = 0;
    prot_control_writeW(buffer.count);
#endif
    
    prot_control_flush();      
}

// 0 bytes to receive
static void SINK_command()
{
#ifdef DEBUGMODE
    printch('k');
#endif
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
#ifdef DEBUGMODE
    printch('g');
#endif
    GUID guid;
    if (!prot_control_read(&guid, sizeof(GUID))) {
        fatal("GU.u");
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
#ifdef DEBUGMODE
    printch('r');
#endif
    WORD sinkId;
    if (!prot_control_readW(&sinkId))
    {
        fatal("RD.u");
    }   
    s_inWriteSink = sinkId;
}

// 2 bytes to receive
static void WRIT_command()
{
#ifdef DEBUGMODE
    printch('w');
#endif
    WORD sinkId;
    if (!prot_control_readW(&sinkId))
    {
        fatal("WR.u");
    }
    s_inReadSink = sinkId;
}

const struct {
    TWOCC cmd;
    void (*fptr)();
    char readSize;
} s_table[] = {
    { 
        "RD", READ_command, 2
    },
    { 
        "WR", WRIT_command, 2
    },
    { 
        "CL", CLOS_command, 0
    },
    { 
        "SL", SELE_command, 2
    },
    { 
        "SK", SINK_command, 0
    },
    { 
        "CH", CHIL_command, 0
    },
    { 
        "GU", GUID_command, 16
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

    if (TickGet() - s_slowTimer >= TICKS_PER_SECOND)
    {
        s_slowTimer = TickGet();
        ip_prot_slowTimer();
    }
#endif
    
    if (!prot_control_isConnected()) {
#ifdef HAS_BUS_SERVER
        bus_disconnectSocket(-1);
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
            // drop the TCP connection        
            prot_control_abort();
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
    if (s >= sizeof(TWOCC)) // Minimum msg size
    {
        // This can even peek only one command.
        // Until not closed by server, or CLOS command sent, the channel can stay open.

        // TODO: Limitation: both the command and its data should be in the read buffer
        // at the same time
        TWOCC msg;
        prot_control_read(&msg, sizeof(TWOCC));
        for (BYTE i = 0; i < COMMAND_COUNT; i++) {
            if (!memcmp(msg.str, s_table[i].cmd.str, sizeof(TWOCC))) {
                s_commandToRun = i;
                return;
            }
        }
        
        // Unknown command
        fatal("CM.u");
    }
    // Otherwise wait for data
}
