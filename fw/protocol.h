#ifndef _PROT_H_APP_
#define _PROT_H_APP_

// Manage poll activities
void prot_poll(void);

// Manage slow timer (1sec) activities
void prot_slowTimer(void);

typedef void (*Action)(void);

// Class virtual pointers
typedef struct SinkStruct
{	
	// Device ID
	unsigned int deviceId; 
	// Device caps
	unsigned int caps; 
	// Port
	unsigned int port2; 
	// Pointer to create function
	Action createHandler;
	// Pointer to destroy function
	Action destroyHandler;
	// Pointer to POLL
	Action pollHandler;
} Sink;


// Sink for flash ROM (flasher sink type)
#define SINK_DISPLAY_TYPE 1
#define SINK_FLASHER_TYPE 2
#define BASE_SINK_PORT 20000

#endif
