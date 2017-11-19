#ifndef DISPLAY_H
#define	DISPLAY_H

void display_setup();
void display_off();
void display_poll();

void display_mode(char modeCode);
void display_data(const char* str);
void display_dotBlink(char _dotBlink);

// Display last E with stopped timers
void display_error_fixed();

// Start irrigation animation on first digit
void display_mode_anim();

typedef enum {
    LED_OFF = 0,
    LED_ON,
    LED_BLINK
} LED_MODE;

void disp_zone_led(char led, LED_MODE mode);

#endif	/* DISPLAY_H */

