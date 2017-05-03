/* 
 * File:   displaySink.h
 * Author: Lucky
 *
 * Created on May 7, 2014, 10:51 PM
 */

#ifndef DISPLAYSINK_H
#define	DISPLAYSINK_H

#include "protocol.h"

#ifdef HAS_BUS
#ifdef HAS_CM1602

// The public sink descriptor
extern const Sink g_displaySink;

#endif
#endif

#endif	/* DISPLAYSINK_H */

