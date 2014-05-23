#include "hardware/fuses.h"
#include "TCPIPStack/TCPIP.h"
#include "audioSink.h"
#include "timers.h"

static DWORD s_1sec;
static DWORD s_10msec;
#define TICKS_1S ((DWORD)TICK_SECOND)
#define TICKS_10MS (DWORD)(TICKS_1S / 100)

void interrupt low_priority low_isr(void)
{
    // Update ETH module timers at ~23Khz freq
    TickUpdate();
}

void timers_init()
{
    // Init ETH Ticks on timer0 (low prio) module
    TickInit();
    // Init ETH loop data
    StackInit();
    // Align 1sec to now()
    s_1sec = s_10msec = TickGet();
}

TIMER_RES timers_check()
{
    TIMER_RES res;
    res.v = 0;
    DWORD now = TickGet();
    if ((now - s_1sec) >= TICKS_1S)
    {
        s_1sec = now;
        res.timer_1s = 1;
    }
    if ((now - s_10msec) >= TICKS_10MS)
    {
        s_10msec = now;
        res.timer_10ms = 1;
    }
    return res;
}