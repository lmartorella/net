#include <xc.h>
#include <string.h>
#include <stdlib.h>
#include "program.h"
#include "display.h"
#include "timers.h"
#include "outputs.h"

#include "../fw/hardware/eeprom.h"

static const long RELAIS_ON_TIME_TICKS = 3 * TICK_PER_SEC;
static const long RELAIS_OFF_TIME_TICKS = 3 * TICK_PER_SEC;
static long s_ticksToWait;

static __eeprom char PROGR_DATA[SUPPORTED_ZONES];

static char s_times[SUPPORTED_ZONES];
static char s_savedTimes[SUPPORTED_ZONES];
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
    memset(s_times, 0, SUPPORTED_ZONES * sizeof(char));
    memset(s_savedTimes, 0, SUPPORTED_ZONES * sizeof(char));
    s_current_zone = -1;
    s_modified = 0;
    s_state = FINISHED;  
}

// Load program to immediate memory
void program_load() {
    memcpy(s_savedTimes, s_times, sizeof(char) * SUPPORTED_ZONES);
    // TODO: read EEPROM
    rom_read((BYTE)PROGR_DATA, s_times, sizeof(char) * SUPPORTED_ZONES);
    s_modified = 0;
}

// Save program from immediate memory
void program_save() {
    // TODO: write EEPROM
    rom_write_imm((BYTE)PROGR_DATA, s_times, sizeof(char) * SUPPORTED_ZONES);
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
        if (s_times[zone] == 0) {
            s_times[zone] = 1;
            s_modified = 1;
        }
    }
    else {
        s_modified = 1;
        
        // Change the time
        // Step 0 -> 1,2,3,4,5,7,10,13,16,20,25,30
        int v = s_times[zone];
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
        s_times[zone] = v;
    }
}

// reset 
void imm_load() { 
    memcpy(s_times, s_savedTimes, sizeof(char) * SUPPORTED_ZONES);
}

void imm_restart(int zone) {
    s_current_zone = zone;
    for (signed char i = SUPPORTED_ZONES - 1; i >= 0; i--) {
        if (s_times[i]) {
            s_current_zone = i;
        }
    }
    if (s_times[s_current_zone] == 0) { 
        s_times[s_current_zone] = 1;
    }
    
    s_modified = 0;
    output_clear();
    s_state = UI;
    currDispVal = -1;

    imm_update_disp();
}

// Update the display with the immediate memory
static void imm_update_disp() {
    int val = s_times[s_current_zone];
    if (s_state == RELAIS_ON) {
        // Use elapsing time
        int elapsedMinutes = ((timer_get_time() - s_stateTime) / (TICK_PER_SEC * SECONDS_PER_MINUTE));
        val -= elapsedMinutes;
    }
    if (currDispVal != val) { 
        if (val < 0 || val > 99) {
            display_data("EE");
        }
        else {
            char buf[3];
            itoa(buf, val, 10);
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
            disp_zone_led(i, s_times[i] != 0 ? LED_ON : LED_OFF);
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
        if (s_times[s_current_zone] > 0) {
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
    if (s_state == FINISHED) {
        return 1; 
    }
    
    imm_update_disp();
    long elapsedTicks = timer_get_time() - s_stateTime;
    switch (s_state) {
        case RELAIS_WAIT_FOR_ON:
            if (elapsedTicks > RELAIS_ON_TIME_TICKS) { 
                // Switch it on, go next state
                output_set(s_current_zone, 1);
                s_ticksToWait = s_times[s_current_zone] * SECONDS_PER_MINUTE * TICK_PER_SEC;
                go_state(RELAIS_ON);
            }
            break;
        case RELAIS_ON:
            if (elapsedTicks > s_ticksToWait) { 
                // Ok, time gone
                // Switch off
                output_set(s_current_zone, 0);
                // Switch off the led
                s_times[s_current_zone] = 0;
                go_state(RELAIS_WAIT_FOR_OFF);
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