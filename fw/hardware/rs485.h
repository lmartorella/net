#ifndef RS485_H
#define	RS485_H

#ifdef HAS_RS485

typedef enum {
    RS485_NO_ERR = 0,
    RS485_FRAME_ERR,
    RS485_OVERRUN_ERR
} RS485_ERR;

/**
 * Initialize asynchronous mode, but only half-duplex is used
 */
void rs485_init();
/**
 * To feed/receive channel
 */
void rs485_interrupt();

/**
 * Requires 1ms poll time
 */
void rs485_poll();

// Enqueue bytes to send. Use 9-bit address. Buffer is copied (max. 32 bytes)
void rs485_write(BOOL address, BYTE* data, int size);
// Read data, if available.
BOOL rs485_read(BYTE* data, int size, BOOL* rc9);
int rs485_readAvail();
RS485_ERR rs485_getError();

#endif

#endif	/* USART_H */

