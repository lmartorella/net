#ifndef _TIMERS_APP_H_
#define _TIMERS_APP_H_

// Init timers stuff
void timers_init(void);
// Check if 1sec timer is elapsed, and reset it if so.
BYTE timers_check1s(void);

#endif
