#include "../pch.h"

// CONFIG
#pragma config FOSC = INTOSCIO  // Oscillator Selection bits (INTOSC oscillator: I/O function on RA6/OSC2/CLKOUT pin, I/O function on RA7/OSC1/CLKIN)
#pragma config WDTE = ON        // Watchdog Timer Enable bit (WDT enabled)
#pragma config PWRTE = ON       // Power-up Timer Enable bit (PWRT enabled)
#pragma config MCLRE = ON      // RA5/MCLR/VPP is MCLR
#pragma config BOREN = ON       // Brown-out Detect Enable bit (BOD enabled)
#pragma config LVP = OFF        // Low-Voltage Programming Enable bit (RB4/PGM pin has digital I/O function, HV on MCLR must be used for programming)
#pragma config CPD = OFF        // Data EE Memory Code Protection bit (Data memory code protection off)
#pragma config CP = OFF         // Flash Program Memory Code Protection bit (Code protection off)

#define _XTAL_FREQ SYSTEM_CLOCK

void wait40us(void)
{	
    __delay_us(40);
}

void wait100us(void)
{
    __delay_us(100);
}

// The oscillator works at 25Mhz without PLL, so 1 cycle is 160nS 
void wait2ms(void)
{
    __delay_ms(2);
}

// The oscillator works at 25Mhz without PLL, so 1 cycle is 160nS 
void wait30ms(void)
{
    __delay_ms(30);
}

void wait1s(void)
{
    __delay_ms(1000);
}

void enableInterrupts()
{
	// Disable low/high interrupt mode
	INTCONbits.GIE = 1;
	INTCONbits.PEIE = 1;
}

void sys_storeResetReason()
{
    _reason = RESET_MCLR;
}