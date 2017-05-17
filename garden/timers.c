#include <pic16f887.h>

#include "timers.h"

// Without prescaler, counter will be reset to 625 to have 50Hz
#define TMR1_VAL (0x10000u - (31250l / TICK_PER_SEC))

static long ticks = 0;
static bit ticksDirty;
static bit on;

void timer_setup() {
    on = 0;
    // Set 1Mhz as int clock
    OSCCONbits.IRCF = 4;
    timer_on();
}

void timer_on() {
    if (on) return;
    // USe TIMER1 (but NOT avail in SLEEP since LP uses RC0 and RC1)
    T1CONbits.TMR1CS = 0;   // USe fosc
    T1CONbits.T1CKPS = 3;   // 1:8 prescaler -> 31250Hz
    T1CONbits.TMR1ON = 1;
    
    PIE1bits.TMR1IE = 1;
    TMR1L = (TMR1_VAL & 0xFF);
    TMR1H = (TMR1_VAL >> 8);   
    on = 1;
}

void timer_off() {
    if (!on) return;
    T1CONbits.TMR1ON = 0;
    on = 0;
}

void timer_isr() {
    PIR1bits.TMR1IF = 0;
    TMR1L = (TMR1_VAL & 0xFF);
    TMR1H = (TMR1_VAL >> 8);   
    ticks++;
    ticksDirty = 1;
}

long timer_get_time() {
    return ticks;
}

char timer_check_dirty() {
    char ret = ticksDirty;
    ticksDirty = 0;
    return ret;
}
