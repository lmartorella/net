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
    PersistentData persistence;
    boot_getUserData(&persistence);
    
    // Only 1 children: me
    prot_control_writeW(1);
    prot_control_write(&persistence.deviceId, sizeof(GUID));
    prot_control_flush();
}

static void SINK_command()
{
    prot_control_writeW(AllSinksSize);
    for (int i = 0; i < AllSinksSize; i++)
    {
        // Put device ID
        prot_control_write(&AllSinks[i]->fourCc, sizeof(FOURCC));
    }
    prot_control_flush();
}

// Need 16 bytes
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
    if (!prot_control_readW((WORD*)&s_inWriteSink))
    {
        fatal("READ.undr");
    }
}

static void WRIT_command()
{
    // Get sink# and msg size
    if (!prot_control_readW((WORD*)&s_inReadSink))
    {
        fatal("WRIT.undr");
    }
}

typedef struct {
    char cmd[4];
    void (*fptr)();
    char readSize;
} TABLE;
const TABLE s_table[] = {
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
#if HAS_BUS_SERVER
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
        FOURCC msg;
        prot_control_read(&msg, sizeof(FOURCC));
        for (BYTE i = 0; i < sizeof(s_table) / sizeof(TABLE); i++) {
            if (memcmp(msg.str, s_table[i].cmd, 4) == 0) {
                s_commandToRun = i;
                return;
            }
        }
        fatal("CMD.unkn");
    }
    // Otherwise wait for data
}
