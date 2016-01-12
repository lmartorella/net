#ifndef _PROT_H_APP_
#define _PROT_H_APP_

#ifdef HAS_IP

#define SERVER_CONTROL_UDP_PORT 17007
#define CLIENT_TCP_PORT 20000

// Manage poll activities
void ip_prot_init();
void ip_prot_timer();
void ip_prot_slowTimer();
// Update ETH module timers at ~23Khz freq
void ip_prot_tickUpdate();

typedef union
{
    struct
    {
        unsigned timer_1s: 1;
        unsigned timer_10ms: 1;
    };
    BYTE v;
} TIMER_RES;

// Check if 1sec timer is elapsed, and reset it if so.
TIMER_RES ip_timers_check();

#endif

#endif //#ifdef HAS_IP

