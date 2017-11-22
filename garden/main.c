#include <pic.h>

#include "timers.h"
#include "display.h"
#include "inputs.h"
#include "outputs.h"
#include "program.h"
#include "state.h"

#include "../fw/pch.h"
#include "../fw/hardware/tick.h"
#include "../fw/rs485.h"
#include "../fw/appio.h"
#include "../fw/persistence.h"
#include "../fw/protocol.h"

// CONFIG1
#pragma config FOSC = INTRC_NOCLKOUT// Oscillator Selection bits (INTOSCIO oscillator: I/O function on RA6/OSC2/CLKOUT pin, I/O function on RA7/OSC1/CLKIN)
#pragma config WDTE = ON        // Watchdog Timer Enable bit (WDT enabled)
#pragma config PWRTE = OFF      // Power-up Timer Enable bit (PWRT disabled)
#pragma config MCLRE = ON       // RE3/MCLR pin function select bit (RE3/MCLR pin function is MCLR)
#pragma config CP = OFF         // Code Protection bit (Program memory code protection is disabled)
#pragma config CPD = OFF        // Data Code Protection bit (Data memory code protection is disabled)
#pragma config BOREN = ON       // Brown Out Reset Selection bits (BOR enabled)
#pragma config IESO = ON        // Internal External Switchover bit (Internal/External Switchover mode is enabled)
#pragma config FCMEN = ON       // Fail-Safe Clock Monitor Enabled bit (Fail-Safe Clock Monitor is enabled)
#pragma config LVP = OFF        // Low Voltage Programming Enable bit (RB3 pin has digital I/O, HV on MCLR must be used for programming)

// CONFIG2
#pragma config BOR4V = BOR21V   // Brown-out Reset Selection bit (Brown-out Reset set to 2.1V), since now Raspberry introduce a lot of noise on the PSU
#pragma config WRT = OFF        // Flash Program Memory Self Write Enable bits (Write protection off)

UI_STATE g_state;

static const char IMMEDIATE_SYMBOL = 'I';
static const char LEVEL_SYMBOL = 'L';
static const char PROGRAM_SYMBOL = 'P';
static long s_idleTime;

#define IMM_TIME_TICK (2l * TICK_PER_SEC)
#ifndef __DEBUG
#define AUTO_OFF_TICK (30l * TICK_PER_SEC)
#else
#define AUTO_OFF_TICK (3l * TICK_PER_SEC)
#endif

static void interrupt low_isr() {
    if (INTCONbits.RBIF) {
        portb_isr();
    }
    if (PIR1bits.TMR1IF) {
        timer_isr();
    }

    // Update tick timers at ~Khz freq
    TickUpdate();
#ifdef HAS_RS485
    rs485_interrupt();
#endif
}

static void go_immediate(int zone) {
    g_state = PROGRAM_IMMEDIATE;
    imm_load();
    imm_restart(zone);
    display_mode(IMMEDIATE_SYMBOL);
}

// Immediately switch to off state
static void go_off() {
    g_state = OFF;
    display_off();
    imm_init();
    output_clear();
}

// Immediately switch to programming state
static void go_program() {
    g_state = PROGRAM_TIMER;
    
    display_mode(PROGRAM_SYMBOL);
    // Load data from memory
    program_load();
    imm_restart(0);
}

