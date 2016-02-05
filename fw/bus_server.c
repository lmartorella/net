#include "pch.h"
#include "ip_client.h"
#include "hardware/rs485.h"
#include "hardware/tick.h"
#include "bus.h"
#include "protocol.h"

#ifdef HAS_BUS_SERVER

static signed char s_pollIndex;
static TICK_TYPE s_timer;
#define BUS_ACK_TIMEOUT (TICKS_PER_SECOND / 100) //10ms (19200,9,1 4bytes = ~2.3ms)
// 8*8 = 63 max children (last is broadcast)
#define MAX_CHILDREN 64
static BYTE s_childKnown[MAX_CHILDREN / 8];
#define MSG_SIZE 4
#define isChildKnown(i) (s_childKnown[i / 8] & (1 << (i % 8)))
static BOOL s_isFullScan;
static TICK_TYPE s_lastFullScan;
static int s_commandNodeIdx;
static BYTE s_commandData[sizeof(GUID) + sizeof(FOURCC)];
static int s_commandDataSize;

#define FULL_SCAN_TIMEOUT (TICKS_PER_SECOND * 5) // 5000ms (it takes BUS_ACK_TIMEOUT * MAX_CHILDREN = 640ms)
#define BUS_COMMAND_BREAK_TIMEOUT (TICKS_PER_SECOND) // 1s

static void bus_startCommand_header();
static void bus_startCommand_body();
static void bus_command_poll();

typedef enum { 
    // Message to beat a bean
    POLL_MSG_TYPE_HEARTBEAT = 1,
    // Message to ask unknown bean to present (broadcast))
    POLL_MSG_TYPE_READY_FOR_REQ = 2,
    // Message to ask the only unknown bean to register itself
    POLL_MSG_TYPE_ADDRESS_ASSIGN = 3,
    // Command data will follow
    POLL_MSG_SELECT_FOR_COMMAND = 4
} POLL_MSG_TYPE;

typedef enum { 
    // Bean: ack heartbeat
    ACK_MSG_TYPE_HEARTBEAT = 1,
    // Bean: notify unknown (response to POLL_MSG_TYPE_READY_FOR_REQ)
    ACK_MSG_TYPE_HELLO = 2,
} ACK_MSG_TYPE;

static enum {
    // To call next child
    BUS_STATE_IDLE,
    // Wait for ack
    BUS_STATE_WAIT_ACK,
    // A command to a node is in progress
    BUS_IN_COMMAND_TX,
    // A command to a node is in progress (receiving data)
    BUS_IN_COMMAND_RX,
    // Breaking an ongoing command
    BUS_COMMAND_BREAK
} s_busState;
static BOOL s_waitTxFlush;

void bus_init()
{
    // No beans are known
    for (int i = 0; i < MAX_CHILDREN / 8; i++) {
        s_childKnown[i] = 0;
    }
    
    // Starts from zero
    s_pollIndex = MAX_CHILDREN - 1;
    s_commandNodeIdx = -1;
    s_waitTxFlush = FALSE;
    
    // Do full scan
    s_isFullScan = TRUE;
    s_lastFullScan = TickGet();
}

// Ask for the next known child
static void bus_pollNextKnownChild()
{
    POLL_MSG_TYPE msgType = POLL_MSG_TYPE_HEARTBEAT;
    
    // Poll next child 
    do {
        s_pollIndex++;
        if (s_pollIndex >= MAX_CHILDREN) {
            // Do broadcast now
            s_pollIndex = -1;
            msgType = POLL_MSG_TYPE_READY_FOR_REQ;
            
            // Check if a full scan should be done
            // Use the last timeout calculated (at least it is the broadcast)
            if (s_timer > (s_lastFullScan + FULL_SCAN_TIMEOUT)){
                s_isFullScan = TRUE;
                s_lastFullScan = s_timer;
            }
            else {
                s_isFullScan = FALSE;
            }
            break;
        }
        // Check for valid node
    } while (!s_isFullScan && !isChildKnown(s_pollIndex));
    
    // Poll the line. Send sync
    BYTE buffer[4] = { 0x55, 0xaa };
    buffer[2] = s_pollIndex;
    buffer[3] = msgType;
    rs485_write(TRUE, buffer, 4);
    s_waitTxFlush = TRUE;
    
    // Wait for tx end and then for ack
    s_busState = BUS_STATE_WAIT_ACK;
}

static void bus_registerNewNode() {
    // Find a free slot
    s_pollIndex = 0;
    // Check for valid node
    while (isChildKnown(s_pollIndex)) {
        s_pollIndex++;
        if (s_pollIndex >= MAX_CHILDREN) {
            // Ops, no space
            s_busState = BUS_STATE_IDLE;
            return;
        }
    };
    
    // Have the good address
    // Send it
    BYTE buffer[4] = { 0x55, 0xaa };
    buffer[2] = s_pollIndex;
    buffer[3] = POLL_MSG_TYPE_ADDRESS_ASSIGN;
    rs485_write(TRUE, buffer, 4);
    s_waitTxFlush = TRUE;

    // Go ahead. Expect a valid response like the heartbeat
    s_busState = BUS_STATE_WAIT_ACK;
}

