#include "hardware/fuses.h"
#include "hardware/eeprom.h"
#include "persistence.h"
#include <string.h>
#include "Compiler.h"

// The editable memory area
far const PersistentData g_persistentData = { {0x00000000, 0x0000, 0x0000, 0x00000000, 0x00000000 } };

void boot_getUserData(PersistentData* newData)
{
	memcpypgm2ram(newData, &g_persistentData, sizeof(PersistentData));
}

void boot_updateUserData(PersistentData* newData)
{
	rom_write((const void*)&g_persistentData, (void*)newData, sizeof(PersistentData));
}