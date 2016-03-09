#include "pch.h"
#include "hardware/rs485.h"
#include "hardware/tick.h"
#include "bus.h"
#include "protocol.h"
#include "persistence.h"
#include "appio.h"
#include "hardware/leds.h"

#ifdef HAS_BUS_CLIENT

static enum {
    STATE_HEADER_0 = 0,         // header0
    STATE_HEADER_1 = 1,         // header1
    STATE_HEADER_2 = 2,         // header2
    STATE_HEADER_ADRESS = 2,         // header2
    STATE_HEADER_3 = 3,
            
    STATE_SOCKET_OPEN = 10,
    STATE_WAIT_TX
            
} s_state;

static BYTE s_header[3] = { 0x55, 0xAA, 0 };
static bit s_availForAddressAssign;
static BYTE s_tempAddressForAssignment;
#define UNASSIGNED_ADDRESS 0xff

static void bus_reinit()
{
    s_state = STATE_HEADER_0;
    // Skip bit9 = 0
    rs485_skipData = TRUE;
}

static void bus_prepareMyAddress()
{
    PersistentData data;
    boot_getUserData(&data);

    s_availForAddressAssign = FALSE;

    // Address should be reset?
    if (g_resetReason == RESET_MCLR) {
        // Reset address
        data.address = UNASSIGNED_ADDRESS;       
        boot_updateUserData(&data);
        led_on();
        
        s_availForAddressAssign = TRUE;
    }

    s_header[2] = data.address;
}

void bus_init()
{
    bus_prepareMyAddress();
    bus_reinit();
}

static void bus_storeAddress()
{
    PersistentData data;
    boot_getUserData(&data);
    data.address = s_header[2] = s_tempAddressForAssignment;
    boot_updateUserData(&data);
    
    s_availForAddressAssign = FALSE;
    led_off();
}

static void bus_sendAck(BYTE ackCode) {
    // Respond with a socket response
    rs485_write(FALSE, s_header, 3);
    rs485_write(FALSE, &ackCode, 1);
    // And then wait for TX end before going idle
    s_state = STATE_WAIT_TX;
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
        if (rs485_readAvail() > 0) {            
            // RC9 will be 1
            rs485_read(&buf, 1);
            // Waiting for header?
            if (s_state < STATE_HEADER_3) {
                // Keep an eye to address if in assign state
                if (s_state == STATE_HEADER_ADRESS && s_availForAddressAssign) {
                    // Store the byte, in case of s_skipNextAddressCheck
                    s_tempAddressForAssignment = buf;
                    // Go ahead
                    s_state++;
                }
                else if (buf == s_header[s_state]) {
                    // Header matches. Go next.
                    s_state++;
                }
                else {
                    // Restart from 0x55
                    bus_reinit();
                }
            }
            else {
                // Header correct. Now read the command and respond
                switch (buf) { 
                    case BUS_MSG_TYPE_ADDRESS_ASSIGN:
                        if (s_availForAddressAssign) {
                            // Store the new address in memory
                            bus_storeAddress();
                            bus_sendAck(BUS_ACK_TYPE_HEARTBEAT);
                        }
                        break;
                       
                    case BUS_MSG_TYPE_HEARTBEAT:
                        // Only respond to heartbeat if has address
                        if (s_header[2] != UNASSIGNED_ADDRESS) {
                            bus_sendAck(BUS_ACK_TYPE_HEARTBEAT);
                        }
                        break;
                        
                    case BUS_MSG_TYPE_READY_FOR_HELLO:
                        // Only respond to hello if ready to program
                        if (s_availForAddressAssign) {
                            bus_sendAck(BUS_ACK_TYPE_HELLO);
                        }
                        break;
                       
                    case BUS_MSG_TYPE_CONNECT:
                        if (s_header[2] != UNASSIGNED_ADDRESS) {
                            // Start reading data with rc9 not set
                            rs485_skipData = rs485_lastRc9 = FALSE;
                            // Socket, direct connect
                            s_state = STATE_SOCKET_OPEN;
                        }
                        break;
                    default:
                        // Unknown command. Reset
                        // Restart from 0x55
                        bus_reinit();
                        break;
                }
            }
        }
    }
}

// Close the socket
void prot_control_close()
{
    if (s_state == STATE_SOCKET_OPEN) {
        // Respond OK with bit9=1, that closes the bus
        rs485_write(TRUE, "\x1E", 1);
        
        // Idle bus
        bus_reinit();
    }
}

// Socket connected?
bit prot_control_isConnected()
{
    return s_state == STATE_SOCKET_OPEN;
}

#endif
