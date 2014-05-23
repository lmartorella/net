#ifndef _TIMERS_APP_H_
#define _TIMERS_APP_H_

#include "GenericTypeDefs.h"

// Init timers stuff
void timers_init(void);

typedef struct
{
    union
    {
        unsigned timer_1s: 1;
        unsigned timer_10ms: 1;
    };
    BYTE v;
} TIMER_RES;

// Check if 1sec timer is elapsed, and reset it if so.
TIMER_RES timers_check();

#endif
