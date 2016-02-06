#ifndef BUS_H
#define	BUS_H

#ifdef HAS_RS485
#ifdef HAS_IP
#define HAS_BUS_SERVER
#else
#define HAS_BUS_CLIENT
#endif
#endif // HAS_RS485


#ifdef HAS_BUS_SERVER

typedef enum {
    BUS_SOCKET_NONE,
    BUS_SOCKET_CONNECTED,
    BUS_SOCKET_TIMEOUT,
} BUS_SOCKET_STATE;


void bus_init();
// Poll general bus activities
void bus_poll();
// Select a child, and start a private communication bridging the IP protocol socket.
void bus_connectSocket(int nodeIdx);
// Is still in command execution, waiting for command data receive complete?
BUS_SOCKET_STATE bus_isSocketConnected();

#endif
#endif	/* BUS_H */

