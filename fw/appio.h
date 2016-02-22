#ifndef _APPIO_INCLUDE_
#define _APPIO_INCLUDE_

void appio_init();

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

// The smallest type capable of representing all values in the enumeration type.
typedef enum 
{
	RESET_POWER = 1,  // Power-on reset
	RESET_BROWNOUT,
	RESET_CONFIGMISMATCH,
	RESET_WATCHDOG,
	RESET_STACKFAIL,
	RESET_MCLR,
	RESET_EXC
} RESET_REASON;

#ifdef SHORT_FATAL
// Reset the device with fatal error
#define fatal(msg) { g_exception = msg; RESET(); }
#else
// Reset the device with fatal error
void fatal(const char* msg);
#endif

extern persistent const char* g_exception;
extern const char* g_lastException;
extern RESET_REASON g_resetReason;

// Get last reset reason as 3 char code
void sys_storeResetReason();
BOOL sys_isResetReasonExc();

#endif
