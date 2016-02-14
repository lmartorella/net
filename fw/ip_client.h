#ifndef _PROT_H_APP_
#define _PROT_H_APP_

#include "hardware/hw.h"

#ifdef HAS_IP

// Manage poll activities
void ip_prot_init();
// Manage slow timer (heartbeats)
void ip_prot_slowTimer(BOOL dirtyChildren);

#endif

#endif //#ifdef HAS_IP

