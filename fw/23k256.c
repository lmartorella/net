#include "23k256.h"
#include "spi.h"
#include "fuses.h"

#define RAMBANK0_CSTRIS	TRIS_RAM.RAMBANK0bit
#define RAMBANK1_CSTRIS	TRIS_RAM.RAMBANK1bit
#define RAMBANK2_CSTRIS	TRIS_RAM.RAMBANK2bit
#define RAMBANK3_CSTRIS	TRIS_RAM.RAMBANK3bit

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
	// Drive SPI RAM chip select pin
	RAMBANK0_CS = 1;
	RAMBANK0_CSTRIS = 0;
	RAMBANK1_CS = 1;
	RAMBANK1_CSTRIS = 0;
	RAMBANK2_CS = 1;
	RAMBANK2_CSTRIS = 0;
	RAMBANK3_CS = 1;
	RAMBANK3_CSTRIS = 0;

	// from 23k256 datasheet and figure 20.3 of PIC datasheet
	// CKP = 0, CKE = 1
	// Output: data sampled at clock falling.
	// Input: data sampled at clock falling, at the end of the cycle.
	spi_init(SPI_SMP_MIDDLE | SPI_CKE_IDLE | SPI_CKP_LOW | SPI_SSPM_CLK_F4);
}

void sram_write(uint16 address, byte data)
{
	// Enter byte mode
	spi_shift(MSG_WRSR);
	spi_shift(ST_SEQMODE | HOLD_DIS);

	// TEST
	wait40us();

	// Write 1 byte
	spi_shift(MSG_WRITE);
	spi_shift(address >> 8);
	spi_shift(address & 0xff);
	spi_shift(data);
}

byte sram_read(uint16 address)
{
	// Enter byte mode
	spi_shift(MSG_WRSR);
	spi_shift(ST_SEQMODE | HOLD_DIS);

	// TEST
	wait40us();
	
	// Write 1 byte
	spi_shift(MSG_READ);
	spi_shift(address >> 8);
	spi_shift(address & 0xff);
	return spi_shift(0);
}
