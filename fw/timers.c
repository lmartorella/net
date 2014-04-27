
#include "hardware/fuses.h"
#include "TCPIPStack/TCPIP.h"

static DWORD s_1sec;

void interrupt low_priority low_isr(void)
{
	// Update ETH module timers
	TickUpdate();
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