#ifndef _SPI_INCLUDE_
#define _SPI_INCLUDE_

enum SPI_INIT
{
	// **** 
	// **** SSPxSTAT section
	// **** 
	// (Master mode): input data sampled at the end of the output time
	SPI_SMP_END = 0x80,	
	// (Master mode): input data sampled at middle of the output time
	SPI_SMP_MIDDLE = 0x00,		// (good for 23k256)

	// Transmit when clock Active -> idle
	SPI_CKE_IDLE = 0x40,	// (good for 23k256)	
	// Transmit when clock idle -> Active
	SPI_CKE_ACTIVE = 0x00,	

	// **** 
	// **** SSPxCON1 section
	// **** 
	// Clock polarity: idle high
	SPI_CKP_HIGH = 0x10,
	// Clock polarity: idle low
	SPI_CKP_LOW = 0x00,   // (good for 23k256)

	// Clock = Fosc/4
	SPI_SSPM_CLK_F4 = 0x0,	// (good for 23k256)
	// Clock = Fosc/16
	SPI_SSPM_CLK_F16 = 0x1,
	// Clock = Fosc/64
	SPI_SSPM_CLK_F64 = 0x2,
	// Clock = TMR2/2
	//SPI_SSPM_CLK_TMR2 = 0x3,
};

// Init SPI as master
void spi_init(enum SPI_INIT value);
// Send/read MSB
BYTE spi_shift(BYTE data);

#endif