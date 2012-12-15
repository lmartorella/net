#include "vs1011e.h"

void vs1011_init(void)
{
	// enable cs out and deassert
	VS1011_PORTBITS.VS1011_CS = 1;
	VS1011_TRISBITS.VS1011_CS = 0;
}