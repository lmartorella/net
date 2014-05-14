#ifndef _PROT_H_APP_
#define _PROT_H_APP_

// Manage poll activities
void prot_poll(void);

// Manage slow timer (1sec) activities
void prot_slowTimer(void);

typedef void (*Action)(void);

// Sink for flash ROM (flasher sink type)
typedef enum
{
    SINK_DISPLAY_TYPE = 1,
    SINK_FLASHER_TYPE = 2,
    SINK_AUDIO_TYPE = 3
} SINK_TYPES;

// Class virtual pointers
typedef struct SinkStruct
{	
	// Device ID
	SINK_TYPES deviceId;
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

#define BASE_SINK_PORT 20000

#endif
