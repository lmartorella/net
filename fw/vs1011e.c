#include "vs1011e.h"

void vs1011_init(void)
{
	// enable cs out and deassert
	PORT(VS1011_PORT).VS1011_CS = 1;
	TRIS(VS1011_PORT).VS1011_CS = 0;
}