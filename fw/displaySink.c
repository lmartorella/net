#include "pch.h"
#include "displaySink.h"
#include "appio.h"
#include "protocol.h"
#include "hardware/cm1602.h"

#ifdef HAS_CM1602

static bit readHandler();
static bit writeHandler();

const Sink g_displaySink = { { "LINE" },
                             &readHandler,
                             &writeHandler
};

static bit readHandler()
{
    // TODO: Only single packet messages are supported
    WORD length, toSkip = 0;
    BYTE buf[CM1602_COL_COUNT];
    prot_control_readW(&length);
    if (length >= CM1602_COL_COUNT)
    {
        toSkip = length - (CM1602_COL_COUNT - 1);
        length = CM1602_COL_COUNT - 1;
    }
    // Read char to display
    prot_control_read(buf, length);
    // Skip the others
    while (toSkip > 0)
    {
        prot_control_read(buf + (CM1602_COL_COUNT - 1), 1);
        toSkip--;
    }
    buf[length] = '\0';
    // Write it
    printlnUp(buf);
    return FALSE;
}

static bit writeHandler()
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