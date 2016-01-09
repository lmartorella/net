#include "pch.h"
#include "hardware/eeprom.h"
#include "persistence.h"

#ifdef _CONF_MCU_ETH_CARD
const PersistentData g_persistentData @ 0x1F800 = { {0x00000000, 0x0000, 0x0000, 0x00000000, 0x00000000 } };
const char g_persistentDataFiller[0x400 - 16] @ 0x1F810;

void boot_getUserData(PersistentData* newData)
{
	memcpy(newData, &g_persistentData, sizeof(PersistentData));
}

void boot_updateUserData(PersistentData* newData)
{
	rom_write((const void*)&g_persistentData, (void*)newData, sizeof(PersistentData));
}

#endif
