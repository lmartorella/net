#include "hardware/fuses.h"
#include "hardware/utilities.h"
#include "hardware/eeprom.h"
#include "persistence.h"
#include <string.h>

// The editable memory area
#pragma romdata PERSISTENT_SECTION
far rom const PersistentData g_persistentData = { {0x00000000, 0x0000, 0x0000, { 0x00000000, 0x00000000} } };

#pragma code

static int X()
{
	rom_write(&g_persistentData, NULL, 16);
	return 1;
}