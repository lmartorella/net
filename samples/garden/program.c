#include <xc.h>
#include <string.h>
#include <stdlib.h>
#include "program.h"
#include "display.h"
#include "timers.h"
#include "outputs.h"

#include "../../src/nodes/hardware/eeprom.h"

static const long RELAIS_ON_TIME_TICKS = 3 * TICK_PER_SEC;
static const long RELAIS_OFF_TIME_TICKS = 3 * TICK_PER_SEC;
static long s_ticksToWait;

// The non-volatile program (for timer input)
static __eeprom IMM_TIMER PROGR_DATA_TIMERS[SUPPORTED_ZONES];

// The current program ready to start or in use. During activity, each times decreases to zero
IMM_TIMER imm_timers[SUPPORTED_ZONES];

// Temp buffer to save immediate times when the user changes the program through the UI
static IMM_TIMER s_savedTimers[SUPPORTED_ZONES];
static int s_current_zone;
static bit s_modified;
static int currDispVal;

typedef enum { 
    // Entering data
    UI,
    // Pre-on, wait water to stabilize
    RELAIS_WAIT_FOR_ON,
    // On, time counting
    RELAIS_ON,
    // Off, wait water to stabilize
    RELAIS_WAIT_FOR_OFF,
    // Done
    FINISHED,
} IMM_STATE;

// State of immediate program (s_current_zone is the zone working)
static IMM_STATE s_state;
static long s_stateTime;
static void imm_update_disp();

// Clear immediate memory
void imm_init() {
    memset(imm_timers, 0, SUPPORTED_ZONES * sizeof(IMM_TIMER));
    memset(s_savedTimers, 0, SUPPORTED_ZONES * sizeof(IMM_TIMER));
    s_current_zone = -1;
    s_modified = 0;
    s_state = FINISHED;  
}

// Load program to immediate memory
void program_load() {
    memcpy(s_savedTimers, imm_timers, sizeof(IMM_TIMER) * SUPPORTED_ZONES);
    rom_read((BYTE)PROGR_DATA_TIMERS, (void*)imm_timers, sizeof(IMM_TIMER) * SUPPORTED_ZONES);
    s_modified = 0;
}

// Save program from immediate memory
void program_save() {
    rom_write((BYTE)PROGR_DATA_TIMERS, (void*)imm_timers, sizeof(IMM_TIMER) * SUPPORTED_ZONES);
    s_modified = 0;
}

// Change the settings of the relevant zone
void imm_zone_pressed(int zone) {
    s_state = UI;
    currDispVal = -1;
    // Select the zone?
    if (zone != s_current_zone) {
        s_current_zone = zone;
        // If zero, change it
        if (imm_timers[zone].time == 0) {
            imm_timers[zone].time = 1;
            s_modified = 1;
        }
    }
    else {
        s_modified = 1;
        
        // Change the time
        // Step 0 -> 1,2,3,4,5,7,10,13,16,20,25,30
        TIME_MINUTE_T v = imm_timers[zone].time;
        if (v < 5) {
            v++;
        }
        else if (v < 7) {
            v += 2;
        }
        else if (v < 16) {
            v += 3;
        }
        else if (v < 20) {
            v += 4;
        }
        else if (v < 30) {
            v += 5;
        }
        else {
            v = 0;
        }
        imm_timers[zone].time = v;
    }
}

// reset 
void imm_load() {
    memcpy(imm_timers, s_savedTimers, sizeof(IMM_TIMER) * SUPPORTED_ZONES);
}

void imm_restart(int zone) {
    // Find the starting zone
    s_current_zone = zone;
    for (signed char i = SUPPORTED_ZONES - 1; i >= 0; i--) {
        if (imm_timers[i].time) {
            s_current_zone = i;
        }
    }
    // If user clicked to enter in IMM mode, have at least 1 minute if all zero.
    if (imm_timers[s_current_zone].time == 0) { 
        imm_timers[s_current_zone].time = 1;
    }
    
    s_modified = 0;
    output_clear_zones();
    s_state = UI;
    currDispVal = -1;

    imm_update_disp();
}

// Update the display with the immediate memory
static void imm_update_disp() {
    TIME_MINUTE_T val = imm_timers[s_current_zone].time;
    if (currDispVal != val) { 
        if (val > 99) {
            display_data("EE");
        }
        else {
            char buf[3];
            utoa(buf, val, 10);
            display_data(buf);
        }
        currDispVal = val;
    }
    display_dotBlink(s_state == RELAIS_ON);
    
    // Update single leds
    for (char i = 0; i < SUPPORTED_ZONES; i++) {
        if (s_current_zone == i) {
            disp_zone_led(i, LED_BLINK);
        }
        else {
            disp_zone_led(i, imm_timers[i].time ? LED_ON : LED_OFF);
        }
    }
}
// Is the immediate modified?
bit imm_is_modified() {
    return s_modified; 
}

static void go_state(IMM_STATE state) {
    s_state = state;
    s_stateTime = timer_get_time();
}

static void go_next_zone() {
    // Find next programmed
    while (++s_current_zone < SUPPORTED_ZONES) {
        if (imm_timers[s_current_zone].time) {
            // Found.
            go_state(RELAIS_WAIT_FOR_ON);
            return;
        }
    }
    // program finished
    timer_off();
    go_state(FINISHED);
}

// Start the program
void imm_start() {
    // Start from zone 0
    s_current_zone = -1;
    go_next_zone();
}

void imm_stop() {
    s_state = FINISHED;
    s_current_zone = -1;
    for (char i = 0; i < SUPPORTED_ZONES; i++) {
        disp_zone_led(i, LED_OFF);
    }
    display_data("  ");
}       


bit imm_poll() { 
    static long s_startOfMinute;
    
    if (s_state == FINISHED) {
        return 1; 
    }
    
    long now = timer_get_time();
    long elapsedTicks = now - s_stateTime;
    
    imm_update_disp();
    switch (s_state) {
        case RELAIS_WAIT_FOR_ON:
            if (elapsedTicks > RELAIS_ON_TIME_TICKS) { 
                // Switch it on, go next state
                output_set(imm_timers[s_current_zone].zones ? imm_timers[s_current_zone].zones : (1 << s_current_zone));
                s_ticksToWait = imm_timers[s_current_zone].time * SECONDS_PER_MINUTE * TICK_PER_SEC;
                go_state(RELAIS_ON);
                s_startOfMinute = now;
            }
            break;
        case RELAIS_ON:
            if (elapsedTicks > s_ticksToWait) { 
                // Ok, time gone
                // Switch off
                output_clear_zones();
                // Switch off the led
                imm_timers[s_current_zone].time = 0;
                go_state(RELAIS_WAIT_FOR_OFF);
            } else {
                // Count minutes
                elapsedTicks = now - s_startOfMinute;
                if (elapsedTicks > (TICK_PER_SEC * SECONDS_PER_MINUTE)) {
                    s_startOfMinute += (TICK_PER_SEC * SECONDS_PER_MINUTE);
                    imm_timers[s_current_zone].time--;
                }
            }
            break;
        case RELAIS_WAIT_FOR_OFF:
            if (elapsedTicks > RELAIS_OFF_TIME_TICKS) { 
                // Switch it off, go next zone
                go_next_zone();
            }
            break;
    }
    return s_state != FINISHED;
}