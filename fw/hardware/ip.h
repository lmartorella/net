#ifndef _IP_INCLUDE_
#define	_IP_INCLUDE_

#include "Compiler.h"

#include "../TCPIPStack/TCPIP.h"
extern UDP_SOCKET s_heloSocket;  // TODO: static

// Manage poll activities
void ip_prot_init(void);
void ip_prot_poll(void);
void ip_prot_slowTimer(void);
void ip_control_close(void);

BOOL ip_control_readW(WORD* w);
BOOL ip_control_read(void* data, WORD size);
void ip_control_writeW(WORD w);
void ip_control_write(void* data, WORD size);
void ip_control_flush(void);

WORD ip_control_getDataSize(void);
BOOL ip_control_isListening(void);        

#endif	/* IP_H */

