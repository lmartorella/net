#include "../../../src/nodes/pch.h"
#include "../../../src/nodes/protocol.h"
#include "digio.h"

#if defined(HAS_DIGIO_IN) || defined(HAS_DIGIO_OUT)

// Debounce at 0.1s
#define DEBOUNCE_TIMEOUT (TICKS_PER_SECOND / 10)

#ifdef HAS_DIGIO_IN
static BYTE s_lastInState;
static TICK_TYPE s_debounceTimer;

// Circular buffer
static BYTE s_evtBegin;
static BYTE s_evtEnd;
typedef struct { TICK_TYPE tick; BYTE state; } EVENT_TABLE_t;
static EVENT_TABLE_t s_events[DIGIO_EVENT_BUFFER_SIZE];

// Protocol write state. Any number from 1 to DIGIO_EVENT_BUFFER_SIZE means that 
// N events are yet to be sent (from the older).
static enum { IN_STATE_HEADER = 0x00 } s_in_writeState;
#endif

// Only support 1 bit for IN and 1 for OUT (can even be the same)

void digio_init()
{
#ifdef HAS_DIGIO_IN
    // First enable input bits
    INIT_DIGIO_IN_PORT();
    s_in_writeState = IN_STATE_HEADER;
    s_evtBegin = s_evtEnd = 0;
    s_lastInState = DIGIO_PORT_IN_BIT;
    s_debounceTimer = timers_get();
#endif
    
#ifdef HAS_DIGIO_OUT
    // Then the output. So if the same port is configured as I/O it will work
    DIGIO_TRIS_OUT_BIT = 0;
#endif
}

#endif

#ifdef HAS_DIGIO_OUT

bit digio_out_write()
{
    // One port
    WORD b = 1;
    // Number of switch = 1
    prot_control_write(&b, sizeof(WORD));
    return FALSE;
}

// Read bits to set as output
bit digio_out_read()
{
    if (prot_control_readAvail() < 2) {
        // Need more data
        return TRUE;
    }
    BYTE arr;
    // Number of bytes sent (expect 1)
    prot_control_read(&arr, 1);
    // The byte: the bit 0 is data
    prot_control_read(&arr, 1);
    DIGIO_PORT_OUT_BIT = !!arr;
    return FALSE;
}

#endif

#ifdef HAS_DIGIO_IN

void digio_in_poll() {
    TICK_TYPE now = timers_get();
    if (now - s_debounceTimer >= DEBOUNCE_TIMEOUT)
    {
        s_debounceTimer = now;
        BYTE state = DIGIO_PORT_IN_BIT;
        if (state != s_lastInState) {
            s_lastInState = state;
            // Allocate a new event in the table
            s_events[s_evtEnd].state = state;
            s_events[s_evtEnd].tick = timers_get();
            s_evtEnd = (s_evtEnd + 1) % DIGIO_EVENT_BUFFER_SIZE; 
            if (s_evtEnd == s_evtBegin) {
                // Overflow
                fatal("EVTOV");
            }
        }
    }
}

// Write bits read as input
bit digio_in_write()
{   
    if (s_in_writeState == IN_STATE_HEADER) {
        if (prot_control_writeAvail() < sizeof(TICK_TYPE) * 2 + 3) {
            // Need more space
            return 1;
        }

        // Timer tick size (low nibble) + bit count (high nibble)
        BYTE sizes = (1 << 4) + sizeof(TICK_TYPE);
        prot_control_write(&sizes, 1);

        // The byte: last bit state
        prot_control_write(&s_lastInState, 1);

        // Follow tick/seconds, with the same size of the tick
        TICK_TYPE ticks = TICKS_PER_SECOND;
        prot_control_write(&ticks, sizeof(TICK_TYPE));    

        // Follow current tick
        ticks = timers_get();
        prot_control_write(&ticks, sizeof(TICK_TYPE));

        // Now calc how many events should be sent. s_in_writeState is the 
        // event count
        s_in_writeState = (s_evtEnd - s_evtBegin) % DIGIO_EVENT_BUFFER_SIZE;
        prot_control_write(&s_in_writeState, 1);

        // Finished?
        return (s_in_writeState > 0);  
    } else {
        // Write one event at a time, to allow small buffer to send out big tables.
        if (prot_control_writeAvail() < sizeof(EVENT_TABLE_t)) {
            // Wait space
            return 1;
        }
        
        // Write next event, and advance the read counter
        prot_control_write(&s_events[s_evtBegin], sizeof(EVENT_TABLE_t));
        s_evtBegin = (s_evtBegin + 1) % DIGIO_EVENT_BUFFER_SIZE;

        // Finished?
        s_in_writeState--;
        return (s_in_writeState > 0);
    }
}

#endif
