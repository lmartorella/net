#ifndef RS485_H
#define	RS485_H

#ifdef HAS_RS485

/**
 * Initialize asynchronous mode, but only half-duplex is used
 */
void rs485_init();
/**
 * To feed/receive channel
 */
void rs485_interrupt();

/**
 * To poll at 10ms timers
 */
void rs485_poll();

// Enqueue bytes to send. Use 9-bit address. Buffer is copied (max. 64 bytes)
void rs485_write(BOOL address, void* data, int size);
void rs485_startRead();
// If size -1, error occurred
BYTE* rs485_read(int* size, BOOL* rc9);

#endif

#endif	/* USART_H */

