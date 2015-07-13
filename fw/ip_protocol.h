#ifndef _PROT_H_APP_
#define _PROT_H_APP_

#include <GenericTypeDefs.h>

#define SERVER_CONTROL_UDP_PORT 17007
#define CLIENT_TCP_PORT 20000

void prot_pollControlPort(void);

// Manage slow timer (1sec) activities
void ip_prot_slowTimer(void);

// Process read data
typedef void (*Sink_ReadHandler)(BYTE* data, WORD length);
// Send data?
typedef WORD (*Sink_WriteHandler)(BYTE* data);

#define SINK_TX_BUFFER_SIZE 32

// Class virtual pointers
typedef struct SinkStruct
{	
	// Device ID
	const char fourCc[4];
	// Pointer to RX function
	Sink_ReadHandler readHandler;
	// Pointer to TX function
	Sink_WriteHandler writeHandler;
} Sink;

#endif //#ifdef HAS_IP

