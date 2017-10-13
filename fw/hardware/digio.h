#ifndef DIGIO_H
#define	DIGIO_H

#include "../protocol.h"

#ifdef HAS_DIGIO

#define DIGIO_IN_SINK_ID "DIAR"
#define DIGIO_OUT_SINK_ID "DOAR"
void digio_init();
bit digio_in_write();
bit digio_out_read();
bit digio_out_write();

#endif

#endif	/* DIGIO_H */

