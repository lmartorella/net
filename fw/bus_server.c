#include "pch.h"
#include "ip_client.h"
#include "hardware/rs485.h"
#include "hardware/tick.h"
#include "bus.h"
#include "protocol.h"
#include "appio.h"

#ifdef HAS_BUS_SERVER

// 8*8 = 63 max children (last is broadcast)
#define MAX_CHILDREN 2
#define BUFFER_MASK_SIZE ((MAX_CHILDREN + 7) / 8)
static BYTE s_childKnown[BUFFER_MASK_SIZE];

// Ack message contains ACK
#define ACK_MSG_SIZE 4
#define BROADCAST_ADDRESS 0xff

static BYTE s_scanIndex;
static TICK_TYPE s_lastScanTime;
static TICK_TYPE s_lastTime;
bit bus_dirtyChildren;

// Socket connected to child. If -1 idle. If -2 socket timeout
static int s_socketConnected;

#define BUS_SCAN_TIMEOUT (TICK_TYPE)(TICKS_PER_SECOND * 1.5) // 1500ms 
#define BUS_SOCKET_TIMEOUT (TICK_TYPE)(TICKS_PER_SECOND / 2)  // 500ms
// ack time is due to engage+disengage time (of the slave) + ack_size
#define BUS_ACK_TIMEOUT (TICK_TYPE)(TICKS_PER_BYTE * ACK_MSG_SIZE * 4) // 9.1ms (19200,9,1 4*3bytes = )

static enum {
    // To call next child
    BUS_PRIV_STATE_IDLE,
    // Wait for ack
    BUS_PRIV_STATE_WAIT_ACK,
    // A direct connection is established
    BUS_PRIV_STATE_SOCKET_CONNECTED
} s_busState;

BUS_MASTER_STATS g_busStats;

static bit s_waitTxFlush;
static bit s_waitTxQuickEnd;

static void bus_socketCreate();
static void bus_socketPoll();

static bit isChildKnown(signed char i)
{
    return (s_childKnown[i / 8] & (1 << (i % 8))) != 0;
}

static BYTE countChildren()
{
    BYTE count = 0;
    for (BYTE i = 0; i < sizeof(s_childKnown); i++) {
        BYTE d = s_childKnown[i];
        while(d) {
            count += (d & 1);
            d >>= 1;
        }
    }
    return count;
}

static void setChildKnown(signed char i)
{
    s_childKnown[i / 8] |= (1 << (i % 8));
    
    char msg[16];
    sprintf(msg, "%u nodes", countChildren());
    println(msg);
}

void bus_init()
{
    // No beans are known
    for (int i = 0; i < BUFFER_MASK_SIZE; i++) {
        s_childKnown[i] = 0;
    }
    
    // Starts from zero
    s_scanIndex = BROADCAST_ADDRESS;
    s_socketConnected = SOCKET_NOT_CONNECTED;
    s_waitTxFlush = 0;
    s_waitTxQuickEnd = 0;
    bus_dirtyChildren = 0;
    
    // Do full scan
    s_lastScanTime = TickGet();

    memset(&g_busStats, 0, sizeof(BUS_MASTER_STATS));
}

// Ask for the next known child
static void bus_scanNext()
{
    BUS_MSG_TYPE msgType = BUS_MSG_TYPE_HEARTBEAT;
    s_lastScanTime = TickGet();
    
    // Poll next child 
    s_scanIndex++;
    if (s_scanIndex >= MAX_CHILDREN) {
        // Do broadcast now
        s_scanIndex = BROADCAST_ADDRESS;
        msgType = BUS_MSG_TYPE_READY_FOR_HELLO;
    }
    
    // Poll the line. Send sync
    BYTE buffer[4] = { 0x55, 0xaa };
    buffer[2] = s_scanIndex;
    buffer[3] = msgType;
    rs485_write(TRUE, buffer, 4);

    // Wait for tx end and then for ack
    s_waitTxFlush = 1;
    s_busState = BUS_PRIV_STATE_WAIT_ACK;
}

static void bus_registerNewNode() {
    // Find a free slot
    s_scanIndex = 0;
    // Check for valid node
    while (isChildKnown(s_scanIndex)) {
        s_scanIndex++;
        if (s_scanIndex >= MAX_CHILDREN) {
            // Ops, no space
            s_busState = BUS_PRIV_STATE_IDLE;
            s_scanIndex = 0;
            
            println("Children full");
            
            return;
        }
    };
    
    // Have the good address
    // Do not store it now, store it after the ack
    
    // Send it
    BYTE buffer[4] = { 0x55, 0xaa };
    buffer[2] = s_scanIndex;
    buffer[3] = BUS_MSG_TYPE_ADDRESS_ASSIGN;
    rs485_write(TRUE, buffer, 4);

    // Go ahead. Expect a valid response like the heartbeat
    s_waitTxFlush = 1;
    s_busState = BUS_PRIV_STATE_WAIT_ACK;
}

static void bus_checkAck()
{
    BYTE buffer[ACK_MSG_SIZE];
    // Receive bytes
    // Expected: 0x55, 0xaa, [index], [state] with rc9 = 0 + EXC
    rs485_read(buffer, ACK_MSG_SIZE);
    if (!rs485_lastRc9 && buffer[0] == 0x55 && buffer[1] == 0xaa && buffer[2] == s_scanIndex) {
        // Ok, good response
        if (buffer[3] == BUS_ACK_TYPE_HELLO && s_scanIndex == BROADCAST_ADDRESS) {
            // Need registration.
            bus_registerNewNode();
            return;
        }
        else if (buffer[3] == BUS_ACK_TYPE_HEARTBEAT && !isChildKnown(s_scanIndex) && s_scanIndex != BROADCAST_ADDRESS) {
            // A node with address registered, but I didn't knew it. Register it.
            bus_dirtyChildren = TRUE;
            setChildKnown(s_scanIndex);
        }
    }
    // Next one.
    s_busState = BUS_PRIV_STATE_IDLE;
}

