
#include "fuses.h"
#include "cm1602.h"
#include "23k256.h"
#include "spi.h"
#include "vs1011e.h"
#include <TCPIP Stack/TCPIP.h>
#include <stdio.h>

// The smallest type capable of representing all values in the enumeration type.
enum RESET_REASON
{
	RESET_POWER = 0,  // Power-on reset
	RESET_BROWNOUT,
	RESET_CONFIGMISMATCH,
	RESET_WATCHDOG,
	RESET_STACKFAIL,
	RESET_MCLR
};
static enum RESET_REASON _reason;

static const rom char* msg1 = "Hi world! ";
static const rom char* g_reasonMsgs[] = { 
				"POR",
				"BOR",
				"CFG",
				"WDT",
				"STK",
				"RST"  };

APP_CONFIG AppConfig;

// Check RCON and STKPTR register for anormal reset cause
static void storeResetReason(void)
{
	if (!RCONbits.NOT_RI)
	{
		RCONbits.NOT_RI = 1;
		// Software exception. _reset contains SW code.
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

static volatile unsigned char _tm2elapsed = 0;
static volatile unsigned char _tm2Count = 0;

void Tmr2Update(void)
{
	if (PIR1bits.TMR2IF)
	{
		// Reset interrupt flag
		PIR1bits.TMR2IF = 0;
		// Increment internal high tick counter (additional 1:256 postscaler)
		if (++_tm2Count == 0)
		{
			_tm2elapsed = 1;	
		}
	}
}

void low_isr(void);
#pragma code lowVector=0x18
void LowVector(void){_asm goto low_isr _endasm}
#pragma interruptlow low_isr
void low_isr(void)
{
	// Update ETH module timers
	TickUpdate();
	Tmr2Update();
}


static void enableInterrupts(void)
{
	// Enable low/high interrupt mode
	RCONbits.IPEN = 1;		
	INTCONbits.GIEL = 1;
	INTCONbits.GIEH = 1;
}

static void checkram(void)
{
	char err = sram_test();
	if (err >= 0)
	{
		cm1602_setDdramAddr(0x40);
		cm1602_writeStr("bankfail#");
		cm1602_write(err + '0');
	}
}

void main()
{
	// Analyze RESET reason
	storeResetReason();

	wait30ms();

	// reset display
	cm1602_reset();
	cm1602_clear();
	cm1602_setEntryMode(MODE_INCREMENT | MODE_SHIFTOFF);
	cm1602_enable(ENABLE_DISPLAY | ENABLE_CURSOR | ENABLE_CURSORBLINK);

	cm1602_setDdramAddr(0);
	cm1602_writeStr(msg1);
	cm1602_writeStr(g_reasonMsgs[_reason]);

	sram_init();
	vs1011_init();

	// Enable SPI
	// from 23k256 datasheet and figure 20.3 of PIC datasheet
	// CKP = 0, CKE = 1
	// Output: data sampled at clock falling.
	// Input: data sampled at clock falling, at the end of the cycle.
	spi_init(SPI_SMP_MIDDLE | SPI_CKE_IDLE | SPI_CKP_LOW | SPI_SSPM_CLK_F4);

	checkram();

	memset(&AppConfig, 0, sizeof(AppConfig));
	AppConfig.Flags.bIsDHCPEnabled = 1;
	AppConfig.MyMACAddr.v[0] = MY_DEFAULT_MAC_BYTE1;
	AppConfig.MyMACAddr.v[1] = MY_DEFAULT_MAC_BYTE2;
	AppConfig.MyMACAddr.v[2] = MY_DEFAULT_MAC_BYTE3;
	AppConfig.MyMACAddr.v[3] = MY_DEFAULT_MAC_BYTE4;
	AppConfig.MyMACAddr.v[4] = MY_DEFAULT_MAC_BYTE5;
	AppConfig.MyMACAddr.v[5] = MY_DEFAULT_MAC_BYTE6;

	// Init ETH Ticks on timer0 (low prio) module
	TickInit();
	StackInit();

	// Install 1-sec timer on timer2 (low prio, to demultiplex)
	T2CONbits.TOUTPS = 0xF; // 1:16 postscaler
	T2CONbits.T2CKPS = 0x3; // 1:16 prescaler, so freq = 25Mhz / 16 / 16 = 24.41KHz.
	PR2 = 95;			// final freq. 256 Hz. that /256 = 1sec
	T2CONbits.TMR2ON = 1;	// enable timer

	PIR1bits.TMR2IF = 0;
	IPR1bits.TMR2IP = 0; // low prio
	PIE1bits.TMR2IE = 1;

	enableInterrupts();

	// Start IP
	DHCPInit(0);
	DHCPEnable(0);

	// I'm alive
	while (1) 
	{
		StackTask();
		if (_tm2elapsed)
		{
			int i;
			_tm2elapsed = 0;

			cm1602_setDdramAddr(0x00);
			for (i = 0; i < 16; i++)
			{
				cm1602_write(' ');
			}	
			if (DHCPIsBound(0))
			{
				char buf[17];
				unsigned char* p = (unsigned char*)(&AppConfig.MyIPAddr);
				cm1602_setDdramAddr(0x00);
				sprintf(buf, "%d.%d.%d.%d", (int)p[0], (int)p[1], (int)p[2], (int)p[3]);
				cm1602_writeStrRam(buf);
			}
		}
		ClrWdt();
	}
}