#include <xc.h>
#include "display.h"
#include "timers.h"
#include "hw/ports.h"

static char digits[3] = { ' ', ' ', ' ' };
static LED_MODE ledModes[4];
static char currDig;
static unsigned char numbers[] = { 
    0x3F,0x06,0x5B,0x4F,0x66,0x6D,0x7D,0x07,0x7F,0x6F
};
static unsigned char letters[] = {
	0x77,0x7C,0x39,0x5E,0x79,0x71,0x6F,0x74,0x10,0x0E,0x70,0x38,0x37,0x54,0x5C,0x73,0x67,0x50,0x6D,0x78,0x1C,0x3E,0x7E,0x64,0x6E,0x5B
};
static unsigned char animation[] = {
    0x20,0x10,0x08,0x04,0x02,0x01
};
static unsigned char dash = 0x40;

static const unsigned char ANIMATION_START = 0x80;
static const unsigned char ANIMATION_LENGTH = 6;
static const int BLINK_TICKS = TICK_PER_SEC / 2;
static const int ANIMATION_TICKS = TICK_PER_SEC / 15;

static int blinkTimer;
static bit blink;
static bit dotBlink;
static bit s_state;

static void set_zone_led(char led, char lit);

void display_setup() {
    // Enable digital I/O on PORTB
    ANSELH = 0;
    // Enable digital I/O on PORTA
    ANSEL = 0;
    // Ensure RA4 as dig I/O
    OPTION_REGbits.T0CS = 0;
    CM1CON0bits.C1ON = CM1CON0bits.C1OE = 0;

    // Buttons
    TRISDbits.TRISD2 = 0;
    TRISDbits.TRISD3 = 0;
    TRISEbits.TRISE1 = 0;
    TRISBbits.TRISB5 = 0;
    TRISBbits.TRISB7 = 0;

    // 7digit
    TRISAbits.TRISA0 = 0;
    TRISAbits.TRISA1 = 0;
    TRISAbits.TRISA2 = 0;
    TRISAbits.TRISA4 = 0;
    TRISCbits.TRISC0 = 0;
    TRISCbits.TRISC1 = 0;
    TRISCbits.TRISC2 = 0;
    TRISEbits.TRISE2 = 0;

    // drives
    TRISCbits.TRISC3 = 0;
    TRISDbits.TRISD0 = 0;
    TRISDbits.TRISD6 = 0;
    
    display_off();
}

void display_off() {
    BUTTON_LED_0 = 0;
    BUTTON_LED_1 = 0;
    BUTTON_LED_2 = 0;
    BUTTON_LED_3 = 0;
#ifndef __DEBUG
    BUTTON_LED_4 = 0;
#endif
    
    DIGIT_DRIVE_0 = 0;
    DIGIT_DRIVE_1 = 0;
    DIGIT_DRIVE_2 = 0;
    
    timer_off();
    s_state = 0;
}

static void display_on() {
    s_state = 1;
    timer_on();
    BUTTON_LED_0 = 1;
}

// Common bank
static unsigned char map @0x7d;
static void next_digit() {
    currDig = (currDig + 1) % 3;
    char dig = digits[currDig];

    if (dig >= '0' && dig <= '9') { 
        map = numbers[dig - '0'];
    }
    else if (dig >= 'A' && dig <= 'Z') { 
        map = letters[dig - 'A'];
    }
    else if (dig == '-') { 
        map = dash;
    }
    else if (dig >= ANIMATION_START) { 
        map = animation[dig - ANIMATION_START];
    }
    else {
        map = 0;
    }
    
    INTCONbits.GIE = 0;

    DIGIT_DRIVE_2 = 0;
    DIGIT_DRIVE_1 = 0;
    DIGIT_DRIVE_0 = 0;

#asm
        bcf	3,5	;RP0=0, select bank0
        bcf	3,6	;RP1=0, select bank0
  
          //PORTCbits.RC1 = (map & 0x1) ? 1 : 0;
        bcf	7,1	
        btfsc	125,0
        bsf	7,1	

          //PORTAbits.RA0 = (map & 0x2) ? 1 : 0;
       	bcf	5,0	
        btfsc	125,1
       	bsf	5,0	

           // PORTAbits.RA1 = (map & 0x4) ? 1 : 0;
        bcf	5,1	
        btfsc	125,2
        bsf	5,1	

          //PORTAbits.RA2 = (map & 0x8) ? 1 : 0;
        bcf	5,2	
        btfsc	125,3
        bsf	5,2	

            //PORTCbits.RC0 = (map & 0x10) ? 1 : 0;
        bcf	7,0	
        btfsc	125,4
        bsf	7,0	

            // PORTCbits.RC2 = (map & 0x20) ? 1 : 0;
        bcf	7,2	
        btfsc	125,5
        bsf	7,2	

            //PORTAbits.RA4 = (map & 0x40) ? 1 : 0;
        bcf	5,4	
        btfsc	125,6
        bsf	5,4	

            // PORTEbits.RE2 = 0;
        bcf	9,2	;volatile

#endasm
                
    INTCONbits.GIE = 1;
                            
    SEGMENT_LED_dot = 0;
    
    switch (currDig) { 
        case 0:
            DIGIT_DRIVE_2 = 1;
            break;
        case 1:
            DIGIT_DRIVE_1 = 1;
            break;
        case 2:
            if (dotBlink) {
                SEGMENT_LED_dot = blink;
            }
            DIGIT_DRIVE_0 = 1;
            break;
    }
}

void display_poll() {
    if (!timer_check_dirty() || !s_state) {
        return;
    }
    blinkTimer++;
    if ((blinkTimer % BLINK_TICKS) == 0) {
        blink = !blink;
        for (int i = 0; i < 4; i++) {
            if (ledModes[i] == LED_BLINK) {
                set_zone_led(i, blink);
            }
        }
    }
    if (digits[0] >= ANIMATION_START && (blinkTimer % ANIMATION_TICKS) == 0) {
        digits[0]++;
        if (digits[0] >= ANIMATION_START + ANIMATION_LENGTH) {
            digits[0] = ANIMATION_START;
        }
    }
    // 25Hz screen. Advance digit
    next_digit();
}

void display_mode(char modeCode) {
    display_on();
    digits[0] = modeCode;
}

void display_dotBlink(char _dotBlink) { 
    dotBlink = _dotBlink;
}

void display_data(const char* str) {
    display_on();
    if (str[1]) { 
        digits[1] = str[0];
        digits[2] = str[1];
    }
    else {
        digits[1] = 0;
        digits[2] = str[0];
    }
}

// Start irrigation animation on first digit
void display_mode_anim() {
    display_on();
    digits[0] = ANIMATION_START;
}    

static void set_zone_led(char led, char lit) {
    switch (led) {
        case 0:
            BUTTON_LED_1 = lit;
            break;
        case 1:
            BUTTON_LED_2 = lit;
            break;
        case 2:
            BUTTON_LED_3 = lit;
            break;
#ifndef __DEBUG
        case 3:
            BUTTON_LED_4 = lit;
            break;
#endif
    }
}

void disp_zone_led(char led, LED_MODE mode) {
    display_on();
    if (ledModes[led] != mode) {
        ledModes[led] = mode;
        set_zone_led(led, !!mode);
    }
}
