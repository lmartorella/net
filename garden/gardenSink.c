#include "program.h"
#include "state.h"
#include "../fw/pch.h"
#include "../fw/protocol.h"

static char s_times[SUPPORTED_ZONES];

// New data coming from the bus. Accept commands
bit gardenSink_read() {
    prot_control_readAvail();
}

// The server asks for status/configuration
bit gardenSink_write() {
    if (prot_control_writeAvail() < (2 + SUPPORTED_ZONES)) {
        return 1;
    }

    // Send number of zones supported
    BYTE data = SUPPORTED_ZONES;
    prot_control_write(&data, 1);
    // Send current state (UI_STATE)
    prot_control_write(&s_state, 1);
    // Send current running program state (times)
    return 0;
}
