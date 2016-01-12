#ifndef RS485_H
#define	RS485_H

#ifdef HAS_RS485

/**
 * Initialize asynchronous mode, but only half-duplex is used
 */
void rs485_init();
void rs485_interrupt();
void rs485_poll();
void rs485_write(void* data, WORD size);
void rs485_read(void* data, WORD size);

#endif

#endif	/* USART_H */

