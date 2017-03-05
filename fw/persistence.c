#include "pch.h"
#include "hardware/eeprom.h"
#include "persistence.h"

PersistentData g_userData;

#ifdef _IS_ETH_CARD
static EEPROM_MODIFIER PersistentData g_persistentData @ 0x1F800;
static EEPROM_MODIFIER char g_persistentDataFiller[0x400 - 0x12] @ 0x1F812;
#define ROM_ADDR ((const void*)&g_persistentData)
#elif defined(_IS_PIC16F628_CARD)
#define ROM_ADDR 0
static EEPROM_MODIFIER PersistentData g_persistentData = DEFAULT_PERS_DATA;
#endif

void boot_getUserData()
{
	rom_read(ROM_ADDR, (BYTE*)&g_userData, sizeof(PersistentData));
}

void boot_updateUserData()
{
	rom_write(ROM_ADDR, (BYTE*)&g_userData, sizeof(PersistentData));
}
