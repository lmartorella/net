#ifndef STATES_H
#define	STATES_H

typedef enum { 
    // Off, display off
    OFF = 0,
    // Immediate program mode
    PROGRAM_IMMEDIATE,
    // Display flow level
    FLOW_CHECK,
    // Looping a program (manual or automatic)
    IN_USE,
    // Timer used after new programming, while the display shows OK, to go back to imm state (2 seconds)
    WAIT_FOR_IMMEDIATE            
} UI_STATE;

extern UI_STATE g_state;
extern __bit g_flowDirty;
extern int g_flow;

#endif	/* STATES_H */

