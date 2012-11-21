#include "cm1602.h"
#include "fuses.h"

#define CMD_CLEAR 		0x1
#define CMD_HOME 		0x2
#define CMD_ENTRY 		0x4
#define CMD_ENABLE 		0x8
#define CMD_SHIFT 		0x10
#define CMD_FUNCSET		0x20
#define CMD_SETCGRAM	0x40
#define CMD_SETDDRAM	0x80

#define CMD_FUNCSET_DL_8	0x10
#define CMD_FUNCSET_DL_4	0x00
#define CMD_FUNCSET_LN_2	0x08
#define CMD_FUNCSET_LN_1	0x00
#define CMD_FUNCSET_FS_11	0x04
#define CMD_FUNCSET_FS_7	0x00

#ifndef CM1602_IF_PORT
#error CM1602_IF_PORT not set
#endif
#ifndef CM1602_IF_BIT_RW
#error CM1602_IF_BIT_RW not set
#endif
#ifndef CM1602_IF_BIT_EN
#error CM1602_IF_BIT_EN not set
#endif
#ifndef CM1602_IF_BIT_RS
#error CM1602_IF_BIT_RS not set
#endif

// Clock the control bits in order to push the 4/8 bits to the display.
// In case of 4-bit, the lower data is sent and the HIGH part should be ZERO
static void pulsePort(byte data)
{
#if CM1602_IF_MODE == 4
	byte oldData;
#endif

	CM1602_IF_BIT_EN = 1;

#if CM1602_IF_MODE == 4
	// wait for eventual bits in port to stabilize (BIT_EN could be on the same port)
	_asm 
		 nop
	_endasm

	#if CM1602_IF_NIBBLE == 'low'
		oldData = (CM1602_IF_PORT & 0xf0);
		// Direct write, takes only the low part
		// 7-4 bits of data should be ZERO
		CM1602_IF_PORT = data | oldData;
	#elif CM1602_IF_NIBBLE == 'high'
		oldData = (CM1602_IF_PORT & 0x0f);
		// Direct write, takes only the high part
		CM1602_IF_PORT = (data << 4) | oldData;
	#else
		#error CM1602_IF_NIBBLE should be set to 'low' or 'high'
	#endif
#else
	CM1602_IF_PORT = data;
#endif
	CM1602_IF_BIT_EN = 0;
}

// write a byte to the port
static void writeByte(byte data)
{
#if CM1602_IF_MODE == 4
	// 4-bit interface. Send high first
	pulsePort(data >> 4);
	pulsePort(data & 0xf);
#elif CM1602_IF_MODE == 8
	// 8-bit interface. Send all
	pulsePort(data);
#else
	#error CM1602_IF_MODE should be set to 4 or 8
#endif
}

static void writeCmd(byte data)
{
	CM1602_IF_BIT_RS = 0;
	writeByte(data);
}

static void writeData(byte data)
{
	CM1602_IF_BIT_RS = 1;
	writeByte(data);
}

void cm1602_reset(void)
{
	byte cmd = CMD_FUNCSET | 
#if CM1602_LINE_COUNT == 1
		CMD_FUNCSET_LN_1
#elif CM1602_LINE_COUNT == 2
		CMD_FUNCSET_LN_2
#else
#error CM1602_LINE_COUNT should be set to 1 or 2
#endif
	|
#if CM1602_IF_MODE == 8
		CMD_FUNCSET_DL_8
#else
		CMD_FUNCSET_DL_4
#endif
	|
#if CM1602_FONT_HEIGHT == 7
		CMD_FUNCSET_FS_7;
#elif CM1602_FONT_HEIGHT == 10
		CMD_FUNCSET_FS_11;
#else
#error CM1602_FONT_HEIGHT should be set to 7 or 10
#endif

	CM1602_IF_BIT_RW = 0;
	waitms(30);

#if CM1602_IF_MODE == 4
	CM1602_IF_BIT_RS = 0;
	pulsePort(cmd >> 4);		// Enables the 4-bit mode
	writeCmd(cmd);
#else
	writeCmd(cmd);
#endif
	waitus(39);
}

void cm1602_clear(void)
{
	writeCmd(CMD_CLEAR);
	waitms(2);
}

void cm1602_home(void)
{
	writeCmd(CMD_HOME);
	waitms(2);
}

void cm1602_setEntryMode(enum CM1602_ENTRYMODE mode)
{
	writeCmd(CMD_ENTRY | mode);
	waitus(39);
}

void cm1602_enable(enum CM1602_ENABLE enable)
{
	writeCmd(CMD_ENABLE | enable);
	waitus(39);
}

void cm1602_shift(enum CM1602_SHIFT data)
{
	writeCmd(CMD_SHIFT | data);
	waitus(39);
}

void cm1602_setCgramAddr(byte address)
{
	writeCmd(CMD_SETCGRAM | address);
	waitus(39);
}

void cm1602_setDdramAddr(byte address)
{
	writeCmd(CMD_SETDDRAM | address);
	waitus(39);
}

void cm1602_write(byte data)
{
	writeData(data);
	waitus(43);
}

