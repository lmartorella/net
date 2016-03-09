#ifndef PROTOCOL_H
#define	PROTOCOL_H

#include "bus.h"
#include "hardware/rs485.h"

#ifdef HAS_BUS_CLIENT
#define prot_control_readW(w) rs485_read((BYTE*)w, 2) 
#define prot_control_read(data, size) rs485_read((BYTE*)data, size)
#define prot_control_writeW(w) rs485_write(FALSE, (BYTE*)&w, 2)
#define prot_control_write(data, size) rs485_write(FALSE, (BYTE*)data, size)
#define prot_control_flush() 
#define prot_control_readAvail() rs485_readAvail()
#else
bit prot_control_readW(WORD* w);
bit prot_control_read(void* data, WORD size);
void prot_control_writeW(WORD w);
void prot_control_write(const void* data, WORD size);
void prot_control_flush();
WORD prot_control_readAvail();
extern bit prot_registered;
#endif

void prot_control_close();
void prot_control_abort();
bit prot_control_isConnected();

void prot_init();
void prot_poll();

// Process WRIT message: read data for sink
// Return TRUE to continue to read, FALSE if read process finished
typedef bit (*Sink_ReadHandler)();
// Process READ message: write data from sink
// Return TRUE to continue to write, FALSE if write process finished
typedef bit (*Sink_WriteHandler)();

bit sink_nullFunc();

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

