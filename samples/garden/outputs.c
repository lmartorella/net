#include <xc.h>
#include "hw/ports.h"
#include "outputs.h"

void output_setup() { 
    // relais
    TRISDbits.TRISD4 = 0;
    PORTDbits.RD4 = 0;
    TRISDbits.TRISD5 = 0;
    PORTDbits.RD5 = 0;
    TRISCbits.TRISC4 = 0;
    PORTCbits.RC4 = 0;
    TRISCbits.TRISC5 = 0;
    PORTCbits.RC5 = 0;
}

void output_clear() {
   RELAIS_0 = 0;
   RELAIS_1 = 0;
   RELAIS_2 = 0;
   RELAIS_3 = 0;
}

void output_set(ZONE_MASK zones) { 
    if (zones & 1) {
        RELAIS_0 = 1;
    }
    if (zones & 2) {
        RELAIS_1 = 1;
    }
    if (zones & 4) {
        RELAIS_2 = 1;
    }
    if (zones & 8) {
        RELAIS_3 = 1;
    }
}