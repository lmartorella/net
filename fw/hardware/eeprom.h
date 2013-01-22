#ifndef _ROM_INCLUDE_H_
#define _ROM_INCLUDE_H_

// Erase the entire row (destination should be multiple of ROW_SIZE = 1Kb)
// and then copy the source bytes to the start of row, length should be at max 1Kb
void rom_write(far rom void* destination, ram BYTE* source, WORD length);

#endif