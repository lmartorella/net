#ifndef DIG_COUNTER_H
#define	DIG_COUNTER_H

#ifdef HAS_DIGITAL_COUNTER

void dcnt_interrupt();
void dcnt_init();
void dcnt_poll();

#endif

#endif	/* COUNTER_H */

