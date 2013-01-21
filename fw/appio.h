#ifndef _APPIO_INCLUDE_
#define _APPIO_INCLUDE_

// Print the upper row (status)
void printlnUp(const rom char* msg);
// Display a ROM message in the below row
void println(const rom char* msg);
// Display a ROM error in the below row
void error(const rom char* msg);

#endif