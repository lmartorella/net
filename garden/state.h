#ifndef STATES_H
#define	STATES_H

typedef enum { 
    // Off, display off
    OFF = 0,
    // Immediate program mode
    PROGRAM_IMMEDIATE,
    // Display water level (future usage))
    LEVEL_CHECK,
    // Program the timer mode
    PROGRAM_TIMER,
    // Looping a program (manual or automatic)
    IN_USE,
    // Timer used after new programming, while the display shows OK, to go back to imm state (2 seconds)
    WAIT_FOR_IMMEDIATE            
} UI_STATE;

extern UI_STATE g_state;

#endif	/* STATES_H */

