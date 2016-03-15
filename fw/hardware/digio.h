#ifndef DIGIO_H
#define	DIGIO_H

#include "../protocol.h"

#ifdef HAS_DIGIO

extern const Sink g_outSink;
extern const Sink g_inSink;
void digio_init();

#endif

#endif	/* DIGIO_H */

