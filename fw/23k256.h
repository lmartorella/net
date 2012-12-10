#ifndef _23K256_INCLUDE_
#define _23K256_INCLUDE_

#include "fuses.h"

#define MEM_CS0	portBits(MEM_PORT).MEM_BANK0_CS
#define MEM_CS1	portBits(MEM_PORT).MEM_BANK1_CS
#define MEM_CS2	portBits(MEM_PORT).MEM_BANK2_CS
#define MEM_CS3	portBits(MEM_PORT).MEM_BANK3_CS

typedef unsigned short uint16;
typedef unsigned char byte;

// This will override the spi_init() call
void sram_init(void);

// Read single byte (BYTE MODE, slow)
void sram_write(uint16 address, byte data);
// Write single byte (BYTE MODE, slow)
byte sram_read(uint16 address);


#endif