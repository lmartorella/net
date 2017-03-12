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
    BYTE filler;
#endif
    
} PersistentData;

#ifdef HAS_BUS_CLIENT
#define DEFAULT_PERS_DATA { { 0, 0, 0, 0, 0 }, UNASSIGNED_SUB_ADDRESS, 0xff }
#define PERSISTENT_SIZE 0x12
#else
#define DEFAULT_PERS_DATA { { 0, 0, 0, 0, 0 } }
#define PERSISTENT_SIZE 0x10
#endif

// The cached copy of the EEPROM data, read at startup/init
// and then saved explicitly
extern PersistentData pers_data;

// Update copy of persistence
void pers_init();
// Poll WR 
#define pers_poll rom_poll
// Program the new content of the UserData
void pers_save();

#endif
