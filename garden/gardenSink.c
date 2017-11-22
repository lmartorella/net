#include "program.h"
#include "state.h"
#include "../fw/pch.h"
#include "../fw/protocol.h"

static enum {
    RS_START,
    RS_READZONES
} s_readState;
static WORD s_readZoneCount;
static BYTE s_zoneTimes[SUPPORTED_ZONES];

bit gsink_start;

void gsink_init() {
    s_readState = RS_START;
    gsink_start = 0;
}

// New data coming from the bus. Accept commands
bit gardenSink_read() {
    if (s_readState == RS_START) {
        // Read zone count
        if (prot_control_readAvail() < 2) {
            // Poll again
            return 1;
        }
        prot_control_read(&s_readZoneCount, 2);
        if (s_readZoneCount > SUPPORTED_ZONES) {
            s_readZoneCount = SUPPORTED_ZONES;
        }
        s_readState = RS_READZONES;
    }
    if (s_readState == RS_READZONES) {
        // Read zone count
        if (prot_control_readAvail() < s_readZoneCount) {
            // Poll again
            return 1;
        }
        prot_control_read(s_zoneTimes, s_readZoneCount);
        
        // Only accept new program when in idle mode
        // Can program?
        if (g_state == OFF) {
            // Store program in memory
            memcpy(imm_times, s_zoneTimes, SUPPORTED_ZONES);
            // Start!
            gsink_start = 1;
        }
        s_readState = RS_START;
        return 0;
    }
    // Should never reach this
    return 0;
}

// The server asks for status/configuration
bit gardenSink_write() {
    // 2 header bytes + 1 byte for each zone (remaining time in minutes)
    if (prot_control_writeAvail() < (3 + SUPPORTED_ZONES)) {
        return 1;
    }

    // Send current state (1 byte)
    WORD data = g_state;
    prot_control_write(&data, 1);

    // Send current program (if compatible with the state)
    // Send zone count
    data = SUPPORTED_ZONES;
    prot_control_write(&data, 2);    
    prot_control_write(imm_times, SUPPORTED_ZONES);

    // Done, no more data
    return 0;
}

