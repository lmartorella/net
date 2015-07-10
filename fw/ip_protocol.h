#ifndef _PROT_H_APP_
#define _PROT_H_APP_

#include "hardware/utilities.h"

#define SERVER_CONTROL_UDP_PORT 17007
#define CLIENT_TCP_PORT 20000

// Manage poll activities
void ip_prot_init(void);
void ip_prot_poll(void);

// Manage slow timer (1sec) activities
void ip_prot_slowTimer(void);

// Class virtual pointers
typedef struct SinkStruct
{	
	// Device ID
	const char fourCc[4];
	// Pointer to create function
	Action createHandler;
	// Pointer to destroy function
	Action destroyHandler;
	// Pointer to POLL
	Action pollHandler;
} Sink;

#endif //#ifdef HAS_IP

