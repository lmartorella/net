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
// Get the last bit9 received
extern BOOL rs485_lastRc9;
// Get/set the skip flag. If set, rc9 = 0 bytes are skipped by receiver
extern BOOL rs485_skipData;

RS485_STATE rs485_getState();

// See OSI model document for timings.
// TICKS_PER_SECOND = 3906 on PIC16 @4MHz
// TICKS_PER_SECOND = 24414 on PIC18 @25MHz
// BYTES_PER_SECONDS = (BAUD / 11 (9+1+1)) = 1744 (round down) = 0.57ms
#define BYTES_PER_SECONDS (DWORD)((RS485_BAUD - 11) / 11)
// 2 ticks per byte, but let's do 3 (round up) for PIC16 @4MHz
// 14 ticks per byte (round up) on PIC18 @25MHz
#define TICKS_PER_BYTE (TICK_TYPE)((TICKS_PER_SECOND + BYTES_PER_SECONDS) / BYTES_PER_SECONDS)

#endif

#endif	/* USART_H */

