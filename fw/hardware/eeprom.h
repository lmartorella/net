#ifndef _ROM_INCLUDE_H_
#define _ROM_INCLUDE_H_

#ifdef _IS_ETH_CARD

// PIC18 has memcpy
#define rom_read(rom,ram,size) memcpy(ram,rom,size)

// Erase the entire row (destination should be multiple of ROW_SIZE = 1Kb)
// and then copy the source bytes to the start of row, length should be at max 1Kb
void rom_write(const void* destination, const void* source, WORD length);
#define rom_poll()

#define EEPROM_MODIFIER const

#elif defined(_IS_PIC16F628_CARD)

void rom_read(BYTE sourceAddress, BYTE* destination, BYTE length);
void rom_write(BYTE destinationAddr, const BYTE* source, BYTE length);

#define EEPROM_MODIFIER eeprom

void rom_poll();

#elif defined(_IS_PIC16F1827_CARD)

#define rom_poll()
#define rom_read(a, b, c)
#define rom_write(a, b, c)

#endif
#endif
