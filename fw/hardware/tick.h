#ifndef __TICK_H
#define __TICK_H

// All TICKS are stored as 32-bit unsigned integers.

// This value is used by TCP and other modules to implement timeout actions.
// For this definition, the Timer must be initialized to use a 1:256 prescalar
// in Tick.c.  

// Internal core clock drives timer with 1:256 prescaler
#define TICKS_PER_SECOND		((TICK_CLOCK_BASE + (TICK_PRESCALER / 2ull)) / TICK_PRESCALER)	

// Represents one second in Ticks
#define TICK_SECOND				((QWORD)TICKS_PER_SECOND)
// Represents one minute in Ticks
#define TICK_MINUTE				((QWORD)TICKS_PER_SECOND * 60ull)
// Represents one hour in Ticks
#define TICK_HOUR				((QWORD)TICKS_PER_SECOND * 3600ull)

typedef union
{
    struct
    {
        unsigned timer_1s: 1;
        unsigned timer_10ms: 1;
        unsigned timer_1ms : 1;
    };
    BYTE v;
} TIMER_RES;

DWORD TickGet(void);
DWORD TickGetDiv256(void);
DWORD TickGetDiv64K(void);
void TickUpdate(void);

void timers_init(void);
// Check if 1sec timer is elapsed, and reset it if so.
TIMER_RES timers_check();

#endif
