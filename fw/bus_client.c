#include "pch.h"
#include "hardware/rs485.h"
#include "hardware/tick.h"
#include "bus.h"
#include "protocol.h"

#ifdef HAS_BUS_CLIENT

static enum {
    STATE_IDLE
} s_state;

void bus_init()
{
    s_state = STATE_IDLE;
}

// Called often
void bus_poll()
{
    BYTE b = rs485_readAvail();
    BYTE buf;
    BOOL rc9;
    
    // Read data
    if (s_state == STATE_IDLE) {
        // Read 1 char
        if (b > 0) {
            rs485_read(&buf, 1, &rc9);
            if (rc9 && buf == 0x55) {
                // Start header
            }
        }
    }
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
