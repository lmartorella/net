#ifndef PROGRAM_H
#define	PROGRAM_H

// Load program to immediate memory
void program_load();
// Save program from immediate memory
void program_save();

// Change the settings of the relevant zone
void imm_zone_pressed(int zone);

void imm_init();
void imm_load();

// Reset immediate memory to saved 
void imm_restart(int zone);
// Is the immediate modified?
bit imm_is_modified();

// Start the program
void imm_start();
void imm_stop();
// Poll
bit imm_poll();

#endif	/* PROGRAM_H */

