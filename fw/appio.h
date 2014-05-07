#ifndef _APPIO_INCLUDE_
#define _APPIO_INCLUDE_

// Clear the upper row (status)
void clearlnUp();
// Clear the below row
void clearln();
// Clear and print string in the upper row (status)
void printlnUp(const char* msg);
// Clear and print string in the below row
void println(const char* msg);
// Print a char in the current row at the cursor
void printch(char ch);

// Reset the device with fatal error
void fatal(const char* msg);
// Retrieve last fatal error
const char* getLastFatal(void);

#endif