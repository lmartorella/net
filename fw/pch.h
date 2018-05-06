#ifndef PCH_H
#define	PCH_H

#ifdef __XC8
#include <xc.h>
#include <GenericTypeDefs.h>
#define HAS_EEPROM
#endif

#ifdef _CONF_RASPBIAN

typedef unsigned char BYTE;
typedef unsigned short WORD;
typedef unsigned long DWORD;
typedef unsigned char bit;
typedef unsigned char BOOL;
#define __PACK
#define FALSE 0
#define TRUE 1
// For netserver use
#define __GNU
// For POSIX extension
#define _GNU_SOURCE

#include <errno.h>
#include <unistd.h>
#include <stdarg.h>

#endif

#include <stdio.h>
#include <stdlib.h>
#include <string.h>

#include "hardware/hw.h"
#include "hardware/utilities.h"
#include "guid.h"

#ifdef __XC8
// Size optimized
bit memcmp8(void* p1, void* p2, BYTE size);
// Internal core clock drives timer with 1:256 prescaler
#define TICKS_PER_SECOND		(TICK_TYPE)((TICK_CLOCK_BASE + (TICK_PRESCALER / 2ull)) / TICK_PRESCALER)	
#define TICKS_PER_MSECOND		(TICK_TYPE)(TICKS_PER_SECOND / 1000)
#endif

#include "hardware/tick.h"

#ifdef HAS_CM1602
#include "hardware/cm1602.h"
#include "displaySink.h"
#endif
#ifdef HAS_SPI_RAM
#include "hardware/spiram.h"
#endif
#ifdef HAS_SPI
#include "hardware/spi.h"
#endif
#ifdef HAS_VS1011
#include "hardware/vs1011e.h"
#include "audioSink.h"
#endif
#ifdef HAS_DHT11
#include "hardware/dht11.h"
#endif
#ifdef HAS_MAX232_SOFTWARE
#include "halfduplex.h"
#include "hardware/max232.h"
#endif
#ifdef HAS_DCF77
#include "dcf77.h"
#endif
#ifdef HAS_RS485
#include "rs485.h"
#endif
#ifdef HAS_DIGIO
#include "hardware/digio.h"
#endif
#ifdef HAS_LED
#include "hardware/leds.h"
#endif
#ifdef HAS_EEPROM
#include "hardware/eeprom.h"
#endif

#endif	/* PCH_H */

