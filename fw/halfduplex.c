#include "pch.h"
#include "halfduplex.h"
#include "appio.h"
#include "protocol.h"
#include "hardware/max232.h"

#ifdef HAS_MAX232_SOFTWARE

const Sink g_halfDuplexSink = { { "SLIN" },
                             &halfduplex_readHandler,
                             &halfduplex_writeHandler
};

static bit s_isRxBuffer;
static bit s_isTxBuffer;
static WORD s_size;
static BYTE s_count;
static BYTE* s_ptr;

void halfduplex_init()
{
    s_isRxBuffer = s_isTxBuffer = 0;
}

bit halfduplex_readHandler()
{
    if (!s_isRxBuffer) {
        // IN HEADER READ
        // Wait for size first
        if (prot_control_readAvail() < 2) {
            // Go on
            return 1;
        }
        // Have size
        prot_control_readW(&s_size);
        if (s_size == 0) {
            goto goread;
        }
        else {
            // Wait for data
            s_isRxBuffer = 1;
            s_count = 0;
            s_ptr = max232_buffer1;
            // Go again
            return 1;
        }
    }
    else {
        // IN BUFFER READ
        while (prot_control_readAvail()) {
            prot_control_read(s_ptr, 1);
            s_count++;
            // Read buffer data
            if (s_count == MAX232_BUFSIZE1) {
                s_ptr = max232_buffer2;
            }
            else {
                s_ptr++;
            }
        }
        // Ask for more data?
        if (s_count < s_size) {
            return 1;
        }
        else { 
            goto goread;
        }
    }
    
goread:
    // Disable bus. Start read.
    s_size = max232_sendReceive(s_size);
    // Resume everything
    halfduplex_init();
    // Enough
    return 0;
}

bit halfduplex_writeHandler()
{
    if (!s_isTxBuffer) {
        // IN HEADER WRITE
        prot_control_writeW(s_size);
        s_count = 0;
        s_ptr = max232_buffer1;
        s_isTxBuffer = 1;
        if (s_size == 0) {
            // Buffer ok
            halfduplex_init();
            return 0;
        }
        else {
            s_isTxBuffer = 1;
            // Go on
            return 1;
        }
    }
    else {
        // Write max 0x10 bytes at a time
        while (prot_control_writeAvail()) {
            prot_control_write(s_ptr, 1);
            s_count += 1;
            if (s_count == MAX232_BUFSIZE1) {
                s_ptr = max232_buffer2;
            }
            else {
                s_ptr++;
            }
        }
        // Ask for more data?
        if (s_count < s_size) {
            // Again
            return 1;
        }
        else { 
            // End.
            halfduplex_init();
            return 0;
        }
    }
}

#endif