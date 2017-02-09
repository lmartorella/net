#ifdef HAS_DCF77 
#include "pch.h"

static bit s_lastState;
static TICK_TYPE s_lastTick;
static TICK_TYPE s_lastValidBit;

#define DCF77_IN_PORT PORTAbits.RA1
#define LED PORTAbits.RA0

// 8 bytes = 64 bits
#define BUFFER_MASK_SIZE 8
static BYTE s_message[BUFFER_MASK_SIZE];
static char s_pos;

// From DA6180B datasheet
#define ZERO_MIN (TICK_TYPE)(TICKS_PER_SECOND * 0.04)
#define ZERO_MAX (TICK_TYPE)(TICKS_PER_SECOND * 0.13)
#define ONE_MIN (TICK_TYPE)(TICKS_PER_SECOND * 0.14)
#define ONE_MAX (TICK_TYPE)(TICKS_PER_SECOND * 0.25)

#define SPACE_MIN (TICK_TYPE)(TICKS_PER_SECOND * 0.8)
#define SPACE_MAX (TICK_TYPE)(TICKS_PER_SECOND * 1.2)
#define SPACE2_MIN (TICK_TYPE)(TICKS_PER_SECOND * 1.8)
#define SPACE2_MAX (TICK_TYPE)(TICKS_PER_SECOND * 2.2)

#define FLASH_TIME (TICK_TYPE)(TICKS_PER_SECOND * 0.15)

static enum {
    BIT_TYPE_INVALID = '-',
    BIT_TYPE_ZERO = '0',
    BIT_TYPE_ONE = '1'
} s_lastMark;

static void reset() {
    s_pos = 0;
    //memset(s_message, 0, 8);
}

static void addBit() {
    if (s_lastMark == BIT_TYPE_ONE) { 
        s_message[s_pos / 8] |= (1 << (s_pos % 8));
    }
    else {
        s_message[s_pos / 8] &= ~(1 << (s_pos % 8));
    }
    s_pos++;
    s_pos = s_pos % 64;
}

void dcf77_init() {
    s_lastState = 0;    // At reset the receiver line is zero
    s_lastMark = BIT_TYPE_INVALID;
    s_lastValidBit = TickGet();
    reset();
}

void dcf77_poll() {
    BOOL s = DCF77_IN_PORT;
    TICK_TYPE now = TickGet();
    TICK_TYPE len;
    
    if (s && !s_lastState) {
        s_lastState = 1;
        s_lastTick = TickGet();
    }    
    else if (!s && s_lastState) {
        s_lastState = 0;
        len = now - s_lastTick;
        s_lastMark = BIT_TYPE_INVALID;
        if (len < ZERO_MIN) {
            // Invalid
        }
        else if (len < ZERO_MAX) {
            s_lastMark = BIT_TYPE_ZERO;
        }
        else if (len < ONE_MIN) {
            // Invalid
        }
        else if (len < ONE_MAX) {
            s_lastMark = BIT_TYPE_ONE;
        }
       
        if (s_lastMark != BIT_TYPE_INVALID) {
            // Get time from last bit
            len = now - s_lastValidBit;
            // 1 second? Accumulate bit into the string
            if (len > SPACE2_MIN && len < SPACE2_MAX) {
                // First bit after space
                //printHex();
                reset();
            }
            else if (len < SPACE_MIN || len > SPACE_MAX) {
                reset();
            } // else 1 second? Accumulate bit into the string

            addBit();
            LED = 1;
            s_lastValidBit = now;
        }
        
        //printStat();
    }    
    
    if (LED && now - s_lastValidBit > FLASH_TIME) {
        LED = 0;
    }
}

#endif