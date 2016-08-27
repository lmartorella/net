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
    
    RS232_TX_PORT = 0; // unasserted
    
    RS232_TCON = RS232_TCON_OFF;
}

#define RESETTIMER() { RS232_TCON_HREG = RS232_TCON_HVALUE; RS232_TCON_LREG = RS232_TCON_LVALUE; RS232_TCON_IF = 0; }
#define WAITBIT() { while(!RS232_TCON_IF) CLRWDT(); RESETTIMER(); }

// Write sync, disable interrupts
int max232_sendReceive(int size) {
    
    bus_suspend();
    disableInterrupts();

    RESETTIMER();
    RS232_TCON = RS232_TCON_ON;

    BYTE j, b, i;
    
    BYTE* ptr = max232_buffer1;
    for (i = 0; i < size; i++) {
        b = *ptr;
        // Write a START bit
        WAITBIT();
        RS232_TX_PORT = 1;
        for (j = 0; j < 8; j++) {
            // Cycle bits
            WAITBIT();
            RS232_TX_PORT = (b & 0x80);
            b <<= 1;
        }
        // Write a STOP bit
        WAITBIT();
        RS232_TX_PORT = 0;
        
        if (i == MAX232_BUFSIZE1) {
            ptr = max232_buffer2;
        }
        else {
            ptr++;
        }
    }

    // Now receive
    ptr = max232_buffer1;
    i = 0;

loop:
    // Wait for start bit
    RS232_TCON_HREG = RS232_TCON_HVALUE_TIMEOUT; 
    RS232_TCON_LREG = RS232_TCON_LVALUE_TIMEOUT; 
    RS232_TCON_IF = 0;
    while (!RS232_RX_PORT) {
        if (RS232_TCON_IF) {
            goto end;
        }
    }

    // Read bits
    // Sample in the middle
    RS232_TCON_HREG = RS232_TCON_HVALUE_HALF; 
    RS232_TCON_LREG = RS232_TCON_LVALUE_HALF; 
    RS232_TCON_IF = 0;
    WAITBIT();
    b = 0;
    for (j = 0; j < 8; j++) {
        WAITBIT();
        if (RS232_RX_PORT) {
            b = b | 1;
        }
        b <<= 1;
    }
    *ptr = b;
    i++;
    if (i == MAX232_BUFSIZE1) {
        ptr = max232_buffer2;
    }
    else {
        ptr++;
    }
    WAITBIT(); // Wait for stop bit
    goto loop;    

end:
    RS232_TCON = RS232_TCON_OFF;
    enableInterrupts();
    bus_resume();
    
    return i;
}
        
#endif
