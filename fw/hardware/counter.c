#include "../pch.h"
#include "counter.h"
#include "../persistence.h"

#ifdef HAS_DIGITAL_COUNTER

static DCNT_DATA s_data;
static bit s_counterDirty;
static DWORD s_lastCounter;

static BYTE s_persTimer;
// Only save once every 30 minutes, a good balance between EEPROM data endurance and potential data loss due to reset
// 10 seconds in debug
#define PERS_TIMER_SECS (10 /*60 * 30*/)

void dcnt_interrupt() {
    if (DCNT_IF) {
        // Interrupt stack
        // This is access atomically since in interrupt
        s_data.counter++;
        s_counterDirty = 1;
        
        DCNT_IF = 0;
    }
    CLRWDT();
}

void dcnt_init() {
    s_lastCounter = s_data.counter = pers_data.dcnt_counter;
    s_data.flow = 0;
    s_counterDirty = 0;
    s_persTimer = 0;
    
    // Init interrupt on edge (RB0)
    DCNT_IF = 0;
    DCNT_IE = 1;
    // Edge not relevant
    
    CLRWDT();
}

// Called every seconds.
// Check when the volatile counter should be stored back in EEPROM
// to minimize write operations
void dcnt_poll() {
    if (s_counterDirty) {
        
        DCNT_IE = 0;
        DWORD currCounter = s_data.counter;
        DCNT_IE = 1;
        
        s_data.flow = currCounter - s_lastCounter;
        s_lastCounter = currCounter;
        
        if ((s_persTimer++) >= PERS_TIMER_SECS) {
            s_persTimer = 0;
            s_counterDirty = 0;
            
            pers_data.dcnt_counter = s_lastCounter;
            pers_save();
        }
    } else {
        s_data.flow = 0;
    }
    CLRWDT();
}

void dcnt_getDataCopy(DCNT_DATA* data) {
    DCNT_IE = 0;
    *data = s_data;
    DCNT_IE = 1;
}


#endif
