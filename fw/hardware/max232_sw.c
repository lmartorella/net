#include "../pch.h"
#include "max232.h"

#ifdef HAS_MAX232_SOFTWARE

void max232_init() {
    // Init I and O bits
    RS232_RX_TRIS = 1;
    RS232_TX_TRIS = 0;
    
    RS232_TX_PORT = 0; // unasserted
}

static void WAITBIT() {
    
}

// Write sync, disable interrupts
void max232_write(BYTE* data, BYTE size) {
    
    disableInterrupts();
    
    for (; size > 0; size--, data++) {
        BYTE b = *data;
        // Write a START bit
        RS232_TX_PORT = 1;
        WAITBIT();
        for (int j = 0; j < 8; j++) {
            // Cycle bits
            RS232_TX_PORT = (b & 0x80);
            b <<= 1;
            WAITBIT();
        }
        // Write a STOP bit
        RS232_TX_PORT = 0;
        WAITBIT();
    }

    enableInterrupts();
}
        
#endif
