#include "persistence.h"

// The editable memory area
#asm
psect persistence,reloc=400h,pure,class=WRITABLE_CONST
_g_persistentData:
	dw	(0) & 0xffff
	dw	highword(0)
	dw	(0)&0ffffh
	dw	(0)&0ffffh
	dw	(0) & 0xffff
	dw	highword(0)
	dw	(0) & 0xffff
	dw	highword(0)

        ds  (0x400 - 16)
#endasm
