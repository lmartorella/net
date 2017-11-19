#ifndef SINKS_H_
#define SINKS_H_

#ifdef HAS_BUS

#define SINK_SYS_ID "SYS "
bit sys_read();
bit sys_write();

// Returns 1 if more data should follow, 0 if finished
typedef bit (*SinkFunction)();
extern const char* const SINK_IDS;
extern const int SINK_IDS_COUNT;
extern const SinkFunction const sink_readHandlers[];
extern const SinkFunction const sink_writeHandlers[];

#endif
#endif