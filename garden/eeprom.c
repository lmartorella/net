#include <xc.h>
#include "eeprom.h"

static __eeprom char BUFFER[8];

void eprom_write(char* src, char size) {
    char address = (char)BUFFER;
    EECON1bits.WREN = 1;
    EECON1bits.EEPGD = 0;
    for (; size != 0; size--) {
        EEADR = address++;
        EEDAT = *(src++);
        
        INTCONbits.GIE = 0;
        while (INTCONbits.GIE);
        EECON2 = 0x55;
        EECON2 = 0xAA;
        EECON1bits.WR = 1;
        INTCONbits.GIE = 1;
        while (EECON1bits.WR);
    }
    EECON1bits.WREN = 0;
}

void eprom_read(char* dest, char size) {
    char address = (char)BUFFER;
    EECON1bits.EEPGD = 0;
    for (; size != 0; size--) {
        EEADR = address++;
        EECON1bits.RD = 1;
        *(dest++) = EEDAT;
    }
}
