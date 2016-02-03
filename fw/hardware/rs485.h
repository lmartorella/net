#ifndef RS485_H
#define	RS485_H

#ifdef HAS_RS485

typedef enum {
    RS485_LINE_RX = 0x4,
    RS485_LINE_TX = 0x8,
    RS485_ERR = 0x10,
    RS485_FRAME_ERR = RS485_ERR,
    RS485_OVERRUN_ERR = RS485_ERR + 1
} RS485_STATE;

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
void rs485_write(BOOL address, const BYTE* data, int size);
// Read data, if available.
BOOL rs485_read(BYTE* data, int size, BOOL* rc9);
int rs485_readAvail();

RS485_STATE rs485_getState();

#endif

#endif	/* USART_H */

