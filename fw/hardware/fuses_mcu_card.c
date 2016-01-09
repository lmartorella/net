#include "../pch.h"

// For the list of fuses, run "mcc18.exe --help-config -p=18f87j60"

// Enable WDT, 1:256 prescaler
#pragma config WDT = ON
#pragma config WDTPS = 256

// Enable stack overflow reset en bit
#pragma config STVR = ON

// Microcontroller mode ECCPA/P2A multiplexed (not used!)
//#pragma config ECCPMX = 1/0
//#pragma config CCP2MX = 1/0

// Ethernet led enabled. RA0/1 are multiplexed with LEDA/LEDB
#pragma config ETHLED = ON

// Fail-safe clock monitor enabled (switch to internal oscillator in case of osc failure)
#pragma config FCMEN = OFF
// Two-speed startup disabled (Internal/External Oscillator Switchover)
#pragma config IESO = OFF

// Disable extended instruction set (not supported by XC compiler)
#pragma config XINST = OFF

// FOSC = HS, no pll, INTRC disabled
#pragma config FOSC = HS, FOSC2 = ON

// No debugger
#pragma config DEBUG = ON
// No code protection
#pragma config CP0 = OFF



// The oscillator works at 25Mhz without PLL, so 1 cycle is 160nS 
void wait40us(void)
{	
    // 40us = ~256 * 160ns
    // So wait 256 cycles
    Delay10TCYx(26);
}

void wait100us(void)
{
    wait40us();
    wait40us();
    wait40us();
}

// The oscillator works at 25Mhz without PLL, so 1 cycle is 160nS 
void wait2ms(void)
{
	// 2ms = ~12500 * 160ns
	Delay1KTCYx(13);
}

// The oscillator works at 25Mhz without PLL, so 1 cycle is 160nS 
void wait30ms(void)
{
	// 30ms = ~187500 * 160ns
	Delay1KTCYx(188);
}

void wait1s(void)
{
    for (int i = 0; i < 33; i++)
    {
        wait30ms();
        CLRWDT();
    }
}

void enableInterrupts()
{
	// Enable low/high interrupt mode
	RCONbits.IPEN = 1;		
	INTCONbits.GIEL = 1;
	INTCONbits.GIEH = 1;
}
 
// Check RCON and STKPTR register for anormal reset cause
void sys_storeResetReason()
{
	if (!RCONbits.NOT_RI)
	{
		// Software exception. 
		// Obtain last reason from appio.h 
		_reason = RESET_EXC;
	}
	else if (!RCONbits.NOT_POR)
	{
		// Normal Power-on startup. Ok.
		_reason = RESET_POWER;
	}
	else if (!RCONbits.NOT_BOR)
	{
		// Brown-out reset. Low voltage.
		_reason = RESET_BROWNOUT;
	}
/*
	else if (!RCONbits.NOT_CM)
	{
		// Configuration mismatch reset. EEPROM fail.
		_reason = RESET_CONFIGMISMATCH;
	}
*/
	else if (!RCONbits.NOT_TO)
	{
		// Watchdog reset. Loop detected.
		_reason = RESET_WATCHDOG;
	}
	else if (STKPTRbits.STKFUL || STKPTRbits.STKUNF)
	{
		// Stack underrun/overrun reset. 
		_reason = RESET_STACKFAIL;
	}
	else
	{
		// Else it was reset manually (MCLR)
		_reason = RESET_MCLR;
	}
	RCON = RCON | 0x33;	// reset all flags
	STKPTRbits.STKFUL = STKPTRbits.STKUNF = 0;
}
