#include "pch.h"
#include "persistence.h"
#include "appio.h"

PersistentData pers_data
#ifdef _CONF_RASPBIAN
        = DEFAULT_PERS_DATA
#endif
        ;

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
#if defined(HAS_EEPROM)
    rom_read(ROM_ADDR, (BYTE*)&pers_data, sizeof(PersistentData));
#elif defined(_CONF_RASPBIAN)
    FILE* file = fopen("home.mem", "rb");
    if (file) {
        if (fread(&pers_data, sizeof(PersistentData), 1, file) == 1) {
            flog("Persistence file read");
        }
        fclose(file);
    } else {
        flog("Persistence file read err: %d", errno);
    }
#endif
}

void pers_save()
{
#if defined(HAS_EEPROM)
    rom_write(ROM_ADDR, (BYTE*)&pers_data, sizeof(PersistentData));
#elif defined(_CONF_RASPBIAN)
    FILE* file = fopen("home.mem", "wb");
    if (file) {
        if (fwrite(&pers_data, sizeof(PersistentData), 1, file) == 1) {
            flog("Persistence file written");
        }
        fclose(file);
    } else {
        flog("Persistence file write err: %d", errno);
    }
#endif
}
