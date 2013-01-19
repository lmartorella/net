
#include "hardware/fuses.h"
#include <TCPIP Stack/TCPIP.h>

static DWORD s_1sec;

void low_isr(void);
#pragma code lowVector=0x18
void LowVector(void){_asm goto low_isr _endasm}
#pragma code
#pragma interruptlow low_isr
void low_isr(void)
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