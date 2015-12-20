/* 
 * File:   usart.h
 * Author: Luciano
 *
 * Created on 20 dicembre 2015, 19.03
 */

#ifndef RS485_H
#define	RS485_H
#include "fuses.h"

#if HAS_RS485

/**
 * Initialize asynchronous mode, but only half-duplex is used
 */
void rs485_reset();
void rs485_write(void* data, WORD size);
void rs485_read(void* data, WORD size);


#endif

#endif	/* USART_H */

