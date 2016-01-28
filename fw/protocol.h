#ifndef PROTOCOL_H
#define	PROTOCOL_H

BOOL prot_control_readW(WORD* w);
BOOL prot_control_read(void* data, WORD size);
void prot_control_writeW(WORD w);
void prot_control_write(const void* data, WORD size);
void prot_control_flush();
void prot_control_close();
BOOL prot_control_isListening();
WORD prot_control_getDataSize();

extern BOOL prot_registered;
extern BOOL prot_started;

inline void prot_poll();
void prot_slowTimer();

// Process WRIT message: read data for sink
// Return TRUE to continue to read, FALSE if read process finished
typedef BOOL (*Sink_ReadHandler)();
// Process READ message: write data from sink
// Return TRUE to continue to write, FALSE if write process finished
typedef BOOL (*Sink_WriteHandler)();

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

#endif	/* PROTOCOL_H */