static void bus_checkAck()
{
    BYTE buffer[MSG_SIZE];
    BOOL isAddress;
    // Receive bytes
    // Expected: 0x55, [index], [state]
    rs485_read(buffer, MSG_SIZE, &isAddress);
    if (!isAddress && buffer[0] == 0xaa && buffer[1] == 0x55 && buffer[2] == s_pollIndex) {
        // Ok, good response
        if (buffer[3] == ACK_MSG_TYPE_HELLO && s_pollIndex == -1) {
            // Need registration.
            bus_registerNewNode();
            return;
        }
    }
    // Next one.
    s_busState = BUS_STATE_IDLE;
}

void bus_poll()
{
    if (rs485_getState() == RS485_FRAME_ERR) {
        // Error. Reset to idle
        s_busState = BUS_STATE_IDLE;
        // Reinit the bus
        rs485_init();
    }
    
    if (s_waitTxFlush) {
        // Finished TX?
        if (rs485_getState() == RS485_LINE_TX) {
            // Skip state management
            return;
        } else {
            // Start timeout and go ahead
            s_timer = TickGet();
            s_waitTxFlush = FALSE;
        }
    }
    
    switch (s_busState) {
        case BUS_STATE_IDLE:
            // Should send a command?
            if (s_commandNodeIdx >= 0) {
                bus_startCommand_header();
            }
            else {
                bus_pollNextKnownChild();
            }
            break;
        case BUS_STATE_WAIT_ACK:
            // Wait timeout for response
            if (rs485_readAvail() >= MSG_SIZE) {
                // Check what is received
                bus_checkAck();
            } else {
                // Check for timeout
                if (TickGet() > (s_timer + BUS_ACK_TIMEOUT)) {
                    // Timeout. Dead bean?
                    // Do nothing, simply skip it for now.
                    s_busState = BUS_STATE_IDLE;
                }
            }
            break;
        case BUS_IN_COMMAND_TX:
            // Start transmitting command
            bus_startCommand_body();
            break;
        case BUS_IN_COMMAND_RX:
            // Check for command completion
            bus_command_poll();
            break;
        case BUS_COMMAND_BREAK:
            // Simply wait
            if (TickGet() > (s_timer + BUS_COMMAND_BREAK_TIMEOUT)) {
                // Timeout. Dead bean?
                // Do nothing, simply skip it for now.
                s_busState = BUS_STATE_IDLE;
            }
            break;
    }
}

// The command starts when the bus is idle
void bus_openCommand(int nodeIdx, const FOURCC* cmd, const BYTE* data, int dataSize)
{
    if (s_busState == BUS_IN_COMMAND_RX || s_busState == BUS_IN_COMMAND_TX) {
        // Break the current command
        s_busState = BUS_COMMAND_BREAK;
        s_timer = TickGet();
    }
    else {
        s_commandNodeIdx = nodeIdx;

        // Copy command body in a buffer
        memcpy(memcpy(s_commandData, cmd, sizeof(FOURCC)), data, dataSize);
        s_commandDataSize = sizeof(FOURCC) + dataSize;
    }
}

BOOL bus_isExecCommand() 
{
    return s_commandNodeIdx >= 0;
}

static void bus_startCommand_header() 
{
    // Bus is idle. Start transmitting.
    
    BYTE buffer[4] = { 0x55, 0xaa };
    buffer[2] = s_commandNodeIdx;
    buffer[3] = POLL_MSG_SELECT_FOR_COMMAND;
    rs485_write(TRUE, buffer, 4);
    s_waitTxFlush = TRUE;

    // Next state (after TX finishes) is IN COMMAND
    s_busState = BUS_IN_COMMAND_TX;
}

static void bus_startCommand_body() 
{
    // Bus header sent. Immediately send the command.
    rs485_write(FALSE, s_commandData, s_commandDataSize);
    s_waitTxFlush = TRUE;

    // Next state (after TX finishes) is IN COMMAND
    s_busState = BUS_IN_COMMAND_RX;
}

static void bus_command_poll()
{
    int size = rs485_readAvail();
    if (size > 0) {
        BYTE buffer[8];
        BOOL rc9;
        size = size < 8 ? size : 8;
        rs485_read(buffer, size, &rc9);
        // Send it to the IP channel
        prot_control_write(buffer, size);
        
        // Finished?
        if (rc9) {
            prot_control_flush();
            
            // End of command, back to idle
            s_commandNodeIdx = -1;   
            s_busState = BUS_STATE_IDLE;
        }
    }
}

#endif