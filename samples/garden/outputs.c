#include <xc.h>
#include "hw/ports.h"
#include "outputs.h"

void output_setup() { 
    // relais
    RELAIS_0_TRIS = 0;
    RELAIS_1_TRIS = 0;
    RELAIS_2_TRIS = 0;
    RELAIS_3_TRIS = 0;
    // power relais
    RELAIS_PWR_TRIS = 0;
}

void output_clear_zones() {
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