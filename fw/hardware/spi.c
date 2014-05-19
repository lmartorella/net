#include "fuses.h"
#include "spi.h"

#define SPIRAM_SPI_IF (PIR1bits.SSP1IF)

//#pragma code spi_section

#define ClearSPIDoneFlag()  SPIRAM_SPI_IF = 0

static void WaitForDataByte(void)   
{
#ifndef __DEBUG
	while (!SPIRAM_SPI_IF); 
	ClearSPIDoneFlag();
#else
#warning DEBUG CODE
#endif
}

#define SPI_ON_BIT     (SPIRAM_SPICON1bits.SSPEN)

// Init SPI as master
void spi_init(enum SPI_INIT value)
{
	// Enable I/O (not automatic)
	// (TCP/IP stack sources does it before enabling SPI)
	TRISCbits.RC3 = 0;		// Enable SCK1
	TRISCbits.RC4 = 1;		// SDI1 as input
	TRISCbits.RC5 = 0;		// Enable SDO1

	// Cycling SSPEN 1->0->1 will reset SPI
	SSP1CON1 = value & 0x1F;	// reset WCOL and SSPOV and SSPEN
	Nop();
	SSP1CON1bits.SSPEN = 1;

	ClearSPIDoneFlag();

	SSP1STAT = value;		// only get 7-6 bits, other are not writable
}

// Send/read a byte MSB
BYTE spi_shift(BYTE data)
{
	// now write data to send
	SSP1BUF = data;
	// now wait until BF is set (data flushed and received)
	WaitForDataByte();
	return SSP1BUF;
}

// Send/read a word MSB
UINT16 spi_shift16(UINT16 data)
{
    BYTE bm = spi_shift(data >> 8);
    BYTE bl = spi_shift((BYTE)data);
    return (bm << 8) + bl;
}

// Send an array and ignore in data
void spi_shift_array(const BYTE* buffer, BYTE size)
{
    for (int i = 0; i < size; i++)
    {
        spi_shift(*(buffer++));
    }
}

// Send a repeated byte
void spi_shift_repeat(BYTE data, BYTE size)
{
    for (int i = 0; i < size; i++)
    {
        spi_shift(data);
    }
}
