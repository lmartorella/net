
#include "programmer.h"
#include "../fw/fuses.h"

static void DoFlash(UINT16 startBlock, UINT16 lastBlock);

#pragma code loaderrec=0x1ffe0
static const struct LoaderRecord LREC =
{
	{0,0,0,0,0,0},
	{0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0},
	{0,0},
	{0,0},
	&DoFlash,
	(0x1ffd0 / 64)
};
#pragma code 

#pragma code loadercode=0x1ffd0
void DoFlash(UINT16 startBlock, UINT16 lastBlock)
{
}
#pragma code 

void main()
{
}