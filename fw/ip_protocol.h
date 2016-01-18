#ifndef _PROT_H_APP_
#define _PROT_H_APP_

#ifdef HAS_IP

#define SERVER_CONTROL_UDP_PORT 17007
#define CLIENT_TCP_PORT 20000

// Manage poll activities
void ip_prot_init();
void ip_prot_slowTimer();
void ip_prot_poll();

#endif

#endif //#ifdef HAS_IP

