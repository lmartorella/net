#ifndef PERSISTENCE_INCLUDE_
#define PERSISTENCE_INCLUDE_

#include "ver.h"

/********************
  USER-DATA PART OF LOADER RECORD (PROGRAMMABLE)
  The record is immediately before the Configuration word, in the higher program memory.
  (loader do supports changing configuration words)
*/
typedef struct
{
  	// GUID:  application instance ID (used by user code)
	GUID deviceId;
} PersistentData;

// This can be accessed by the running application in read-only mode
extern const PersistentData g_persistentData;

// Update my copy of persistence
void boot_getUserData(PersistentData* newData);
// Program the new content of the UserData
void boot_updateUserData(PersistentData* newData);

#endif
