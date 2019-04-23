#ifndef XC_OUTPUT_H
#define	XC_OUTPUT_H

typedef unsigned char ZONE_MASK;

void output_setup();

void output_set(ZONE_MASK zones);
void output_clear();


#endif	/* XC_OUTPUT_H */

