#include "hardware/fuses.h"
#include "hardware/utilities.h"
#include "hardware/cm1602.h"
#include "hardware/spiram.h"
#include "hardware/spi.h"
#include "hardware/vs1011e.h"
#include "protocol.h"
#include "timers.h"
#include "appio.h"
#include "TCPIPStack/TCPIP.h"
#include "audioSink.h"
#include <stdio.h>

// The smallest type capable of representing all values in the enumeration type.
enum RESET_REASON
{
	RESET_POWER = 0,  // Power-on reset
	RESET_BROWNOUT,
	RESET_CONFIGMISMATCH,
	RESET_WATCHDOG,
	RESET_STACKFAIL,
	RESET_MCLR,
	RESET_EXC
};
static enum RESET_REASON _reason;

static const char* msg1 = "Hi world! ";
static const char* g_reasonMsgs[] = { 
				"POR",
				"BOR",
				"CFG",
				"WDT",
				"STK",
				"RST",
				"EXC:"  };

APP_CONFIG AppConfig;
static BOOL s_dhcpOk = FALSE;

// Check RCON and STKPTR register for anormal reset cause
static void storeResetReason(void)
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


static void enableInterrupts(void)
{
	// Enable low/high interrupt mode
	RCONbits.IPEN = 1;		
	INTCONbits.GIEL = 1;
	INTCONbits.GIEH = 1;
}

void timer1s(void);
void sendHelo(void);

static BYTE buffer[32];

void main()
{
    // Analyze RESET reason
    storeResetReason();

    // reset display
    cm1602_reset();
    cm1602_clear();
    cm1602_setEntryMode(MODE_INCREMENT | MODE_SHIFTOFF);
    cm1602_enable(ENABLE_DISPLAY | ENABLE_CURSOR | ENABLE_CURSORBLINK);

    cm1602_setDdramAddr(0);
    cm1602_writeStr(msg1);
    cm1602_writeStr(g_reasonMsgs[_reason]);
    if (_reason == RESET_EXC)
    {
        cm1602_setDdramAddr(0x40);
        cm1602_writeStr(getLastFatal());
    }

    wait1s();
    println("Spi");

    // Enable SPI
    // from 23k256 datasheet and figure 20.3 of PIC datasheet
    // CKP = 0, CKE = 1
    // Output: data changed at clock falling.
    // Input: data sampled at clock rising.
    spi_init(SPI_SMP_END | SPI_CKE_IDLE | SPI_CKP_LOW | SPI_SSPM_CLK_F4);

    sram_init();
    vs1011_setup();

    wait1s();
    println("ChkRam");
    sram_test_gui(buffer, sizeof(buffer));
    clearlnUp();
    
    println("IP/DHCP");
    memset(&AppConfig, 0, sizeof(AppConfig));
    AppConfig.Flags.bIsDHCPEnabled = 1;
    AppConfig.MyMACAddr.v[0] = MY_DEFAULT_MAC_BYTE1;
    AppConfig.MyMACAddr.v[1] = MY_DEFAULT_MAC_BYTE2;
    AppConfig.MyMACAddr.v[2] = MY_DEFAULT_MAC_BYTE3;
    AppConfig.MyMACAddr.v[3] = MY_DEFAULT_MAC_BYTE4;
    AppConfig.MyMACAddr.v[4] = MY_DEFAULT_MAC_BYTE5;
    AppConfig.MyMACAddr.v[5] = MY_DEFAULT_MAC_BYTE6;

    enableInterrupts();
    timers_init();

    // Start IP
    DHCPInit(0);
    DHCPEnable(0);

    println("Waiting..");

    // I'm alive
    while (1)
    {
            TIMER_RES timers = timers_check();

            if (timers.timer_10ms)
            {
                // Do ETH stuff
                StackTask();
                // This tasks invokes each of the core stack application tasks
                StackApplications();
                if (s_dhcpOk)
                {
                    prot_poll();
                }
            }

            if (timers.timer_1s)
            {
                timer1s();
                if (s_dhcpOk)
                {
                    prot_slowTimer();
                }
            }

            audio_pollMp3Player();
            ClrWdt();
    }
}

void timer1s()
{
    int dhcpOk;
    println("");

    dhcpOk = DHCPIsBound(0) != 0;

    if (dhcpOk != s_dhcpOk)
    {
            if (dhcpOk)
            {
                    unsigned char* p = (unsigned char*)(&AppConfig.MyIPAddr);
                    sprintf(buffer, "%d.%d.%d.%d", (int)p[0], (int)p[1], (int)p[2], (int)p[3]);
                    cm1602_setDdramAddr(0x0);
                    cm1602_writeStr(buffer);
                    s_dhcpOk = TRUE;
            }
            else
            {
                    s_dhcpOk = FALSE;
                    fatal("DHCP.nok");
            }
    }
}

