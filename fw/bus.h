#ifndef BUS_H
#define	BUS_H

#ifdef HAS_RS485
#ifdef HAS_IP
#define HAS_BUS_SERVER
#else
#define HAS_BUS_CLIENT
#endif
#endif // HAS_RS485

#ifdef HAS_BUS_SERVER || HAS_BUS_CLIENT
#define HAS_BUS
#endif

void bus_poll();

typedef enum { 
    // Message to beat a bean
    BUS_MSG_TYPE_HEARTBEAT = 1,
    // Message to ask unknown bean to present (broadcast)
    BUS_MSG_TYPE_READY_FOR_HELLO = 2,
    // Message to ask the only unknown bean to register itself
    BUS_MSG_TYPE_ADDRESS_ASSIGN = 3,
    // Command/data will follow: socket open
    BUS_MSG_CONNECT = 4
} BUS_MSG_TYPE;

typedef enum { 
    // Bean: ack heartbeat
    BUS_ACK_TYPE_HEARTBEAT = 1,
    // Bean: notify unknown (response to BUS_MSG_TYPE_READY_FOR_HELLO)
    BUS_ACK_TYPE_HELLO = 2,
} BUS_ACK_TYPE;

#ifdef HAS_BUS_SERVER

typedef enum {
    BUS_STATE_NONE,
    BUS_STATE_SOCKET_CONNECTED,
    BUS_STATE_SOCKET_TIMEOUT,
    BUS_STATE_DIRTY_CHILDREN
} BUS_STATE;


void bus_init();
// Poll general bus activities
void bus_poll();
// Select a child, and start a private communication bridging the IP protocol socket.
void bus_connectSocket(int nodeIdx);
void bus_disconnectSocket();

// Is still in command execution, waiting for command data receive complete?
BUS_STATE bus_getState();

// Get known children count
int bus_getAliveCountAndResetDirty();


#endif
#endif	/* BUS_H */

