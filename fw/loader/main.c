
#include "programmer.h"
#include "../fw/fuses.h"
#include "../fw/23k256.h"

#define START_MEM (MAX_PROG_MEM - 256)

static void DoFlash();

#pragma code loaderrec = 0x1ffd8   // LOADER_PTR = MAX_PROG_MEM - 0x28
static const struct LoaderRecord LREC = 
{
	{0,0,0,0,0,0},
	{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
	{0,0},
	{0,0},
	&DoFlash,
	(START_MEM / ROM_BLOCK_SIZE)
};

static BYTE s_bitmap[256];

/***** 256-bytes in high mem *****/
#pragma code loadercode = 0x1ff00	// START_MEM = MAX_PROG_MEM - 256

static void en0(void) { MEM_CS0 = 0; }
static void en1(void) { MEM_CS1 = 0; }
static void en2(void) { MEM_CS2 = 0; }
static void en3(void) { MEM_CS3 = 0; }

typedef void(*action)(void); 
static action en[] = { en0, en1, en2, en3 };  

/* 
	Select the right bank of the given bank (0-2047 on a PIC18 with 128K)
	The last 4 banks (256) are reserved for the bank bitmap
*/
static void selectRam(UINT16 block)
{
	// Select the right bank from the block number
	// block count = 11 bits
	// Check BITs 10/9 for bank selection
	en[block >> 9]();
	
}

static void unselectRam()
{
	// All 1 to CSs
	MEM_PORT |= MEM_BANK_CS_MASK;
}

void DoFlash()
{	
	// Disable ALL interrupts
	INTCON.GIEH = 0;	
	INTCON.GIEL = 0;	

	// Read validity bitmap from SPI RAM
	
}

void main()
{
}

#pragma code 
