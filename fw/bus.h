#ifndef BUS_H
#define	BUS_H

#include "protocol.h"

#ifdef HAS_RS485
#ifdef HAS_IP
#define HAS_BUS_SERVER
#else
#define HAS_BUS_CLIENT
#endif
#endif // HAS_RS485


#ifdef HAS_BUS_SERVER

// Poll general bus activities
void bus_poll();
// Select a child, and start a private communication bridging the IP protocol socket.
void bus_connectSocket(int nodeIdx);
// Is still in command execution, waiting for command data receive complete?
BOOL bus_isSocketConnected();

#endif
#endif	/* BUS_H */

