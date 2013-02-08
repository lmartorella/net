#include "hardware/fuses.h"
#include "hardware/eeprom.h"
#include "persistence.h"
#include <string.h>

// The editable memory area
#pragma romdata PERSISTENT_SECTION
far rom const PersistentData g_persistentData = { {0x00000000, 0x0000, 0x0000, 0x00000000, 0x00000000 } };

#pragma code

void boot_getUserData(ram PersistentData* newData)
{
	memcpypgm2ram(newData, &g_persistentData, sizeof(PersistentData));
}

void boot_updateUserData(const ram PersistentData* newData)
{
	rom_write((far rom void*)&g_persistentData, (ram void*)newData, sizeof(PersistentData));
}