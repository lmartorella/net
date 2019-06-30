#include "program.h"
#include "state.h"
#include "../../src/nodes/pch.h"
#include "../../src/nodes/protocol.h"

static enum {
    RS_START,
    RS_READZONES
} s_readState;
static WORD s_readZoneCount;
static IMM_TIMER s_zoneTimers[SUPPORTED_ZONES];

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
        memset(s_zoneTimers, 0, SUPPORTED_ZONES * sizeof(IMM_TIMER));
        // Read zone count
        if (prot_control_readAvail() < s_readZoneCount * sizeof(IMM_TIMER)) {
            // Poll again
            return 1;
        }
        prot_control_read(s_zoneTimers, s_readZoneCount * sizeof(IMM_TIMER));
        
        // Only accept new program when in idle mode
        // Can program?
        if (g_state == OFF) {
            // Store program in memory
            memcpy(imm_timers, s_zoneTimers, SUPPORTED_ZONES * sizeof(IMM_TIMER));
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
    // 1 status byte + 2 header bytes + IMM_TIMER for each zone (remaining time in minutes + zones)
    if (prot_control_writeAvail() < (3 + SUPPORTED_ZONES * sizeof(IMM_TIMER))) {
        return 1;
    }

    // Send current state (1 byte)
    WORD data = g_state;
    prot_control_write(&data, 1);

    // Send current program (if compatible with the state)
    // Send zone count
    data = SUPPORTED_ZONES;
    prot_control_write(&data, 2);    
    prot_control_write(imm_timers, SUPPORTED_ZONES * sizeof(IMM_TIMER));

    // Done, no more data
    return 0;
}

