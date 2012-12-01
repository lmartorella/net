#ifndef _23K256_INCLUDE_
#define _23K256_INCLUDE_

typedef unsigned short uint16;
typedef unsigned char byte;

// This will override the spi_init() call
void sram_init(void);

// Read single byte (BYTE MODE, slow)
void sram_write(uint16 address, byte data);
// Write single byte (BYTE MODE, slow)
byte sram_read(uint16 address);

#endif