void bus_poll()
{   
    if (s_waitTxFlush) {
        // Finished TX?
        if (rs485_getState() == RS485_LINE_TX_DATA || rs485_getState() == RS485_LINE_TX_DISENGAGE) {
            // Skip state management
            return;
        } else {
            // Start timeout and go ahead
            s_lastTime = TickGet();
            s_waitTxFlush = 0;
        }
    }
    else if (s_waitTxQuickEnd) {
        // Finished TX?
        if (rs485_getState() == RS485_LINE_TX_DATA) {
            // Skip state management
            return;
        } else {
            // Start timeout and go ahead
            s_lastTime = TickGet();
            s_waitTxQuickEnd = 0;
        }
    }
 
    BYTE s = rs485_readAvail();
    switch (s_busState) {
        case BUS_PRIV_STATE_IDLE:
            // Should open a socket?
            if (s_socketConnected >= 0) {
                bus_socketCreate();
            }
            else {
                // Do/doing a scan?
                if (TickGet() - s_lastScanTime >= BUS_SCAN_TIMEOUT) {
                    bus_scanNext();
                }
            }
            break;
        case BUS_PRIV_STATE_WAIT_ACK:
            // Wait timeout for response
            if (s >= ACK_MSG_SIZE) {
                // Check what is received
                bus_checkAck();
            } else {
                // Check for timeout
                if (TickGet() - s_lastTime >= BUS_ACK_TIMEOUT) {
                    // Timeout. Dead bean?
                    // Do nothing, simply skip it for now.
                    s_busState = BUS_PRIV_STATE_IDLE;
                }
            }
            break;
        case BUS_PRIV_STATE_SOCKET_CONNECTED:
            if (TickGet() - s_lastTime >= BUS_SOCKET_TIMEOUT) {
                // Timeout. Dead bean?
                // Drop the TCP connection and reset the channel
                bus_disconnectSocket(SOCKET_ERR_TIMEOUT);
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

void bus_disconnectSocket(int val)
{
    if (s_socketConnected >= 0) {
        // Send break char
        rs485_write(TRUE, &val, 1);
        s_waitTxFlush = 1;
        
        s_busState = BUS_PRIV_STATE_IDLE;
        
        g_busStats.socketTimeouts++;
    }
    s_socketConnected = val;
}

BUS_STATE bus_getState() 
{
    if (s_socketConnected >= 0)
        return BUS_STATE_SOCKET_CONNECTED;
    if (s_socketConnected == SOCKET_ERR_TIMEOUT) 
        return BUS_STATE_SOCKET_TIMEOUT;
    if (s_socketConnected == SOCKET_ERR_FERR) 
        return BUS_STATE_SOCKET_FERR;
    return BUS_STATE_NONE;
}

static void bus_socketCreate() 
{
    // Bus is idle. Start transmitting/receiving.
    BYTE buffer[4] = { 0x55, 0xaa };
    buffer[2] = s_socketConnected;
    buffer[3] = BUS_MSG_TYPE_CONNECT;
    rs485_write(TRUE, buffer, 4);

    // Don't wait the TX channel to be free, but immediately enqueue socket data, to avoid engage/disengage time and glitches
    // However wait for TX9 to be reusable, so wait for TX to be finished
    s_waitTxQuickEnd = 1;
    // Next state (after TX finishes) is IN COMMAND
    s_busState = BUS_PRIV_STATE_SOCKET_CONNECTED;
}

static void bus_socketPoll() 
{
    // Bus line is slow, though
    BYTE buffer[RS485_BUF_SIZE / 2];
            
    // Data from IP?
    WORD rx = prot_control_readAvail();
    if (rx > 0) {
        // Read data and push it into the line
        if (rx > sizeof(buffer)) {
            rx = sizeof(buffer);
        }
        BYTE av = rs485_writeAvail();
        if (rx > av) {
            rx = av;
        }
        // Transfer rx bytes
        if (rx > 0) {
            prot_control_read(buffer, rx);
            rs485_write(FALSE, buffer, rx);
            s_lastTime = TickGet();
        }
    }
    else {
        // Rx mode
        // Ensure to clear the FERR before entering in this state!
        //if (rs485_getState() == RS485_LINE_RX_FRAME_ERR) {
        //    // Close socket, ferr during rx
        //    // Drop the TCP connection and reset the channel
        //    rs485_clearFerr();
        //    bus_disconnectSocket(SOCKET_ERR_FERR);
        //}

        // Data received?
        WORD tx = rs485_readAvail(); 
        if (tx > 0) {
           
            // Read data and push it into IP
            tx = tx > sizeof(buffer) ? sizeof(buffer) : tx;
            rs485_read(buffer, tx);
            
            prot_control_write(buffer, tx);
          
            // Socket gracefully closed?
            if (rs485_lastRc9) {
                // Now the channel is idle again
                s_busState = BUS_PRIV_STATE_IDLE;
                s_socketConnected = SOCKET_NOT_CONNECTED;
            }
            else {
                s_lastTime = TickGet();
            }
        }
    }
}

int bus_getChildrenMaskSize()
{
    return BUFFER_MASK_SIZE;
}

const BYTE* bus_getChildrenMask()
{
    return s_childKnown;
}

#endif