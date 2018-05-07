#include "../pch.h"
#include "audioSink.h"
#include "bpm180.h"
#include "dcf77.h"
#include "spiram.h"

void apps_init() 
{
#ifdef HAS_VS1011
    vs1011_init();
#endif

#ifdef HAS_DCF77
    dcf77_init();
#endif

#ifdef HAS_BPM180
    bpm180_init();
#endif      
}

void apps_poll() 
{
#if HAS_VS1011
        audio_pollMp3Player();
#endif

#ifdef HAS_DCF77
        dcf77_poll();
#endif

#ifdef HAS_BPM180
    bpm180_poll();
#endif
}
