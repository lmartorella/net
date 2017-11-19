#ifndef STATES_H
#define	STATES_H

typedef enum { 
    // Off, display off
    OFF,
    // Immediate program mode
    PROGRAM_IMMEDIATE,
    // Display water level (future usage))
    LEVEL_CHECK,
    // Program the timer mode
    PROGRAM_TIMER,
    // Looping a program (manual or automatic)
    IN_USE,
    // Timer for OK string
    WAIT_FOR_IMMEDIATE            
} UI_STATE;

extern UI_STATE g_state;

#endif	/* STATES_H */

