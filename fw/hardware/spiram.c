#include "spiram.h"
#include "spi.h"
#include "fuses.h"

#define MEM_CSTRIS0	MEM_TRISBITS.MEM_BANK0_CS
#define MEM_CSTRIS1	MEM_TRISBITS.MEM_BANK1_CS
#define MEM_CSTRIS2	MEM_TRISBITS.MEM_BANK2_CS
#define MEM_CSTRIS3	MEM_TRISBITS.MEM_BANK3_CS

static void en0(void) { MEM_CS0 = 0; }
static void en1(void) { MEM_CS1 = 0; }
static void en2(void) { MEM_CS2 = 0; }
static void en3(void) { MEM_CS3 = 0; }

typedef void(*action)(void); 
static action en[] = { en0, en1, en2, en3 };  

enum STATUSREG
{
	ST_BYTEMODE = 0x0,
	ST_PAGEMODE = 0x80,
	ST_SEQMODE = 0x40,
	HOLD_EN = 0x0,
	HOLD_DIS = 0x1,
};

enum MSG
{
	MSG_READ = 0x3,		// Read data from memory array beginning at selected address
	MSG_WRITE = 0x2,	// Write data to memory array beginning at selected address
	MSG_RDSR = 0x5,		// Read Status register
	MSG_WRSR = 0x1,		// Write Status register
};

static void disableAll(void)
{	
	// Set all CS to 1 (disabled)
	MEM_PORT |= MEM_BANK_CS_MASK;
}

// This will override the spi_init() call
void sram_init()
{
	char b;
	// Drive SPI RAM chip select pin
	MEM_CSTRIS0 = 0;
	MEM_CSTRIS1 = 0;
	MEM_CSTRIS2 = 0;
	MEM_CSTRIS3 = 0;

	disableAll();

	for (b = 0; b < 4; b++)
	{
		en[b]();
		// Enter full range mode
		spi_shift(MSG_WRSR);
		spi_shift(ST_SEQMODE | HOLD_DIS);
		disableAll();
	}
}

static UINT16 enableBank(UINT32 addr)
{
	// support 4 banks = 128k
	addr &= 0x1ffff;
	en[addr >> 15]();
	return (UINT16)addr;
}

// Write a vector of bytes in RAM.
// NOTE: do not support SPI cross-bank access
//  - *dest is in banked PIC RAM
//  - address is logic SPIRAM address of the first byte to write
//  - count is the count of byes to write
void sram_write(BYTE* src, UINT32 address, BYTE count)
{
	UINT16 raddr;
	if (count == 0)
	{
		return;
	}
	spi_shift(MSG_WRITE);
	raddr = enableBank(address);
	spi_shift(raddr >> 8);
	spi_shift((BYTE)raddr);
	do 
	{
		// Write 1 byte
		spi_shift(*(src++));
		ClrWdt();
	}
	while (--count > 0);
	disableAll();
}

// Read a vector of bytes in RAM.
// NOTE: do not support SPI cross-bank access
//  - *dest is in banked PIC RAM
//  - address is logic SPIRAM address of the first byte to read
//  - count is the count of byes to read
void sram_read(BYTE* dest, UINT32 address, BYTE count)
{
	UINT16 raddr;
	if (count == 0)
	{
		return;
	}
	spi_shift(MSG_READ);
	raddr = enableBank(address);
	spi_shift(raddr >> 8);
	spi_shift((BYTE)raddr);
	do 
	{
		// Read 1 byte
		*(dest++) = spi_shift(0);
		ClrWdt();
	}
	while (--count > 0);
	disableAll();
}

// Test all 4 banks, return the number of the failing bank
// or -1 if no fails
signed char sram_test(void)
{
	// To use a random seq string that spans on all banks
	// write first, read then
	BYTE b;
	BYTE test;
	UINT32 addr = 0x1234;

	// Do some test with banks
	for (b = 0; b < 4; b++, addr += 0x8000)
	{
		sram_write(&b, addr, 1);
	}
	addr = 0x1234;
	for (b = 0; b < 4; b++, addr += 0x8000)
	{
		sram_read(&test, addr, 1);
		if (test != b)
		{
			return b;
		}
	}
	return -1;
}
