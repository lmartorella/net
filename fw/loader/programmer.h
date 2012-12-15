#ifndef _PROGRAMMER_BOOTSTRAP_H_
#define _PROGRAMMER_BOOTSTRAP_H_

#include "../fw/utilities.h"
#include "../fw/fuses.h"

/********************

  LOADER RECORD
  Start at SZ-0x20:

  0x00:  SZ-0x20:  6b:    free
  0x06:  SZ-0x1A:  GUID:  application instance ID (used by user code)
  0x16:  SZ-0x0A:  WORD:  application version
  0x18:  SZ-0x08:  WORD:  programmer version
  0x1A:  SZ-0x06:  DWORD: doFlash(word startBlock, word endBlock) pointer
  0x1E:  SZ-0x02:  WORD:  Program max size in blocks (64-bytes), starting from 0 
					      (makes programmer code safe). Equals to the block where the
		                  programmer starts.
*/


typedef void (*DoFlashHandler)(UINT16 startBlock, UINT16 lastBlock);

#define LOADER_PTR (MAX_PROG_MEM - sizeof(LoaderRecord) - 0x08)

struct LoaderRecord
{
	BYTE free[6];
	GUID appId;
    VERSION appVersion;
	VERSION programmerVersion;
    DoFlashHandler flashHandler;
	UINT16 blockFree;
};

#endif
