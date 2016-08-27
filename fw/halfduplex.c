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
        // Wait for size first
        if (prot_control_readAvail() < 2) {
            // Go on
            return TRUE;
        }
        // Have size
        prot_control_readW(&s_size);
        if (s_size == 0) {
            // Disable bus. Start read.
            s_size = max232_sendReceive(s_size);
            // Resume everything
            halfduplex_init();
            return FALSE;
        }
        else {
            // Wait for data
            s_isRxBuffer = 1;
            s_count = 0;
            s_ptr = max232_buffer1;
            return TRUE;
        }
    }
    else {
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
            return TRUE;
        }
        else { 
            // Disable bus. Start read.
            s_size = max232_sendReceive(s_size);
            // Resume everything
            halfduplex_init();
            return FALSE;
        }
    }
}

bit halfduplex_writeHandler()
{
    if (!s_isTxBuffer) {
        prot_control_writeW(s_size);
        s_count = 0;
        s_ptr = max232_buffer1;
        s_isTxBuffer = 1;
        if (s_size == 0) {
            halfduplex_init();
            return FALSE;
        }
        else {
            s_isTxBuffer = 1;
            return TRUE;
        }
    }
    else {
        // Write max 0x10 bytes at a time
        if ((s_size - s_count) > 0x10) {
            prot_control_write(s_ptr, 0x10);
            s_count += 0x10;
            // WARN!! TODO: 0x10 and 0x30 (MAX232_BUFSIZE1) are multiple, so the check below works
            if (s_count == MAX232_BUFSIZE1) {
                s_ptr = max232_buffer2;
            }
            else {
                s_ptr += 0x10;
            }
            return TRUE;
        }
        else {
            // End.
            prot_control_write(s_ptr, (s_size - s_count));
            halfduplex_init();
            return FALSE;
        }
    }
}

#endif