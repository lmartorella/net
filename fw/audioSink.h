/* 
 * File:   audioSink.h
 * Author: Lucky
 *
 */

#ifndef AUDIO_SINK_H
#define	AUDIO_SINK_H

#include "protocol.h"
// The public sink descriptor
extern const Sink g_audioSink;

// Poll 
void audio_pollMp3Player();

#endif	/* AUDIO_SINK_H */

