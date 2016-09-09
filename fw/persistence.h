#ifndef PERSISTENCE_INCLUDE_
#define PERSISTENCE_INCLUDE_

#include "bus.h"
#include "hardware/eeprom.h"

/********************
  USER-DATA PART OF LOADER RECORD (PROGRAMMABLE)
  The record is immediately before the Configuration word, in the higher program memory.
  (loader do supports changing configuration words)
*/
typedef struct
{
  	// GUID:  application instance ID (used by user code)
	GUID deviceId;
    
    // Used by PIC16 / bus_client
#ifdef HAS_BUS_CLIENT
    BYTE address;
#endif
    
} PersistentData;

#ifdef HAS_BUS_CLIENT
#define DEFAULT_PERS_DATA { { 0, 0, 0, 0, 0 }, UNASSIGNED_SUB_ADDRESS }
#else
#define DEFAULT_PERS_DATA { { 0, 0, 0, 0, 0 } }
#endif


// This can be accessed by the running application in read-only mode
extern EEPROM_MODIFIER PersistentData g_persistentData;

// Update my copy of persistence
void boot_getUserData(PersistentData* newData);
// Program the new content of the UserData
void boot_updateUserData(PersistentData* newData);

#endif
