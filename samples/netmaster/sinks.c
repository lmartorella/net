#include <net/net.h>

const char* const SINK_IDS = SINK_SYS_ID;
const int SINK_IDS_COUNT = 1;
const SinkFunction const sink_writeHandlers[] = { sys_write_prim };
const SinkFunction const sink_readHandlers[] = { sys_read_prim };

void sinks_init() {
}

_Bool sinks_poll() {
    return false;
}