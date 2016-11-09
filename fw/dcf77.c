#include "pch.h"
#include "appio.h"

#ifdef HAS_DCF77

static bit s_lastState;
static int s_pulseCount;
static TICK_TYPE s_lastTick;

#define TICKS_PER_PULSE (TICKS_PER_SECOND / 10)
#define ZERO_MIN (TICK_TYPE)(TICKS_PER_PULSE * 0.9)
#define ZERO_MAX (TICK_TYPE)(TICKS_PER_PULSE * 1.1)
#define ONE_MIN (TICK_TYPE)(TICKS_PER_PULSE * 2 * 0.9)
#define ONE_MAX (TICK_TYPE)(TICKS_PER_PULSE * 2 * 1.1)

static enum {
    BIT_TYPE_INVALID,
    BIT_TYPE_ZERO
    BIT_TYPE_ONE
} s_lastMark;

static void print() {
    // Print it
    char buf[16];
    sprintf(buf, "%c DCF: %d", s_lastMark == BIT_TYPE_INVALID ? '-' : (s_lastMark == BIT_TYPE_ZERO ? '0' : '1'), s_pulseCount);
    println(buf);
}

void dcf77_init() {
    DCF77_IN_TRIS = 1; // input
    DCF77_EN_TRIS = 0; // out
    
    DCF77_EN_PORT = 1; // disable
    // Wait at least 50ms to charge caps (see da6180B datasheet)
    wait1s();
    // Fast startup
    DCF77_EN_PORT = 0; 
    
    s_lastState = 0;    // At reset the receiver line is zero
    s_pulseCount = 0;
    s_lastMark = BIT_TYPE_INVALID;

    print();
}

void dcf77_poll() {
    BOOL s = DCF77_IN_PORT;
    if (s && !s_lastState) {
        s_lastState = 1;
        s_lastTick = TickGet();
    }    
    else if (!s && s_lastState) {
        s_lastState = 0;
        DWORD len = TickGet() - s_lastTick;
        s_lastMark = BIT_TYPE_INVALID;
        if (len < ZERO_MIN) {
            // Invalid
        }
        else if (len < ZERO_MAX) {
            type = BIT_TYPE_ZERO;
        }
        else if (len < ONE_MIN) {
            // Invalid
        }
        else if (len < ONE_MAX) {
            type = BIT_TYPE_ONE;
        }

        // Count
        s_pulseCount++;
        
        print();
    }    
}

#endif
