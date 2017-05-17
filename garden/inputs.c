#include <pic16f887.h>

#include "hw/ports.h"
#include "inputs.h"

// Scan codes:
// 0: button0 (power)
// 1: button zone1
// 2: button zone2
// 3: button zone3
// 4: button zone4
// 5: external input

// bit0 is button0, to bit5 (ext)
static unsigned char s_currState;
// Pending event to dispatch, as scan code
static bit s_isr;

#pragma warning disable:1090
static unsigned char lastPortB;

static char get_state();

void portb_setup() {
    // Enable digital I/O on PORTB
    ANSELH = 0;
    // Pullups
    OPTION_REGbits.nRBPU = 0;
    WPUB = 0xff;
    // Interrupt on change
    IOCB = BUTTON_IN_MASK;
    
    INTCONbits.RBIE = 1;
    s_currState = 0;
    s_isr = 0;
}

// returns 00E43210
static unsigned char get_state() {
    char ret = 0;
    ret |= !BUTTON_IN_EXT;
    ret <<= 1;
#ifndef __DEBUG
    ret |= !BUTTON_IN_4;
#endif
    ret <<= 1;
    ret |= !BUTTON_IN_3;
    ret <<= 1;
    ret |= !BUTTON_IN_2;
    ret <<= 1;
    ret |= !BUTTON_IN_1;
    ret <<= 1;
    ret |= !BUTTON_IN_0;
    return ret;
}

void portb_isr() {
    // Otherwise RBIF will go in loop!
    lastPortB = PORTB;
    INTCONbits.RBIF = 0;
    s_isr = 1;
}

int portb_event() {
    if (s_isr) {
        s_isr = 0;
        unsigned char state = get_state();

        // Check pressed button
        unsigned char changes = state ^ s_currState;
        // Filter depress events
        changes = changes & state;
        s_currState = state;
        if (changes) { 
            // Detected
            int scanCode = -1;
            do { 
                changes >>= 1;
                scanCode++;
            } while (changes);
            // s_pressedButton is the (highest) index of the pressed button
            return scanCode;
        }
    }
    return PORTB_NO_EVENTS;
}
