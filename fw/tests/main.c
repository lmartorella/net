#include "hardware/fuses.h"
#include "hardware/utilities.h"
#include "hardware/cm1602.h"
#include <stdio.h>


void main()
{
	wait30ms();

	// reset display
	cm1602_reset();
	cm1602_clear();
	cm1602_setEntryMode(MODE_INCREMENT | MODE_SHIFTOFF);
	cm1602_enable(ENABLE_DISPLAY | ENABLE_CURSOR | ENABLE_CURSORBLINK);
}
