#ifndef _PROT_H_APP_
#define _PROT_H_APP_

// [Flags]
typedef enum PROTOCOL_STATUS_enum
{	
	// Connected to server?
	PROT_CONNECTED = 1,
	// Helo UDP socked opened?
	PROT_HELO_OPENED = 2,
	// is DHCP ok?
	PROT_DHCP_OK = 4,
} PROTOCOL_STATUS;

extern PROTOCOL_STATUS s_protStatus;

// Send a broadcast HELO packet, with the current device ID
void prot_sendHelo(void);

#endif
