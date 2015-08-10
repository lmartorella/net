#ifndef _PROT_H_APP_
#define _PROT_H_APP_

#include <GenericTypeDefs.h>
#include "ver.h"

#define SERVER_CONTROL_UDP_PORT 17007
#define CLIENT_TCP_PORT 20000

void prot_poll(void);
void prot_slowTimer(void);

// Process WRIT message: read data for sink
// Return TRUE to continue to read, FALSE if read process finished
typedef BOOL (*Sink_ReadHandler)();
// Process READ message: write data from sink in one go
typedef void (*Sink_WriteHandler)();

// Class virtual pointers
typedef struct SinkStruct
{	
	// Device ID
	FOURCC fourCc;
	// Pointer to RX function
	Sink_ReadHandler readHandler;
	// Pointer to TX function
	Sink_WriteHandler writeHandler;
} Sink;

#endif //#ifdef HAS_IP

