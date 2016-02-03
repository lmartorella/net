#include "pch.h"
#include "protocol.h"
#include "appio.h"
#include "persistence.h"

#ifdef HAS_IP
#include "Compiler.h"
#include "TCPIPStack/TCPIP.h"
#endif

static signed char s_inReadSink = -1;
static signed char s_inWriteSink = -1;
static signed char s_commandToRun = -1;
BOOL prot_registered = FALSE;

#ifdef HAS_BUS_SERVER
int s_selectedBusNode = -1;
#endif
static FOURCC s_lastMsgRead;

static void CLOS_command()
{
	prot_control_close();
#ifdef HAS_BUS_SERVER
    if (s_selectedBusNode >= 0) {
        bus_closeCommand();
    }
#endif
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
    // I need to address the communication to a bus node
    s_selectedBusNode = w;
#endif
}

#ifdef HAS_BUS_SERVER
static void(*s_currentBusCommand)();
static void forwardCommandToBus(void(*handler)(), const BYTE* buffer, int size) {
    bus_server_select(s_selectedBusNode);
    bus_server_send((BYTE*)&s_lastMsgRead, sizeof(FOURCC));
    bus_server_send(buffer, size);
    // Now waits for data
    s_currentBusCommand = handler;
}
#endif

// 0 bytes to receive
static void CHIL_command()
{
#ifdef HAS_BUS_SERVER
    if (s_selectedBusNode >= 0) {
        // Forward the call to bus
        forwardCommandToBus(&CHIL_bus_handler, NULL, 0);
        return;
    }
#endif

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
#ifdef HAS_BUS_SERVER
    if (s_selectedBusNode >= 0) {
        // Forward the call to bus
        forwardCommandToBus(&SINK_bus_handler, NULL, 0);
        return;
    }
#endif

    prot_control_writeW(AllSinksSize);
    for (int i = 0; i < AllSinksSize; i++)
    {
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

#ifdef HAS_BUS_SERVER
    if (s_selectedBusNode >= 0) {
        // Forward the call to bus
        forwardCommandToBus(&GUID_bus_handler, (BYTE*)&guid, sizeof(GUID));
        return;
    }
#endif

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

#ifdef HAS_BUS_SERVER
    if (s_selectedBusNode >= 0) {
        // Forward the call to bus
        forwardCommandToBus(&READ_bus_handler, (BYTE*)&sinkId, 2);
        return;
    }
#endif
    
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

#ifdef HAS_BUS_SERVER
    if (s_selectedBusNode >= 0) {
        // Forward the call to bus
        forwardCommandToBus(&WRIT_bus_handler, (BYTE*)&sinkId, 2);
        return;
    }
#endif

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
inline void prot_poll()
{
#ifdef HAS_IP
    // Do ETH stuff
    StackTask();
    // This tasks invokes each of the core stack application tasks
    StackApplications();
#endif
#ifdef HAS_BUS_SERVER
    bus_poll();
#endif
    
    if (!prot_started || !prot_control_isListening())
    {
        return;
    }

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

    unsigned short s = prot_control_getDataSize();
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
        prot_control_read(&s_lastMsgRead, sizeof(FOURCC));
        for (BYTE i = 0; i < COMMAND_COUNT; i++) {
            if (memcmp(s_lastMsgRead.str, s_table[i].cmd, 4) == 0) {
                s_commandToRun = i;
                return;
            }
        }
        fatal("CMD.unkn");
    }
    // Otherwise wait for data
}
