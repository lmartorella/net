#ifndef _BPM180_NET_H
#define _BPM180_NET_H

// BPM180 I2C module to read barometric data (air pressure)

#ifdef HAS_BPM180

void bpm180_init();
void bpm180_poll();

#endif

#endif