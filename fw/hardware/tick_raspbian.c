#include "../pch.h"
#include "tick.h"
#include <sys/time.h>

DWORD TickGet() {
    struct timeval tv;
    if (gettimeofday(&tv, NULL) != 0) {
        fatal("gettimeofday");
    }
    return tv.tv_usec;
}

void TickUpdate() {
    
}

void timers_init() {

}
