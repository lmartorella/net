#ifndef _PROT_H_APP_
#define _PROT_H_APP_

// Manage poll activities
void prot_poll(void);

// Manage slow timer (1sec) activities
void prot_slowTimer(void);

// Sink for flash ROM (flasher sink type)
#define SINK_FLASHER_PORT 20001

#endif
