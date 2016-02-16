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

