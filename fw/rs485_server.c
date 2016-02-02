#include "pch.h"
#include "ip_client.h"
#include "hardware/rs485.h"
#include "hardware/tick.h"

#ifdef HAS_RS485_SERVER

static signed char s_pollIndex;
static TICK_TYPE s_timeout;
#define BUS_ACK_TIMEOUT (TICKS_PER_SECOND / 100) //10ms (19200,9,1 4bytes = ~2.3ms)
// 8*8 = 63 max children (last is broadcast)
#define MAX_CHILDREN 64
static BYTE s_childKnown[MAX_CHILDREN / 8];
#define MSG_SIZE 4
#define isChildKnown(i) (s_childKnown[i / 8] & (1 << (i % 8)))
static BOOL s_isFullScan;
static TICK_TYPE s_lastFullScan;
#define FULL_SCAN_TIMEOUT (TICKS_PER_SECOND * 5) // 5000ms (it takes BUS_ACK_TIMEOUT * MAX_CHILDREN = 640ms)

typedef enum { 
    // Message to beat a bean
    POLL_MSG_TYPE_HEARTBEAT = 1,
    // Message to ask unknown bean to present (broadcast))
    POLL_MSG_TYPE_READY_FOR_REQ = 2,
    // Message to ask the only unknown bean to register itself
    POLL_MSG_TYPE_ADDRESS_ASSIGN = 3,
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
    // Wait for bus free
    BUS_STATE_WAIT_FOR_POLL_TX_FINISHED,
    // Wait for ack
    BUS_STATE_WAIT_ACK,
} s_busState;


void bus_init()
{
    // No beans are known
    for (int i = 0; i < MAX_CHILDREN / 8; i++) {
        s_childKnown[i] = 0;
    }
    // Starts from zero
    s_pollIndex = MAX_CHILDREN - 1;

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
            if (s_timeout > (s_lastFullScan + FULL_SCAN_TIMEOUT)){
                s_isFullScan = TRUE;
                s_lastFullScan = s_timeout;
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
    // Wait for tx end
    s_busState = BUS_STATE_WAIT_FOR_POLL_TX_FINISHED;
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

    // Go ahead. Expect a valid response like the heartbeat
    s_busState = BUS_STATE_WAIT_FOR_POLL_TX_FINISHED;
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
    if (rs485_getState() & RS485_ERR) {
        // Error. Reset to idle
        s_busState = BUS_STATE_IDLE;
    }
    
    switch (s_busState) {
        case BUS_STATE_IDLE:
            bus_pollNextKnownChild();
            break;
        case BUS_STATE_WAIT_FOR_POLL_TX_FINISHED:
            // Finished TX?
            if (!(rs485_getState() & RS485_LINE_TX)) {
                // Ok, wait for ACK response
                s_busState = BUS_STATE_WAIT_ACK;
                // timeout
                s_timeout = TickGet() + BUS_ACK_TIMEOUT;
            }
            break;
        case BUS_STATE_WAIT_ACK:
            // Wait timeout for response
            if (rs485_readAvail() >= MSG_SIZE) {
                // Check what is received
                bus_checkAck();
            } else {
                // Check for timeout
                if (TickGet() > s_timeout) {
                    // Timeout. Dead bean?
                    // Do nothing, simply skip it for now.
                    s_busState = BUS_STATE_IDLE;
                }
            }
            break;
    }
}


#endif