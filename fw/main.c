
#include "hardware/fuses.h"
#include "hardware/cm1602.h"
#include "hardware/spiram.h"
#include "hardware/spi.h"
#include "hardware/vs1011e.h"
#include "timers.h"
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

void timer1s(void);
void sendHelo(void);

// [Flags]
typedef enum STATUS_MODE_enum
{	
	// Connected to server?
	MODE_CONNECTED = 1,
	// Helo UDP socked opened?
	MODE_HELO_OPENED = 2,
	// is DHCP ok?
	MODE_DHCP_OK = 4,
} STATUS_MODE;

static STATUS_MODE s_mode = 0;

void print(const rom char* str)
{
	char i;
	cm1602_setDdramAddr(0x40);
	for (i = 0; i < 16; i++)
	{
		cm1602_write(' ');
	}
	cm1602_setDdramAddr(0x40);
	cm1602_writeStr(str);
	ClrWdt();
}

void error(const rom char* str)
{
	print("E:");
	cm1602_writeStr(str);
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

	print("Spi");

	// Enable SPI
	// from 23k256 datasheet and figure 20.3 of PIC datasheet
	// CKP = 0, CKE = 1
	// Output: data sampled at clock falling.
	// Input: data sampled at clock falling, at the end of the cycle.
	spi_init(SPI_SMP_MIDDLE | SPI_CKE_IDLE | SPI_CKP_LOW | SPI_SSPM_CLK_F4);

	sram_init();
	vs1011_init();

	print("ChkRam");
	checkram();

	print("IP");
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

	print("DHCP");

	// Start IP
	DHCPInit(0);
	DHCPEnable(0);

	print("Waiting..");

	// I'm alive
	while (1) 
	{
		// Do ETH stuff
		StackTask();
		if (timers_check1s())
		{
			timer1s();
		}
		ClrWdt();
	}
}

void timer1s()
{
	char buf[17];
	int dhcpOk, dhcpWasOk;
	print("");

	dhcpOk = DHCPIsBound(0) != 0;
	dhcpWasOk = (s_mode & MODE_DHCP_OK) != 0;

	if (dhcpOk != dhcpWasOk)
	{
		if (dhcpOk)
		{
			unsigned char* p = (unsigned char*)(&AppConfig.MyIPAddr);
			sprintf(buf, "%d.%d.%d.%d", (int)p[0], (int)p[1], (int)p[2], (int)p[3]);
			cm1602_setDdramAddr(0x0);
			cm1602_writeStrRam(buf);
			s_mode |= MODE_DHCP_OK;
		}
		else
		{
			s_mode &= ~MODE_DHCP_OK;
			error("DHCP.nok");
		}
	}

	if (dhcpOk)
	{
		// send helo
		if (!(s_mode & MODE_CONNECTED))
		{
			sendHelo();
		}
	}
}

static UDP_SOCKET s_heloSocket;
#define HomeProtocolPort 17007

// Send HELO packet to the helo port
void sendHelo()
{
	int i;
	if (!(s_mode & MODE_HELO_OPENED))
	{
		s_heloSocket = UDPOpenEx(NULL, UDP_OPEN_NODE_INFO, 0, HomeProtocolPort);
		if (s_heloSocket == INVALID_UDP_SOCKET)
		{
			error("HELO.open");
			return;
		}
		s_mode |= MODE_HELO_OPENED;
	}

	// Socket opened
	if (UDPIsPutReady(s_heloSocket) < (16 + 8))
	{
		error("HELO.rdy");
		return;
	}

	UDPPutROMString("HOMEHELO");
	for (i = 0; i < 16; i++)
	{
		UDPPut(0);
	}
	UDPFlush();
}
