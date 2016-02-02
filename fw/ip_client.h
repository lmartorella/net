#ifndef _PROT_H_APP_
#define _PROT_H_APP_

#include "hardware/fuses_mini_bean.h"


#ifdef HAS_IP

// Manage poll activities
void ip_prot_init();

#endif

#ifdef HAS_RS485
#ifdef HAS_IP
#define HAS_RS485_SERVER
#else
#define HAS_RS485_CLIENT
#endif
#endif // HAS_RS485

#endif //#ifdef HAS_IP

