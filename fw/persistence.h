#ifndef PERSISTENCE_INCLUDE_
#define PERSISTENCE_INCLUDE_

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
extern far rom const PersistentData g_persistentData;

// Program the new content of the UserData
void boot_updateUserData(const ram PersistentData* newData);

#endif