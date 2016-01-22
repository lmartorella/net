#include "pch.h"
#include "hardware/eeprom.h"
#include "persistence.h"

#ifdef _CONF_MCU_ETH_CARD
EEPROM_MODIFIER PersistentData g_persistentData @ 0x1F800 = { {0x00000000, 0x0000, 0x0000, 0x00000000, 0x00000000 } };
EEPROM_MODIFIER char g_persistentDataFiller[0x400 - 16] @ 0x1F810;
#define ROM_ADDR ((const void*)&g_persistentData)
#elif _CONF_MINI_BEAN
#define ROM_ADDR 0
EEPROM_MODIFIER PersistentData g_persistentData = { {0x00000000, 0x0000, 0x0000, 0x00000000, 0x00000000 } };
#endif

void boot_getUserData(PersistentData* newData)
{
	rom_read(ROM_ADDR, (void*)newData, sizeof(PersistentData));
}

void boot_updateUserData(PersistentData* newData)
{
	rom_write(ROM_ADDR, (void*)newData, sizeof(PersistentData));
}
