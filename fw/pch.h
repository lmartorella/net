#ifndef PCH_H
#define	PCH_H

#include <xc.h>
#include <GenericTypeDefs.h>
#include <stdio.h>
#include <string.h>
#include "hardware/hw.h"
#include "hardware/utilities.h"
#include "guid.h"

// Size optimized
bit memcmp8(void* p1, void* p2, BYTE size);

#endif	/* PCH_H */

