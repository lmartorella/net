#include "../../../src/nodes/pch.h"
#include "../../../src/nodes/protocol.h"
#include "../../../src/nodes/appio.h"
#include "displaySink.h"

#if defined(HAS_BUS) && (defined(HAS_CM1602) || defined(_CONF_RASPBIAN))

#ifdef HAS_CM1602
#elif defined(_CONF_RASPBIAN)
#define CM1602_COL_COUNT 80
#define CM1602_LINE_COUNT 25
#endif

static BYTE s_buf[CM1602_COL_COUNT];
static WORD s_length;
static BYTE s_pos;
static enum {
    STATE_NONE,
    STATE_LEN_READY,
} s_state = STATE_NONE;

bit line_read()
{
    if (s_state == STATE_NONE) {
        if (prot_control_readAvail() < 2) {
            // Continue write
            return 1;
        }
        prot_control_readW(&s_length);
        s_pos = 0;
        s_state = STATE_LEN_READY;
    }
    
    while (s_length > 0) {
        if (prot_control_readAvail() == 0) {
            // Continue
            return 1;
        }      
        
        // Else read
        prot_control_read(s_buf + s_pos, 1);
        s_pos++;
        s_length--;
    }

    // Done
    s_state = STATE_NONE;
    s_buf[s_pos] = 0;
    io_printlnStatus(s_buf);
    return 0;
}

bit line_write()
{
    // Num of lines
    WORD l = CM1602_LINE_COUNT - 1;
    prot_control_writeW(l);

    // Num of columns
    l = CM1602_COL_COUNT - 1;
    prot_control_writeW(l);

   // Done
   return FALSE;
}

#endif