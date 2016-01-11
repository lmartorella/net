#ifndef _FUSES_INCLUDE_
#define _FUSES_INCLUDE_

//#include <p18f87j60.h>

// PORTA: 0/5, Digital and analog. RA0/RA1 used by ethernet leds
// PORTB: 0/7, interrupt on change. RB6/7 used by ICSP. RB0/5 used by Mp3
// PORTC: 0/7, RC3/4 used by I2C. 1/2 and 6/7 used by EXTRAM I2C. Used by IO modules. 
// PORTD: 0/2
// PORTE: 0/5: Used by CM1602 module (0 and 2/7)
// PORTF: 0/7: digital and analog. Used by IO modules.
// PORTG: 4: 1/2 used by USART2 + 0 used by MAX485 enable logic


// ******* 
// DISPLAY
// Uses PORTE, 0, 2-7
// ******* 
#define HAS_CM1602
#define CM1602_PORT 		PORTE
#define CM1602_TRIS 		TRISE
#define CM1602_PORTADDR         0xF84;
#define CM1602_IF_MODE 		4
#define CM1602_IF_NIBBLE_LOW 	0
#define CM1602_IF_NIBBLE_HIGH 	1
#define CM1602_IF_NIBBLE 	CM1602_IF_NIBBLE_HIGH
#define CM1602_IF_BIT_RW 	PORTEbits.RE2
#define CM1602_IF_BIT_RS 	PORTEbits.RE0
#define CM1602_IF_BIT_EN 	PORTEbits.RE3
#define CM1602_LINE_COUNT 	2
#define CM1602_FONT_HEIGHT 	7

// ******* 
// MEMORY, uses PORTC 1, 2 6 and 7 + I2C
// ******* 
#define MEM_PORT	 PORTC
#define MEM_PORTBITS PORTCbits
#define MEM_TRISBITS TRISCbits
#define MEM_BANK0_CS RC6
#define MEM_BANK1_CS RC2
#define MEM_BANK2_CS RC7
#define MEM_BANK3_CS RC1
#define MEM_BANK_CS_MASK 0b11000110

// ******* 
// MP3, uses PORTB 0/5
// ******* 
#undef HAS_VS1011
#define VS1011_PORT  	PORTB
#define VS1011_PORTBITS PORTBbits
#define VS1011_TRISBITS TRISBbits
#define VS1011_RESET 	RB4
#define VS1011_DREQ  	RB5
#define VS1011_GPIO3 	RB2
#define VS1011_GPIO2  	RB3
#define VS1011_XDCS 	RB0
#define VS1011_XCS	RB1
#define VS1011_XTALI    25000           // in mhz
#define VS1011_CLK_DOUBLING    0


// ******* 
// MEM & LOADER & FLASH
// *******
#define MAX_PROG_MEM		0x20000
#define ROM_BLOCK_SIZE		64
#define CONFIGURATION_SIZE 	8
#endif

// ******
// SPI
// ******
#undef HAS_SPI

// ******
// SPI RAM
// ******
#undef HAS_SPI_RAM

// ******
// IP: uses PORTA0,1 (leds)
// ******
#define HAS_IP

// ******
// IO: uses PORTC and PORTF full
// ******
#define HAS_IO

// ******
// RS485: use USART2 on 18F87J60 (PORTG)
// ******
#define HAS_RS485



#ifdef HAS_IO
#ifdef HAS_SPI
#error Cannot use SPI and IO togheter
#elif HAS_SPI_RAM
#error Cannot use SPI RAM and IO togheter
#endif
#endif
