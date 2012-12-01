#include "fuses.h"
#include "spi.h"

// Init SPI as master
void spi_init(enum SPI_INIT value)
{
	SSP1STAT = (value & 0xC0);

	// Cycling SSPEN 1->0->1 will reset SPI
	SSP1CON1 = value & 0x1F;
	SSP1CON1bits.SSPEN = 1;

	// Enable I/O (not automatic)
	TRISCbits.RC5 = 0;		// Enable SDO1
	TRISCbits.RC3 = 0;		// Enable SCK1
}

// Send/read MSB
byte spi_shift(byte data)
{
	SSP1BUF = data;
	while (!SSP1STATbits.BF);
	return SSP1BUF;
}
