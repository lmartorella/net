#ifndef SINKS_H_
#define SINKS_H_

/**
 * Sink definition and API module
 */

#include "hardware/hw.h"

#ifdef HAS_BUS

/**
 * Init sinks
 */
void sinks_init();

/**
 * Poll sinks
 */
void sinks_poll();

/**
 * The system sink type ID
 */
#define SINK_SYS_ID "SYS "

/**
 * The system sink read function
 */
bit sys_read();
/**
 * The system sink write function
 */
bit sys_write();

// Returns 1 if more data should be read or writter, 0 if done and the socket can  be freed.
// The 'write' function sends data to master. The 'read' function receives data from master.
typedef bit (*SinkFunction)();

/**
 * The concatenation of all sink IDS
 */
extern const char* const SINK_IDS;

/**
 * The sink count
 */
extern const int SINK_IDS_COUNT;

/**
 * The sink read handlers
 */
extern const SinkFunction const sink_readHandlers[];
/**
 * The sink write handlers
 */
extern const SinkFunction const sink_writeHandlers[];

#endif
#endif
