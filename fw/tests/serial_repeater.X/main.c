#include "../../pch.h"
#include <stdio.h>
#include <string.h>
#include "../../appio.h"
#include "../../hardware/tick.h"
#include "../../hardware/rs485.h"

// ==== ETH CARD based on 18f87j60

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

// Debugger
#pragma config DEBUG = ON
    
// No code protection
#pragma config CP0 = OFF

// Long (callable) version of fatal
void fatal(const char* str)
{
    wait30ms();
    RESET(); // generates RCON.RI
}

void enableInterrupts()
{
    // Disable low/high interrupt mode
    RCONbits.IPEN = 0;		
    INTCONbits.GIE = 1;
    INTCONbits.PEIE = 1;
    INTCON2bits.TMR0IP = 0;		// TMR0 Low priority
    
#ifdef HAS_RS485
    // Enable low priority interrupt on transmit
    RS485_INIT_INT();
#endif
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

void interrupt PRIO_TYPE low_isr()
{
    // Update tick timers at ~Khz freq
    TickUpdate();
#ifdef HAS_RS485
    rs485_interrupt();
#endif
}

void main() {
    appio_init();
    // Init Ticks on timer0 (low prio) module
    timers_init();
    
    printlnUp("Serial Repeater");
    char buf[16];
    sprintf(buf, "%d baud ", RS485_BAUD);
    char* point = buf + strlen(buf) - 1;
    println(buf);
    
    enableInterrupts();
    
    TICK_TYPE time = TickGet();
    BOOL headerReady = 0;
    BYTE header[2];
    WORD* pSize = (WORD*)header; 
    BYTE data[16];
    int packetCount = 0;
    
    while (1) {
        TICK_TYPE now = TickGet();
        if (now - time > TICKS_PER_SECOND) {
            *point = *point ^ (' ' ^ '.'); 
            println(buf);
            time = now;
        }
        CLRWDT();
        
        if (!headerReady) {
            if (rs485_readAvail() >= 4) {
                rs485_read(header, 1);
                if (header[0] == 0x55) {
                    rs485_read(header, 1);
                    if (header[0] == 0xAA) {
                        rs485_read(header, 2);
                        if (*pSize <= 16) {
                            headerReady = 1;
                        }
                    }
                }
            }
        } else {
            if (rs485_readAvail() >= *pSize) {
                // Read data
                rs485_read(data, *pSize);
                // Send back
                rs485_write(0, data, *pSize);
                headerReady = 0;

                packetCount++;
                sprintf(data, "%d packets", packetCount);
                printlnUp(data);
            }
        }
    }
}
