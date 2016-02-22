#include "pch.h"
#include "hardware/rs485.h"
#include "hardware/tick.h"
#include "bus.h"
#include "protocol.h"
#include "persistence.h"
#include "appio.h"

#ifdef HAS_BUS_CLIENT

static enum {
    STATE_HEADER_0 = 0,         // header0
    STATE_HEADER_1 = 1,         // header1
    STATE_HEADER_2 = 2,         // header2
    STATE_HEADER_3 = 3,
            
    STATE_SOCKET_OPEN = 10,
    STATE_WAIT_TX
            
} s_state;

static BYTE s_header[3] = { 0x55, 0xAA, 0 };

static void bus_reinit()
{
    s_state = STATE_HEADER_0;
    // Skip bit9 = 0
    rs485_skipData = TRUE;
}

static BYTE get_address()
{
    PersistentData data;
    boot_getUserData(&data);
    return data.address;
}

void bus_init()
{
    s_header[2] = get_address();
    bus_reinit();
}

// Called often
void bus_poll()
{
    if (s_state >= STATE_SOCKET_OPEN) {
        switch (s_state) {
            case STATE_SOCKET_OPEN: 
                if (rs485_lastRc9) {
                    // Received a break char, go idle
                    bus_reinit();
                }
                // Else do nothing
                break;
            case STATE_WAIT_TX: 
                if (rs485_getState() != RS485_LINE_TX) {
                    bus_reinit();
                }
                break;
        }
    }
    else {
        // Header decode
        BYTE buf;
        while (rs485_readAvail() > 0) {            
            // RC9 will be 1
            rs485_read(&buf, 1);
            // Waiting for header?
            if (s_state < STATE_HEADER_3) {
                if (buf == s_header[s_state]) {
                    // Header matches. Go next.
                    ++s_state;
                }
                else {
                    // Restart from 0x55
                    bus_reinit();
                }
            }
            else {
                printch(3);
                // Header correct. now read the command
                switch (buf) { 
                    case BUS_MSG_TYPE_HEARTBEAT:
                        // Respond with a socket response
                        rs485_write(FALSE, s_header, 3);
                        buf = BUS_ACK_TYPE_HEARTBEAT;
                        rs485_write(FALSE, &buf, 1);
                        // And then wait for TX end before going idle
                        s_state = STATE_WAIT_TX;
                        break;
                    case BUS_MSG_CONNECT:
                        // Start reading data with rc9 not set
                        rs485_skipData = FALSE;
                        // Socket, direct connect
                        s_state = STATE_SOCKET_OPEN;
                        break;
                    default:
                        // Unknown command. Reset
                        // Restart from 0x55
                        bus_reinit();
                }
            }
        }
    }
}

// Close the socket
void prot_control_close()
{
    if (s_state == STATE_SOCKET_OPEN) {
        // Write with bit9=1
        rs485_write(TRUE, "\xff", 1);
        
        // Idle bus
        bus_reinit();
    }
}

// Socket connected?
BOOL prot_control_isConnected()
{
    return s_state == STATE_SOCKET_OPEN;
}

#endif
