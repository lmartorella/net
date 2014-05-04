#include "persistence.h"

//#pragma psect mediumconst=persistence,reloc=4
const PersistentData g_persistentData @ 0x1F800 = { {0x00000000, 0x0000, 0x0000, 0x00000000, 0x00000000 } };
const char g_persistentDataFiller[0x400 - 16] @ 0x1F810;
