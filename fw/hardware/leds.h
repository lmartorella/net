#ifndef LEDS_H
#define	LEDS_H

#define led_init() { LED_PORTBIT = 0; LED_TRISBIT = 0; }
#define led_off() { LED_PORTBIT = 0; }
#define led_on() { LED_PORTBIT = 1; }

#endif	/* LEDS_H */

