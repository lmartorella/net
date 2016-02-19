#ifndef RS485_H
#define	RS485_H

#ifdef HAS_RS485

typedef enum {
    RS485_LINE_RX,
    RS485_LINE_TX,
    RS485_FRAME_ERR,
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
 * Poll as much as possible (internal timered)
 */
void rs485_poll();

// Enqueue bytes to send. Use 9-bit address. Buffer is copied (max. 32 bytes)
void rs485_write(BOOL address, const BYTE* data, BYTE size);
// Read data, if available.
BOOL rs485_read(BYTE* data, BYTE size);
BYTE rs485_readAvail();
BYTE rs485_writeAvail();
// Get the last bit9 received
extern BOOL rs485_lastRc9;
// Get/set the skip flag. If set, rc9 = 0 bytes are skipped by receiver
extern BOOL rs485_skipData;

RS485_STATE rs485_getState();


#endif

#endif	/* USART_H */

