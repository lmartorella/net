#ifndef TIMERS_H
#define	TIMERS_H

// 100Hz timer
const long TICK_PER_SEC = 100l;

// Setup all timers
void timer_setup();

// Enable/disable the interrupts for real-time timer
void timer_on();
void timer_off();
void timer_isr();

// Get current time in ticks
long timer_get_time();
char timer_check_dirty();

#endif	/* TIMERS_H */

