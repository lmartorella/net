#include "../pch.h"
#include "counter.h"
#include "../persistence.h"

#ifdef HAS_DIGITAL_COUNTER

// Copied from persistence to better control atomicity of accesses
static DWORD s_counter;
static bit s_counterDirty;
static TICK_TYPE s_slowTimer;
// Only save once every 30 minutes, a good balance between EEPROM data endurance and potential data loss due to reset
#define SLOW_PERS_TIMER (TICKS_PER_SECOND * 60 * 30)

void dcnt_interrupt() {
    // Interrupt stack
    // This is access atomically since in interrupt
    s_counter++;
    s_counterDirty = 1;
    CLRWDT();
}

void dcnt_init() {
    s_counter = pers_data.dcnt_counter;
    s_counterDirty = 0;
    s_slowTimer = TickGet();
    
    // Init interrupt on edge (RB0)
    
    CLRWDT();
}

// Called when the volatile counter should be stored back in EEPROM
// to minimize write operations
void dcnt_poll() {
    TICK_TYPE now = TickGet();
    if (now - s_slowTimer >= SLOW_PERS_TIMER) {
        s_slowTimer = now;
        if (s_counterDirty) {
            pers_data.dcnt_counter = s_counter;
            pers_save();
            s_counterDirty = 0;
        }
    }
    CLRWDT();
}

#endif
