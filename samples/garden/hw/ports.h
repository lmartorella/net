#ifndef XC_PORTS_H
#define	XC_PORTS_H

#ifdef __DEBUG
#define DEBUG_PINS
#endif

#define BUTTON_LED_0 PORTDbits.RD2
#define BUTTON_LED_1 PORTDbits.RD3
#define BUTTON_LED_2 PORTEbits.RE1
#define BUTTON_LED_3 PORTBbits.RB5
#if !defined(DEBUG_PINS)
// ICD uses RB7
#define BUTTON_LED_4 PORTBbits.RB7
#endif

#define BUTTON_IN_0 PORTBbits.RB4
#define BUTTON_IN_1 PORTBbits.RB1
#define BUTTON_IN_2 PORTBbits.RB3
#define BUTTON_IN_3 PORTBbits.RB0
#if !defined(DEBUG_PINS)
// ICD uses RB6
#define BUTTON_IN_4 PORTBbits.RB6
#endif
#define BUTTON_IN_EXT PORTBbits.RB2
#if !defined(DEBUG_PINS)
#define BUTTON_IN_MASK 0x5f
#else
#define BUTTON_IN_MASK 0x1f
#endif

#define SEGMENT_LED_a PORTCbits.RC1
#define SEGMENT_LED_b PORTAbits.RA0
#define SEGMENT_LED_c PORTAbits.RA1
#define SEGMENT_LED_d PORTAbits.RA2
#define SEGMENT_LED_e PORTCbits.RC0
#define SEGMENT_LED_f PORTCbits.RC2
#define SEGMENT_LED_g PORTAbits.RA4
#define SEGMENT_LED_dot PORTEbits.RE2

#define DIGIT_DRIVE_0 PORTCbits.RC3
#define DIGIT_DRIVE_1 PORTDbits.RD6
#define DIGIT_DRIVE_2 PORTDbits.RD0

#define RELAIS_0 PORTDbits.RD5   // pin7 ribbon cable
#define RELAIS_1 PORTDbits.RD4   // pin4
#define RELAIS_2 PORTCbits.RC5   // pin2
#define RELAIS_3 PORTCbits.RC4   // pin1


#endif	/* XC_PORTS_H */

