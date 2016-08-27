#ifndef XC_HFD_TEMPLATE_H
#define	XC_HFD_TEMPLATE_H

#include "protocol.h"

void halfduplex_init();
bit halfduplex_readHandler();
bit halfduplex_writeHandler();
extern const Sink g_halfDuplexSink;

#endif	/* XC_HFD_TEMPLATE_H */

