
#include "hardware/fuses.h"
#include "TCPIPStack/TCPIP.h"
#include "audioSink.h"

static DWORD s_1sec;

void interrupt low_priority low_isr(void)
{
    // Update ETH module timers at ~23Khz freq
    BYTE b = TickUpdate();

    // Prescale to ~1.5khz
    if ((b % 16) == 0)
    {
        audio_pollMp3Player();
    }
}

void timers_init()
{
	// Init ETH Ticks on timer0 (low prio) module
	TickInit();
	// Init ETH loop data
	StackInit();
	// Align 1sec to now()
	s_1sec = TickGet();
}

BYTE timers_check1s()
{
	DWORD now = TickGet();
	if ((now - s_1sec) >= TICK_SECOND)
	{
		s_1sec = now;
		return 1;
	}
	else
	{
		return 0;
	}
}