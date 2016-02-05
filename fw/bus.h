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
// Select a child, and send command header and data.
// Starts waiting for data back
void bus_openCommand(int nodeIdx, const FOURCC* cmd, const BYTE* data, int dataSize);
// Is still in command execution, waiting for command data receive complete?
BOOL bus_isExecCommand();

#endif
#endif	/* BUS_H */