int main() {
    // Analyze RESET reason
    sys_storeResetReason();

    // Init Ticks on timer0 (low prio) module
    timers_init();
    appio_init();

    pers_init();

#ifdef HAS_RS485
    rs485_init();
#endif

#ifdef HAS_BUS
    prot_init();
#endif

    timer_setup();
    display_setup();
    portb_setup();
    imm_init();
    output_setup();
    gsink_init();
    
    g_state = OFF;
    
    enableInterrupts();
   
    while (1) {
        CLRWDT();
        
#if defined(HAS_BUS_CLIENT) || defined(HAS_BUS_SERVER)
        bus_poll();
#endif
#ifdef HAS_BUS
        prot_poll();
#endif
#ifdef HAS_RS485
        rs485_poll();
#endif
        pers_poll();

        // Low-prio task?
        if (rs485_state == RS485_LINE_RX && bus_isIdle()) {
            // Avoid heavy calc when in real-time mode (e.g. implementing bus times)
            // Poll animations
            display_poll();
            // Poll timers to make program go on
            if (!imm_poll() && g_state == IN_USE) {
                // Program finished! Go off.
                go_off();
            }
        
            // Back to immediate?
            long elapsed = timer_get_time() - s_idleTime;
            if (g_state == WAIT_FOR_IMMEDIATE && elapsed > IMM_TIME_TICK) {
                go_immediate(0);
            }
            else if (g_state != OFF && g_state != IN_USE && elapsed > AUTO_OFF_TICK) {
                // Auto-off
                go_off();
            }

            // Wait for a PORTB event.
            // Range 0-31 are keys, and it can be negative (long press)
            // If 0x7f it is triggered the external timer port
            int scanCode = portb_event();
            if (scanCode == PORTB_NO_EVENTS) {
                // Still wait
                continue;
            }

            if (scanCode == PORTB_EXT_TRIGGER) { 
                // Manage the external timer.
                // If the UI is already in IN_USE mode, ignore it.
                // Otherwise load the program and switch to it.
                if (g_state != IN_USE) {
                    // Load program from memory
                    program_load();
                    imm_restart(0);
                    goto in_use;
                }
                // else ignore, spurious command
            }
            else {
                s_idleTime = timer_get_time();
                // Check the current mode for the meaning of key
                if (scanCode < 0) {
                    // Get the long press code
                    scanCode = ~scanCode;
                    // Long press a key
                    if (scanCode == 0) {
                        // If long-pressing the off key, stop all and go in off state
                        go_off();
                    }
                }
                else {
                    // Normal key press
                    if (scanCode == 0) {
                        // Power/mode key pressed. Cycle mode, if possible
                        // Master button pressed: cycle if possible
                        switch (g_state) {
                            case OFF:
                                // Go in immediate mode
                                go_immediate(0);
                                break;
                            case PROGRAM_IMMEDIATE:
                                // Accept the immediate program?
                                if (imm_is_modified()) {
                                    // Accept it
                                    goto in_use;
                                }
                                else {
                                    imm_stop();
                                    // Immediately switch to level state
                                    g_state = LEVEL_CHECK;
                                    display_mode(LEVEL_SYMBOL);
                                    // Not implemented
                                    display_data("--");
                                }
                                break;
                            case LEVEL_CHECK:
                                // Go in program mode
                                go_program();
                                break;
                            case PROGRAM_TIMER:
                                // Accept the program?
                                if (imm_is_modified()) {
                                    // Accept it
                                    program_save();
                                    display_mode(' ');
                                    display_data("OK");
                                    g_state = WAIT_FOR_IMMEDIATE;
                                }
                                else {
                                    // Back to immediate
                                    go_immediate(0);
                                }
                                break;
                            case IN_USE:
                                // Panic button!!
                                go_off();
                                break;
                        }
                    }
                    else { 
                        // Individual zone button pressed.
                        // Zone button pressed: add it to the current program if possible
                        switch (g_state) {
                            case OFF: 
                                // Switch it on
                                go_immediate(scanCode - 1);
                                break;
                            case PROGRAM_IMMEDIATE:
                            case PROGRAM_TIMER:
                                // Only accepted during programming (immediate or program)
                                imm_zone_pressed(scanCode - 1);
                                break;
                        }
                    }
                }
            }
        }

        // Received program from the bus?
        if (gsink_start) {
            gsink_start = 0;
            goto in_use;
        }

        continue;
in_use:    
        // Immediately switch to in use, using the current loaded program
        g_state = IN_USE;
        display_mode_anim();
        imm_start();
    }
}

