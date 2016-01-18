// This file is requested by TCP/IP stack

// For Tcp/ip stack
#define GetSystemClock()    (PERIPHERAL_CLOCK)
#define GetInstructionClock() (PERIPHERAL_CLOCK/4)
#define GetPeripheralClock() (PERIPHERAL_CLOCK/4)

#include <TCPIPStack/ETH97J60.h>
