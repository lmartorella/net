#ifndef _PROT_H_APP_
#define _PROT_H_APP_

#define SERVER_CONTROL_UDP_PORT 17007
#define CLIENT_TCP_PORT 20000

void prot_poll(void);
void prot_slowTimer(void);

BOOL prot_control_readW(WORD* w);
BOOL prot_control_read(void* data, WORD size);
void prot_control_writeW(WORD w);
void prot_control_write(void* data, WORD size);

// Process WRIT message: read data for sink
// Return TRUE to continue to read, FALSE if read process finished
typedef BOOL (*Sink_ReadHandler)();
// Process READ message: write data from sink in one go
typedef void (*Sink_WriteHandler)();

// Class virtual pointers
typedef struct SinkStruct
{	
	// Device ID
	FOURCC fourCc;
	// Pointer to RX function
	Sink_ReadHandler readHandler;
	// Pointer to TX function
	Sink_WriteHandler writeHandler;
} Sink;

extern const Sink* AllSinks[];
extern int AllSinksSize;

#endif //#ifdef HAS_IP

