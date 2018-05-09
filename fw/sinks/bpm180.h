#ifndef _BPM180_H_SINK_
#define	_BPM180_H_SINK_

void bpm180_init();
void bpm180_askIdCalib(BYTE* buffer);
void bpm180_askTemp(BYTE* buffer);
void bpm180_askPressure(BYTE* buffer);
BOOL bpm180_poll();

#endif	

