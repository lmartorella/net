#include "pch.h"
#include "ip_client.h"
#include "hardware/rs485.h"
#include "hardware/tick.h"
#include "bus.h"
#include "protocol.h"

#ifdef HAS_BUS_SERVER

// 8*8 = 63 max children (last is broadcast)
#define MAX_CHILDREN 64
static BYTE s_childKnown[MAX_CHILDREN / 8];

#define MSG_SIZE 4
#define isChildKnown(i) (s_childKnown[i / 8] & (1 << (i % 8)))

static signed char s_scanIndex;
static TICK_TYPE s_lastScanTime;
static TICK_TYPE s_lastTime;

// Socket connected to child. If -1 idle. If -2 socket timeout
static int s_socketConnected;

#define BUS_SCAN_TIMEOUT (TICKS_PER_SECOND * 5) // 5000ms (it takes BUS_ACK_TIMEOUT * MAX_CHILDREN = 640ms)
#define BUS_SOCKET_TIMEOUT (TICKS_PER_SECOND / 2)  // 500ms
#define BUS_ACK_TIMEOUT (TICKS_PER_SECOND / 100) //10ms (19200,9,1 4bytes = ~2.3ms)

typedef enum { 
    // Message to beat a bean
    POLL_MSG_TYPE_HEARTBEAT = 1,
    // Message to ask unknown bean to present (broadcast)
    POLL_MSG_TYPE_READY_FOR_HELLO = 2,
    // Message to ask the only unknown bean to register itself
    POLL_MSG_TYPE_ADDRESS_ASSIGN = 3,
    // Command/data will follow: socket open
    POLL_MSG_CONNECT = 4
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
    // A direct connection is established
    BUS_STATE_SOCKET_CONNECTED
} s_busState;
static BOOL s_waitTxFlush;

static void bus_socketCreate();
static void bus_socketPoll();

void bus_init()
{
    // No beans are known
    for (int i = 0; i < MAX_CHILDREN / 8; i++) {
        s_childKnown[i] = 0;
    }
    
    // Starts from zero
    s_scanIndex = MAX_CHILDREN - 1;
    s_socketConnected = -1;
    s_waitTxFlush = FALSE;
    
    // Do full scan
    s_lastScanTime = TickGet();
}

// Ask for the next known child
static void bus_scanNext()
{
    POLL_MSG_TYPE msgType = POLL_MSG_TYPE_HEARTBEAT;
    
    // Poll next child 
    s_scanIndex++;
    if (s_scanIndex >= MAX_CHILDREN) {
        // Do broadcast now
        s_scanIndex = -1;
        msgType = POLL_MSG_TYPE_READY_FOR_HELLO;
        s_lastScanTime = TickGet();
    }
    
    // Poll the line. Send sync
    BYTE buffer[4] = { 0x55, 0xaa };
    buffer[2] = s_scanIndex;
    buffer[3] = msgType;
    rs485_write(TRUE, buffer, 4);
    s_waitTxFlush = TRUE;
    
    // Wait for tx end and then for ack
    s_busState = BUS_STATE_WAIT_ACK;
}

static void bus_registerNewNode() {
    // Find a free slot
    s_scanIndex = 0;
    // Check for valid node
    while (isChildKnown(s_scanIndex)) {
        s_scanIndex++;
        if (s_scanIndex >= MAX_CHILDREN) {
            // Ops, no space
            s_busState = BUS_STATE_IDLE;
            return;
        }
    };
    
    // Have the good address
    // Send it
    BYTE buffer[4] = { 0x55, 0xaa };
    buffer[2] = s_scanIndex;
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
    if (!isAddress && buffer[0] == 0xaa && buffer[1] == 0x55 && buffer[2] == s_scanIndex) {
        // Ok, good response
        if (buffer[3] == ACK_MSG_TYPE_HELLO && s_scanIndex == -1) {
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
            s_lastTime = TickGet();
            s_waitTxFlush = FALSE;
        }
    }
    
    switch (s_busState) {
        case BUS_STATE_IDLE:
            // Should open a socket?
            if (s_socketConnected >= 0) {
                bus_socketCreate();
            }
            else {
                // Do/doing a scan?
                TICK_TYPE tick = TickGet();
                if (tick > s_lastScanTime + BUS_SCAN_TIMEOUT) {
                    bus_scanNext();
                }
            }
            break;
        case BUS_STATE_WAIT_ACK:
            // Wait timeout for response
            if (rs485_readAvail() >= MSG_SIZE) {
                // Check what is received
                bus_checkAck();
            } else {
                // Check for timeout
                if (TickGet() > (s_lastTime + BUS_ACK_TIMEOUT)) {
                    // Timeout. Dead bean?
                    // Do nothing, simply skip it for now.
                    s_busState = BUS_STATE_IDLE;
                }
            }
            break;
        case BUS_STATE_SOCKET_CONNECTED:
            if (TickGet() > (s_lastTime + BUS_SOCKET_TIMEOUT)) {
                // Timeout. Dead bean?
                // Drop the TCP connection and reset the channel
                s_busState = BUS_STATE_IDLE;
                s_socketConnected = -2;
            }
            else {
                bus_socketPoll();
            }
            break;
    }
}

// The command starts when the bus is idle
void bus_connectSocket(int nodeIdx)
{
    s_socketConnected = nodeIdx;
    // Next IDLE will start the connection
}

void bus_disconnectSocket()
{
    s_socketConnected = -1;
}

BUS_SOCKET_STATE bus_isSocketConnected() 
{
    return s_socketConnected >= 0 ? BUS_SOCKET_CONNECTED : (s_socketConnected == -2 ? BUS_SOCKET_TIMEOUT : BUS_SOCKET_NONE);
}

static void bus_socketCreate() 
{
    // Bus is idle. Start transmitting/receiving.
    BYTE buffer[4] = { 0x55, 0xaa };
    buffer[2] = s_socketConnected;
    buffer[3] = POLL_MSG_CONNECT;
    rs485_write(TRUE, buffer, 4);
    s_waitTxFlush = TRUE;

    // Next state (after TX finishes) is IN COMMAND
    s_busState = BUS_STATE_SOCKET_CONNECTED;
}

static void bus_socketPoll() 
{
    // Bus line is slow, though
    BYTE buffer[8];
    BOOL updateTimer = FALSE;
            
    // Data from IP?
    WORD rx = prot_control_readAvail();
    if (rx > 0) {
        // Read data and push it into the line
        rx = rx > sizeof(buffer) ? sizeof(buffer) : rx;
        prot_control_read(buffer, rx);
        
        rs485_write(FALSE, buffer, rx);
        updateTimer = TRUE;
    }
    else {
        // Data received?
        WORD tx = rs485_readAvail();
        if (tx > 0) {
            BOOL rc9;
            
            // Read data and push it into IP
            tx = tx > sizeof(buffer) ? sizeof(buffer) : tx;
            rs485_read(buffer, tx, &rc9);
            
            if (rc9) {
                // Socket close. Strip last byte
                tx--;
            }

            prot_control_write(buffer, tx);
            
            if (rc9) {
                // Now the channel is idle again
                s_busState = BUS_STATE_IDLE;
            }
            else {
                updateTimer = TRUE;
            }
        }
    }
    
    if (updateTimer) {
        s_lastTime = TickGet();
    }
}

int bus_getAliveCount() {
    int res = 0;
    for (int i = 0; i < MAX_CHILDREN; i++) {
        if (isChildKnown(i)) {
            res++;
        }
    }
    return res;
}

#endif