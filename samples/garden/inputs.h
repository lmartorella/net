#ifndef INPUTS_H
#define	INPUTS_H

#ifdef __DEBUG
#define DEBUG_PINS
#endif

void portb_setup();

// Wait for a PORTB event.
// Range 0-4 are keys, and it can be 16-bit negative (long press)
// If 0x80 no events
int portb_event();

// Process interrupt
void portb_isr();

const int PORTB_NO_EVENTS = 0x80;

#endif	/* INPUTS_H */

