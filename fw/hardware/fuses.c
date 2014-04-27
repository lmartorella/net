#include "fuses.h"
#include <delays.h>
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
#pragma config DEBUG = OFF
// No code protection
#pragma config CP0 = OFF



// The oscillator works at 25Mhz without PLL, so 1 cycle is 160nS 
void wait40us(void)
{	
	// 40us = ~256 * 160ns
	// So wait 256 cycles
	Delay10TCYx(26);
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
