#ifndef XC_HFD_TEMPLATE_H
#define	XC_HFD_TEMPLATE_H

#include "protocol.h"

#if defined(HAS_BUS) && defined(HAS_MAX232_SOFTWARE)

void halfduplex_init();
extern const Sink g_halfDuplexSink;

#endif

#endif	/* XC_HFD_TEMPLATE_H */

