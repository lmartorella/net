#ifndef XC_RS232_H
#define	XC_RS232_H

#if defined(HAS_MAX232_SOFTWARE) || defined(HAS_FAKE_RS232)

// Connection 9600,N,8,1
void max232_init();

#define MAX232_BUFSIZE1 0x22
#define MAX232_BUFSIZE2 0x22
extern BYTE max232_buffer1[MAX232_BUFSIZE1];
extern BYTE max232_buffer2[MAX232_BUFSIZE2];

// Disable interrupts. Send txSize byte from the buffer and then receive in the buffer, and returns the size
// Timeout of 0.05s of no data
int max232_sendReceive(int txSize);

// Disable interrupts. Send txSize byte from the buffer
void max232_send(int txSize);

#endif

#endif	/* XC_RS232_H */

