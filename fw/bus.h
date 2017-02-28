#ifndef BUS_H
#define	BUS_H

#ifdef HAS_RS485
#ifdef HAS_IP
#define HAS_BUS_SERVER
#else
#define HAS_BUS_CLIENT
#endif
#endif // HAS_RS485

void bus_init();
// Poll general bus activities
void bus_poll();
bit bus_isIdle();

typedef enum { 
    // Message to beat a bean
    BUS_MSG_TYPE_HEARTBEAT = 1,
    // Message to ask unknown bean to present (broadcast)
    BUS_MSG_TYPE_READY_FOR_HELLO = 2,
    // Message to ask the only unknown bean to register itself
    BUS_MSG_TYPE_ADDRESS_ASSIGN = 3,
    // Command/data will follow: socket open
    BUS_MSG_TYPE_CONNECT = 4
} BUS_MSG_TYPE;

typedef enum { 
    // Bean: ack heartbeat
    BUS_ACK_TYPE_HEARTBEAT = 0x20,
    // Bean: notify unknown (response to BUS_MSG_TYPE_READY_FOR_HELLO)
    BUS_ACK_TYPE_HELLO = 0x21
} BUS_ACK_TYPE;

#define UNASSIGNED_SUB_ADDRESS 0xff

#ifdef HAS_BUS_SERVER

typedef enum {
    BUS_STATE_NONE,
    BUS_STATE_SOCKET_CONNECTED,
    BUS_STATE_SOCKET_TIMEOUT,
    BUS_STATE_SOCKET_FERR,
} BUS_STATE;

typedef struct {
    // Count of socket timeouts
    char socketTimeouts;
} BUS_MASTER_STATS;
extern BUS_MASTER_STATS g_busStats;

// Select a child, and start a private communication bridging the IP protocol socket.
void bus_connectSocket(int nodeIdx);
void bus_disconnectSocket(int val);

// Is still in command execution, waiting for command data receive complete?
BUS_STATE bus_getState();
extern bit bus_dirtyChildren;

// Get active children mask & size
int bus_getChildrenMaskSize();
const BYTE* bus_getChildrenMask();

#define SOCKET_NOT_CONNECTED -1
#define SOCKET_ERR_TIMEOUT -2
#define SOCKET_ERR_FERR -3
#define SOCKET_ERR_NO_IP -4

#endif
#endif	/* BUS_H */

