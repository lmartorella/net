#ifndef INPUTS_H
#define	INPUTS_H

#ifdef __DEBUG
#define DEBUG_PINS
#endif

void portb_setup();

// Wait for a PORTB event.
// Range 0-5 are keys, and it can be 16-bit negative (long press)
// (5 is the external button, that doesn't support long press)
// If 0x80 no events
int portb_event();

// Process interrupt
void portb_isr();

const int PORTB_EXT_TRIGGER = 5;
const int PORTB_NO_EVENTS = 0x80;

#endif	/* INPUTS_H */

