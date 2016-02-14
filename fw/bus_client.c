#include "pch.h"
#include "hardware/rs485.h"
#include "hardware/tick.h"
#include "bus.h"
#include "protocol.h"

#ifdef HAS_BUS_CLIENT

// Called often
void bus_poll()
{
}

// Close the socket
void prot_control_close()
{
}

// Socket connected?
BOOL prot_control_isConnected()
{
    return TRUE;
}

#endif
