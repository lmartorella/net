#include <xc.h>
#include "../hw/ports.h"

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
#pragma config BOR4V = BOR40V   // Brown-out Reset Selection bit (Brown-out Reset set to 4.0V)
#pragma config WRT = OFF        // Flash Program Memory Self Write Enable bits (Write protection off)


static char buttonLed; 
static char segmentLed; 
static char digitDrive;
static char postScalerTimer;

static void setButtonLed(char value) {
    switch (buttonLed) {
        case 0:
            BUTTON_LED_0 = value;
            break;
        case 1:
            BUTTON_LED_1 = value;
            break;
        case 2:
            BUTTON_LED_2 = value;
            break;
        case 3:
            BUTTON_LED_3 = value;
            break;
#ifndef __DEBUG
        case 4:
            BUTTON_LED_4 = value;
            break;
#endif
    }
}

static void setSegmentLed(char value) {
    switch (segmentLed) {
        case 0:
            SEGMENT_LED_a = value;
            break;
        case 1:
            SEGMENT_LED_b = value;
            break;
        case 2:
            SEGMENT_LED_c = value;
            break;
        case 3:
            SEGMENT_LED_d = value;
            break;
        case 4:
            SEGMENT_LED_e = value;
            break;
        case 5:
            SEGMENT_LED_f = value;
            break;
        case 6:
            SEGMENT_LED_g = value;
            break;
        case 7:
            SEGMENT_LED_dot = value;
            break;
    }
}

static void setDigitDrive(char value) {
    switch (digitDrive) {
        case 0:
            DIGIT_DRIVE_0 = value;
            break;
        case 1:
            DIGIT_DRIVE_1 = value;
            break;
        case 2:
            DIGIT_DRIVE_2 = value;
            break;
    }
}

static void setRelais(char addr, char value) {
    switch (addr) {
        case 0:
            RELAIS_0 = value;
            break;
        case 1:
            RELAIS_1 = value;
            break;
        case 2:
            RELAIS_2 = value;
            break;
        case 3:
            RELAIS_3 = value;
            break;
    }
}

static char buttonPressed(char addr) {
    switch (addr) {
        case 0:
            return !BUTTON_IN_0;
        case 1:
            return !BUTTON_IN_1;
        case 2:
            return !BUTTON_IN_2;
        case 3:
            return !BUTTON_IN_3;
#ifndef __DEBUG
        case 4:
            return !BUTTON_IN_4;
#endif
        case 5:
            return !BUTTON_IN_EXT;
    }
    return 0;
}

static void nextButtonLed() {
    setButtonLed(0);
    buttonLed = (buttonLed + 1) % 5;
    setButtonLed(1);
}

static void next7segmentLed() {
    setSegmentLed(0);
    segmentLed = (segmentLed + 1) % 8;
    setSegmentLed(1);
}

static void nextDigitDrive() {
    setDigitDrive(0);
    digitDrive = (digitDrive + 1) % 3;
    setDigitDrive(1);
}

static void interrupt low_isr() {
    INTCONbits.T0IF = 0;
    postScalerTimer++;
    // Count to 8 (0.5sec)
    if ((postScalerTimer % 4) == 0) {
        // Advance button LEDs
        nextButtonLed();
        next7segmentLed();
    }
    if ((postScalerTimer % 32) == 0) {
        // Advance drive
        nextDigitDrive();
    }
}

void main(void) {
    // Test all output ports individually
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
    
    // Enable digital I/O on PORTB
    ANSELH = 0;
    
    for (buttonLed = 0; buttonLed < 5; buttonLed++) {
        setButtonLed(0);
    }
    buttonLed = 0;
    for (segmentLed = 0; segmentLed < 8; segmentLed++) {
        setSegmentLed(0);
    }
    buttonLed = 0;
    for (digitDrive = 0; digitDrive < 3; digitDrive++) {
        setDigitDrive(digitDrive == 0);
    }
    digitDrive = 0;

    /*
    for (int i = 0; i < 4; i++) {
        setRelais(i, 1);
    }
     * */

    // relais
    TRISDbits.TRISD4 = 0;
    PORTDbits.RD4 = 0;
    TRISDbits.TRISD5 = 0;
    PORTDbits.RD5 = 0;
    TRISCbits.TRISC4 = 0;
    PORTCbits.RC4 = 0;
    TRISCbits.TRISC5 = 0;
    PORTCbits.RC5 = 0;

    // Set timer .5 seconds
    // 1:256 = 3900Hz at 4Mhz -> overflow each 15Hz
    OPTION_REG = 0x87;
    
    INTCONbits.GIE = 1;
    INTCONbits.T0IE = 1;

    OPTION_REGbits.nRBPU = 0;
    WPUB = 0xff;
    
    while (1) {
        CLRWDT();
        for (int b = 0; b < 4; b++) {
            setRelais(b, buttonPressed(b + 1));
        }
        if (buttonPressed(0)) {
            buttonLed = 0;
        }
        if (buttonPressed(5)) {
            buttonLed = 3;
        }
    }
}
