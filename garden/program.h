#ifndef PROGRAM_H
#define	PROGRAM_H

#ifdef __DEBUG
#define DEBUG_PINS
#endif

#ifndef DEBUG_PINS
#define SUPPORTED_ZONES 4
static const int SECONDS_PER_MINUTE = 60;
#else
#define SUPPORTED_ZONES 3
static const int SECONDS_PER_MINUTE = 3;
#endif

extern char imm_times[SUPPORTED_ZONES];

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

extern bit gsink_start;
void gsink_init();


#endif	/* PROGRAM_H */

