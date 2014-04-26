// This file is requested by TCP/IP stack

// For Tcp/ip stack
#define GetSystemClock()    (25000000)
#define GetInstructionClock() (25000000/4)
#define GetPeripheralClock() (25000000/4)

#include <TCPIPStack/ETH97J60.h>
