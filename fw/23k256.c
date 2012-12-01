#include "23k256.h"
#include "spi.h"
#include "fuses.h"

// This will override the spi_init() call
void sram_init()
{
	// from 23k256 datasheet and figure 20.3 of PIC datasheet
	// CKP = 0, CKE = 1
	// Output: data sampled at clock falling.
	// Input: data sampled at clock falling, at the end of the cycle.
	spi_init(SPI_SMP_MIDDLE | SPI_CKE_IDLE | SPI_CKP_LOW | SPI_SSPM_CLK_F16);
}

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
	MSG_READ = 0x3,
	MSG_WRITE = 0x2,
	MSG_RDSR = 0x5,
	MSG_WRSR = 0x1,
};

void sram_write(uint16 address, byte data)
{
	// Enter byte mode
	spi_shift(MSG_WRSR);
	spi_shift(ST_BYTEMODE | HOLD_DIS);
	
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
	spi_shift(ST_BYTEMODE | HOLD_DIS);
	
	// Write 1 byte
	spi_shift(MSG_READ);
	spi_shift(address >> 8);
	spi_shift(address & 0xff);
	return spi_shift(0);
}
