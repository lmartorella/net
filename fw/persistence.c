#include "hardware/fuses.h"
#include "hardware/eeprom.h"
#include "persistence.h"
#include <string.h>
#include "Compiler.h"


void boot_getUserData(PersistentData* newData)
{
	memcpypgm2ram(newData, &g_persistentData, sizeof(PersistentData));
}

void boot_updateUserData(PersistentData* newData)
{
	rom_write((const void*)&g_persistentData, (void*)newData, sizeof(PersistentData));
}