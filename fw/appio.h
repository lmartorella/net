#ifndef _APPIO_INCLUDE_
#define _APPIO_INCLUDE_

#include "protocol.h"

// Print the upper row (status)
void printlnUp(const rom char* msg);
// Display a ROM message in the below row
void println(const rom char* msg);
// Display a RAM message in the below row
void printlnr(const ram char* msg);

// Reset the device with fatal error
void fatal(const rom char* msg);
// Retrieve last fatal error
const ram char* getLastFatal(void);

extern const rom Sink g_displaySink;

#endif