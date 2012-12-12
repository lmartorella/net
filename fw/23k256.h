#ifndef _23K256_INCLUDE_
#define _23K256_INCLUDE_

#include "fuses.h"

#define MEM_CS0	MEM_PORTBITS.MEM_BANK0_CS
#define MEM_CS1	MEM_PORTBITS.MEM_BANK1_CS
#define MEM_CS2	MEM_PORTBITS.MEM_BANK2_CS
#define MEM_CS3	MEM_PORTBITS.MEM_BANK3_CS

// This will override the spi_init() call
void sram_init(void);
// Test all 4 banks, return the number of the failing bank
// or -1 if no fails
char sram_test(void);

// Read single byte (BYTE MODE, slow)
void sram_write(UINT16 address, BYTE data);
// Write single byte (BYTE MODE, slow)
BYTE sram_read(UINT16 address);


#endif