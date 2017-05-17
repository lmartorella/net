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

void output_set(int zone, char state) { 
    switch (zone) {
        case 0:
            RELAIS_0 = state;
            break;
        case 1:
            RELAIS_1 = state;
            break;
        case 2:
            RELAIS_2 = state;
            break;
        case 3:
            RELAIS_3 = state;
            break;
    }
}