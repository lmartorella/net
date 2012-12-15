#include "23k256.h"
#include "spi.h"
#include "fuses.h"

#define MEM_CSTRIS0	MEM_TRISBITS.MEM_BANK0_CS
#define MEM_CSTRIS1	MEM_TRISBITS.MEM_BANK1_CS
#define MEM_CSTRIS2	MEM_TRISBITS.MEM_BANK2_CS
#define MEM_CSTRIS3	MEM_TRISBITS.MEM_BANK3_CS

static void en0(void) { MEM_CS0 = 0; }
static void dis0(void) { MEM_CS0 = 1; }
static void en1(void) { MEM_CS1 = 0; }
static void dis1(void) { MEM_CS1 = 1; }
static void en2(void) { MEM_CS2 = 0; }
static void dis2(void) { MEM_CS2 = 1; }
static void en3(void) { MEM_CS3 = 0; }
static void dis3(void) { MEM_CS3 = 1; }

typedef void(*action)(void); 
static action en[] = { en0, en1, en2, en3 };  
static action dis[] = { dis0, dis1, dis2, dis3 };  

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

// This will override the spi_init() call
void sram_init()
{
	char b;
	// Drive SPI RAM chip select pin
	MEM_CSTRIS0 = 0;
	MEM_CSTRIS1 = 0;
	MEM_CSTRIS2 = 0;
	MEM_CSTRIS3 = 0;

	for (b = 0; b < 4; b++)
	{
		dis[b]();
	}
}

void sram_write(UINT16 address, BYTE data)
{
	// Write 1 byte
	spi_shift(MSG_WRITE);
	spi_shift(address >> 8);
	spi_shift((BYTE)address);
	spi_shift(data);
}

BYTE sram_read(UINT16 address)
{
	// Write 1 byte
	spi_shift(MSG_READ);
	spi_shift(address >> 8);
	spi_shift((BYTE)address);
	return spi_shift(0);
}

// Test all 4 banks, return the number of the failing bank
// or -1 if no fails
char sram_test(void)
{
	char b;
	char test;

	// Do some test with banks
	for (b = 0; b < 4; b++)
	{
		en[b]();
		// Enter full range mode
		spi_shift(MSG_WRSR);
		spi_shift(ST_SEQMODE | HOLD_DIS);
		dis[b]();

		en[b]();
		sram_write(0x1234, 0x56);
		dis[b]();

		en[b]();
		test = sram_read(0x1234);
		dis[b]();
		if (test != 0x56)
		{
			return b;
		}
	}
	return -1;
}
