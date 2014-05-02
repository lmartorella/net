#ifndef _APPIO_INCLUDE_
#define _APPIO_INCLUDE_

#include "protocol.h"

// Print the upper row (status)
void printlnUp(const char* msg);
// Display a RAM/ROM message in the below row
void println(const char* msg);

// Reset the device with fatal error
void fatal(const char* msg);
// Retrieve last fatal error
const char* getLastFatal(void);

// The public sink descriptor
extern const Sink g_displaySink;

#endif