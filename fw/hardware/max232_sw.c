#include "../pch.h"
#include "max232.h"
#include "../bus.h"

#ifdef HAS_MAX232_SOFTWARE

BYTE max232_buffer1[MAX232_BUFSIZE1];
BYTE max232_buffer2[MAX232_BUFSIZE2];

void max232_init() {
    // Init I and O bits
    RS232_RX_TRIS = 1;
    RS232_TX_TRIS = 0;
    
    RS232_TX_PORT = 1; // unasserted
    
    RS232_TCON = RS232_TCON_OFF;
    RS232_TCON_REG = RS232_TCON_VALUE; 
}

#define RESETTIMER(t) { RS232_TCON_REG = t; RS232_TCON_ACC = 0; RS232_TCON_IF = 0; }
#define WAITBIT() { while(!RS232_TCON_IF) { CLRWDT(); } RS232_TCON_IF = 0; }

void max232_send(int size) {
    INTCONbits.GIE = 0;

    RESETTIMER(RS232_TCON_VALUE)
    RS232_TCON = RS232_TCON_ON;

    BYTE j, b, i;
    
    BYTE* ptr = max232_buffer1;
    WAITBIT()
    for (i = 0; i < size; i++) {
        // Write a idle bit
        WAITBIT()
        // Write a START bit
        RS232_TX_PORT = 0;
        b = *ptr;
        for (j = 0; j < 8; j++) {
            // Cycle bits
            WAITBIT()
            RS232_TX_PORT = b & 0x1;
            b >>= 1;
        }
        // Write a STOP bit
        WAITBIT()
        RS232_TX_PORT = 1;
        
        if (i == MAX232_BUFSIZE1) {
            ptr = max232_buffer2;
        }
        else {
            ptr++;
        }
        WAITBIT()
    }
    
    INTCONbits.GIE = 1;
}

// Write sync, disable interrupts
int max232_sendReceive(int size) {

    INTCONbits.GIE = 0;

    max232_send(size);
    // Now receive
    BYTE* ptr = max232_buffer1;
    BYTE j, b, i = 0;
    int timeoutCount;

loop:
    // Wait for start bit
    timeoutCount = 480;    // 480 bits = 0.05s
    while (RS232_RX_PORT) {
        CLRWDT();
        if (RS232_TCON_IF) {
            RS232_TCON_IF = 0;
            if ((timeoutCount--) == 0) {
                goto end;
            }
        }
    }

    // Read bits
    // Sample in the middle
    RESETTIMER(RS232_TCON_VALUE_HALF)
    WAITBIT()
    RESETTIMER(RS232_TCON_VALUE)
   
    b = 0;
    for (j = 0; j < 8; j++) {
        WAITBIT()
        b >>= 1;
        if (RS232_RX_PORT) {
            b = b | 0x80;
        }
    }
    *ptr = b;
    i++;
    if (i == MAX232_BUFSIZE1) {
        ptr = max232_buffer2;
    }
    else {
        ptr++;
    }

    // Wait for the stop bit
    WAITBIT();
    // Now in STOP state
    goto loop;    
    
end:
    INTCONbits.GIE = 1;
    return i;
}
        
#endif
