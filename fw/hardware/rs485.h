#ifndef RS485_H
#define	RS485_H

#ifdef HAS_RS485

/**
 * Initialize asynchronous mode, but only half-duplex is used
 */
void rs485_init();
void rs485_interrupt();

// Enqueue bytes to send. Use 9-bit address. Buffer is copied (max. 64 bytes)
void rs485_write(BOOL address, void* data, BYTE size);
// If return -1, error occurred
BYTE rs485_dataAvail(BOOL* rc9);
void rs485_startRead();
BYTE* rs485_readBuffer();

#endif

#endif	/* USART_H */

