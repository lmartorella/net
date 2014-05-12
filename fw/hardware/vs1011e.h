#ifndef _VS1011E_INCLUDE_
#define _VS1011E_INCLUDE_

#include "fuses.h"

typedef enum 
{
    VS1011_MODEL_VS1001 = 0,
    VS1011_MODEL_VS1011 = 1,
    VS1011_MODEL_VS1011E = 2,
    VS1011_MODEL_VS1003 = 3,
    // Memory test fail
    VS1011_MODEL_HWFAIL = 0xff
} VS1011_MODEL;

// Init hardware ports and chip
// Returns the VS1011 MODEL
VS1011_MODEL vs1011_init(void);

#endif
