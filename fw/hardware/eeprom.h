#ifndef _ROM_INCLUDE_H_
#define _ROM_INCLUDE_H_

#ifdef _IS_ETH_CARD

// PIC18 has memcpy
#define rom_read(rom,ram,size) memcpy(ram,rom,size)

// Erase the entire row (destination should be multiple of ROW_SIZE = 1Kb)
// and then copy the source bytes to the start of row, length should be at max 1Kb
void rom_write(const void* destination, const void* source, WORD length);

#define EEPROM_MODIFIER const

#elif _CONF_MINI_BEAN

void rom_read(int sourceAddress, BYTE* destination, WORD length);
void rom_write(int destinationAddr, const BYTE* source, WORD length);

#define EEPROM_MODIFIER eeprom

#endif
#endif
