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

#ifdef SHORT_FATAL
// Reset the device with fatal error
#define fatal(msg) { s_lastErr = msg; RESET(); }
#else
// Reset the device with fatal error
void fatal(const char* msg);
#endif

extern persistent const char* s_lastErr;

// Get last reset reason as 3 char code
void sys_storeResetReason();
const char* sys_getResetReasonStr();
BOOL sys_isResetReasonExc();

#endif
