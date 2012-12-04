
#include "fuses.h"
#include "cm1602.h"
#include "23k256.h"
#include <TCPIP Stack/TCPIP.h>

// The smallest type capable of representing all values in the enumeration type.
enum RESET_REASON
{
	RESET_POWER,  // Power-on reset
	RESET_BROWNOUT,
	RESET_CONFIGMISMATCH,
	RESET_WATCHDOG,
	RESET_STACKFAIL,
	RESET_MCLR
};
static enum RESET_REASON _reason;

// Check RCON and STKPTR register for anormal reset cause
static void storeResetReason(void)
{
	if (!RCONbits.NOT_RI)
	{
		// Software exception. _reset contains SW code.
		return;
	}
	if (!RCONbits.NOT_POR)
	{
		// Normal Power-on startup. Ok.
		RCONbits.NOT_POR = 1;
		_reason = RESET_POWER;
		return;
	}
	if (!RCONbits.NOT_BOR)
	{
		// Brown-out reset. Low voltage.
		RCONbits.NOT_BOR = 1;
		_reason = RESET_BROWNOUT;
		return;
	}
/*
	if (!RCONbits.NOT_CM)
	{
		// Configuration mismatch reset. EEPROM fail.
		RCONbits.NOT_CM = 1;
		_reason = RESET_CONFIGMISMATCH;
		return;
	}
*/
	if (!RCONbits.NOT_TO)
	{
		// Watchdog reset. Loop detected.
		RCONbits.NOT_TO = 1;
		_reason = RESET_WATCHDOG;
		return;
	}
	if (STKPTRbits.STKFUL || STKPTRbits.STKUNF)
	{
		// Stack underrun/overrun reset. 
		STKPTRbits.STKFUL = STKPTRbits.STKUNF = 0;
		_reason = RESET_STACKFAIL;
		return;
	}
	// Else it was reset manually (MCLR)
	_reason = RESET_MCLR;
}

static const rom char* msg1 = "Hello world!";

static void testBank(char bank)
{
	cm1602_write(bank + '0');
	cm1602_write(':');
	sram_write(0x1234, 0x56);
	if (sram_read(0x1234) != 0x56)
	{
		cm1602_write('N');	
	}
	else
	{
		cm1602_write('Y');	
	}
	cm1602_write(',');
}

void __interrupt INT()
{
	TickUpdate();
}

void main()
{
	// Analyze RESET reason
	storeResetReason();

	// Enable all PORTE as output (display)
	PORTE = 0xff;
	TRISE = 0;
	
	// Enable CS ram banks
	PORTC = 0xff;
	TRISCbits.RC1 = 0;
	TRISCbits.RC2 = 0;
	TRISCbits.RC6 = 0;
	TRISCbits.RC7 = 0;

	wait30ms();

	// reset display
	cm1602_reset();
	cm1602_clear();
	cm1602_setEntryMode(MODE_INCREMENT | MODE_SHIFTOFF);
	cm1602_enable(ENABLE_DISPLAY | ENABLE_CURSOR | ENABLE_CURSORBLINK);

	cm1602_setDdramAddr(0);
	cm1602_writeStr(msg1);

	cm1602_setDdramAddr(0x40);

	// Enable SPI
	sram_init();

	// Do some test with banks
	RAMBANK0_CS = 0;
	testBank(0);
	RAMBANK0_CS = 1;
	RAMBANK1_CS = 0;
	testBank(1);
	RAMBANK1_CS = 1;
	RAMBANK2_CS = 0;
	testBank(2);
	RAMBANK2_CS = 1;
	RAMBANK3_CS = 0;
	testBank(3);
	RAMBANK3_CS = 1;

	// Init Ticks()
	TickInit();

	// I'm alive
	while (1) ClrWdt();
}