#include "pch.h"
#include "halfduplex.h"
#include "appio.h"
#include "protocol.h"
#include "hardware/max232.h"
#include "hardware/rs485.h"

#ifdef HAS_MAX232_SOFTWARE

static bit halfduplex_readHandler();
static bit halfduplex_writeHandler();

const Sink g_halfDuplexSink = { { "SLIN" },
                             &halfduplex_readHandler,
                             &halfduplex_writeHandler
};

static WORD s_count;
static BYTE s_pos;
static BYTE* s_ptr;

static enum {
    ST_IDLE,
    // Receiving data, len OK
    ST_RECEIVE_SIZE,
    // Receiving data
    ST_RECEIVE_DATA,

    // Transmit data
    ST_TRANSMIT_DATA,
            
    ST_READY_TO_COMM,

} s_state;

void halfduplex_init()
{
    s_state = ST_IDLE;
}

void halfduplex_poll()
{
    if (s_state == ST_READY_TO_COMM && bus_isIdle()) {
        
#ifdef DEBUGMODE
        printch('#');
        printch('0' + (s_count / 10));
        printch('0' + (s_count % 10));
        // Echo back same data
#else
        // Disable bus. Start read. Blocker.
        INTCONbits.GIE = 0;
        s_count = max232_sendReceive(s_count);
        // Before resetting interrupts, reset RS485 state (it could have gone underrun)
        rs485_init();
        INTCONbits.GIE = 1;
#endif
        // Resume everything
        s_state = ST_IDLE;

#ifdef DEBUGMODE
        printch('&');
        printch('0' + s_count);
#endif
    }
}

static bit halfduplex_readHandler()
{
    if (s_state != ST_RECEIVE_DATA) {
        // IN HEADER READ
        s_state = ST_RECEIVE_SIZE;
        // Wait for size first
        if (prot_control_readAvail() < 2) {
            // Go on
            return 1;
        }
        // Have size
        prot_control_readW(&s_count);
        // Wait for data
        s_state = ST_RECEIVE_DATA;
        s_ptr = max232_buffer1;
        s_pos = 0;
    }
    
    // I'm in ST_RECEIVE_DATA mode
    while (prot_control_readAvail() && s_pos < (BYTE)s_count) {
        prot_control_read(s_ptr, 1);
        s_pos++;
        // Read buffer data
        if (s_pos == MAX232_BUFSIZE1) {
            s_ptr = max232_buffer2;
        }
        else {
            s_ptr++;
        }
    }

    // Ask for more data?
    if (s_pos < (BYTE)s_count) {
        // Again
        return 1;
    }
    else {
        // Else data OK
        s_state = ST_READY_TO_COMM;
#ifdef DEBUGMODE
        printch('@');
#endif
        // Stop data
        return 0;
    }
}

static bit halfduplex_writeHandler()
{
    if (s_state != ST_TRANSMIT_DATA) {
        // IN HEADER WRITE
        prot_control_writeW(s_count);
        s_ptr = max232_buffer1;
        s_pos = 0;
        s_state = ST_TRANSMIT_DATA;
    }

    // Write max 0x10 bytes at a time
    while (prot_control_writeAvail() && s_pos < (BYTE)s_count) {
        prot_control_write(s_ptr, 1);
        s_pos++;
        if (s_pos == MAX232_BUFSIZE1) {
            s_ptr = max232_buffer2;
        }
        else {
            s_ptr++;
        }
    }
   
    // Ask for more data?
    if (s_pos < (BYTE)s_count) {
        // Again
        return 1;
    }
    else { 
        // End of transmit task -> reset sink state
        s_state = ST_IDLE;
        return 0;
    }
}

#endif