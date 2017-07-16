#include "pch.h"
#include "persistence.h"

PersistentData pers_data;

#ifdef _IS_ETH_CARD
static EEPROM_MODIFIER PersistentData s_persistentData @ 0x1F800 = DEFAULT_PERS_DATA;
static EEPROM_MODIFIER char s_persistentDataFiller[0x400 - PERSISTENT_SIZE] @ (0x1F800 + PERSISTENT_SIZE);
#define ROM_ADDR ((const void*)&s_persistentData)
#elif defined(_IS_PIC16F628_CARD) || defined(_IS_PIC16F1827_CARD)
#define ROM_ADDR 0
static EEPROM_MODIFIER PersistentData s_persistentData = DEFAULT_PERS_DATA;
#endif

void pers_init()
{
#ifdef HAS_EEPROM
    rom_read(ROM_ADDR, (BYTE*)&pers_data, sizeof(PersistentData));
#endif
#ifdef __GNU
    memset(&pers_data, 0, sizeof(pers_data));
#endif
}

void pers_save()
{
#ifdef HAS_EEPROM
    rom_write(ROM_ADDR, (BYTE*)&pers_data, sizeof(PersistentData));
#endif
}
