#ifndef XC_OUTPUT_H
#define	XC_OUTPUT_H

#include "hw/ports.h"
typedef unsigned char ZONE_MASK;

void output_setup();

void output_set(ZONE_MASK zones);
void output_clear_zones();

#define output_clear_pwr() RELAIS_PWR = 0; 
#define output_pwr() RELAIS_PWR = 1;

#endif	/* XC_OUTPUT_H */

