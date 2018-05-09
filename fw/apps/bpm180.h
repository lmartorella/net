#ifndef _BPM180_NET_H
#define _BPM180_NET_H

// BPM180 I2C module to read barometric data (air pressure)

#ifdef HAS_BPM180_APP

void bpm180_app_init();
void bpm180_app_poll();

#endif

#endif