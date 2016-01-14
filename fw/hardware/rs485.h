#ifndef RS485_H
#define	RS485_H

#ifdef HAS_RS485

/**
 * Initialize asynchronous mode, but only half-duplex is used
 */
void rs485_init();
void rs485_interrupt();
//void rs485_poll();

// Enqueue bytes to send. Use 9-bit address.
void rs485_write(BOOL address, void* data, BYTE size);
// Still busy sending?
BOOL rs485_busy();

//void rs485_read(void* data, BYTE size);

#endif

#endif	/* USART_H */

