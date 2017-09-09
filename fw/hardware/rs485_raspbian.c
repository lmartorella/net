#include "../pch.h"
#include "rs485.h"

#include <stdlib.h>
#include <stdio.h>
#include <stdint.h>
#include <string.h>
#include <unistd.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <sys/mman.h>
#include <errno.h> 
#include <math.h>

typedef enum {
    UART_REG_DR = 0x00,
    UART_REG_RSRECR = 0x01,
    UART_REG_FR = 0x06,
    UART_REG_IBRD = 0x09,
    UART_REG_FBRD = 0x0a,
    UART_REG_LCRH = 0x0b,
    UART_REG_CR = 0x0c,
    //UART_REG_IFLS = 0x0d,
    //UART_REG_IMSC = 0x0e,
    //UART_REG_RIS = 0x0f,
    //UART_REG_MIS = 0x10,
    //UART_REG_ICR = 0x11,
    //UART_REG_DMACR = 0x12
} UART_REGS;

typedef enum {
    UART_REG_FR_TXFE = 0x80,
    UART_REG_FR_RXFF = 0x40,
    UART_REG_FR_TXFF = 0x20,
    UART_REG_FR_RXFE = 0x10,
    UART_REG_FR_BUSY = 0x08
} UART_REG_FR_BITS;

typedef enum {
    // Sticky parity
    UART_REG_LCRH_SPS = 0x80,
    UART_REG_LCRH_WLEN8 = 0x60,
    UART_REG_LCRH_FEN = 0x10,
    UART_REG_LCRH_EPS = 0x04,
    UART_REG_LCRH_PEN = 0x02,
            
    UART_REG_LCRH_SPS_1 = (UART_REG_LCRH_SPS | 0),
    UART_REG_LCRH_SPS_0 = (UART_REG_LCRH_SPS | UART_REG_LCRH_EPS)
} UART_REG_LCRH_BITS;

typedef enum {
    UART_REG_CR_RXE = 0x200,
    UART_REG_CR_TXE = 0x100,
    UART_REG_CR_UARTEN = 0x01
} UART_REG_CR_BITS;

typedef enum {
    UART_REG_RD_FE = 0x100,
    UART_REG_RD_PE = 0x200,
    UART_REG_RD_BE = 0x400,
    UART_REG_RD_OE = 0x800
} UART_REG_RD_BITS;

typedef enum {
    GPIO_MODE_INPUT = 0,
    GPIO_MODE_OUTPUT = 1
} GPIO_MODE;

typedef enum {
    GPIO_REG_GPFSEL0 = 0,
    GPIO_REG_GPSET0 = 7,
    GPIO_REG_GPCLR0 = 10
} GPIO_REGS;

typedef struct {
    void* vmap;
    uint32_t size;
    int fdMem;
    volatile uint32_t* ptr;
} Mmap;

static Mmap* mmap_create(uint32_t base, uint32_t len) {
    Mmap* ret = (Mmap*)malloc(sizeof(Mmap));
    uint32_t page_size = 0x4000;//getpagesize();
    
    // Round first byte to page boundary
    uint32_t addr0 = (base / page_size) * page_size;
    uint32_t offs = base - addr0;
    // Round last byte to page boundary
    uint32_t addr1 = ((base + len - 1) / page_size) * page_size;
    ret->size = (addr1 - addr0) + page_size;
 
    ret->fdMem = open("/dev/mem", O_RDWR | O_SYNC);
    if (ret->fdMem < 0) {
       fatal("Mem open");
    }
    char* mmap_vmap = mmap(NULL, ret->size, PROT_READ | PROT_WRITE | PROT_EXEC, MAP_SHARED | MAP_LOCKED, ret->fdMem, addr0);
    if (mmap_vmap == MAP_FAILED) { 
       fatal("Mem map");
    }
    ret->ptr = (uint32_t*)(mmap_vmap + offs);
    return ret;
}

static void mmap_destroy(Mmap* map) {
    if (munmap(map->vmap, map->size) != 0) {
       fatal("Mem unmap");
    }
    if (close(map->fdMem) != 0) {
       fatal("Mem close");
    }
    free(map);
}

static void mmap_wr(const Mmap* map, int regaddr, uint32_t value) {
    map->ptr[regaddr] = value;
}

static uint32_t mmap_rd(const Mmap* map, int regaddr) {
    return map->ptr[regaddr];
}

static void gpio_setMode(Mmap* map, unsigned gpio, GPIO_MODE mode)
{
   int reg = GPIO_REG_GPFSEL0 + (gpio / 10);
   int shift = (gpio % 10) * 3;
   mmap_wr(map, reg, (map->ptr[reg] & ~(7 << shift)) | (mode << shift));
}

static void gpio_write(Mmap* map, unsigned gpio, unsigned value)
{
    int bank = gpio >> 5;
    int bit = (1 << (gpio & 0x1F));
    if (!value) {
        mmap_wr(map, GPIO_REG_GPCLR0 + bank, bit);
    }
    else {
        mmap_wr(map, GPIO_REG_GPSET0 + bank, bit);
    }
}

static void uart_init(Mmap* map, int baud, uint32_t parity) {
    double fb = 48000000.0 / (16.0 * baud);
    uint32_t ibrd = (uint32_t)floor(fb);
    uint32_t fbrd = (uint32_t)(fmod(fb, 1.0) * 64);

    // 1. Disable UART
    mmap_wr(map, UART_REG_CR, 0);
    // 2. Wait for the end of transmission or reception of the current character
    while (mmap_rd(map, UART_REG_FR) & UART_REG_FR_BUSY);
    // 3. Flush the transmit FIFO by setting the FEN bit to 0 in the Line Control Register, UART_LCRH
    mmap_wr(map, UART_REG_LCRH, 0);
    // 4. Reprogram the Control Register, UART_LCR, writing xBRD registers and the LCRH at the end (strobe)
    mmap_wr(map, UART_REG_IBRD, ibrd);
    mmap_wr(map, UART_REG_FBRD, fbrd);
    mmap_wr(map, UART_REG_LCRH, UART_REG_LCRH_WLEN8 | UART_REG_LCRH_FEN | parity | UART_REG_LCRH_PEN);
    // 5. Enable the UART.
    mmap_wr(map, UART_REG_CR, UART_REG_CR_TXE | UART_REG_CR_RXE | UART_REG_CR_UARTEN);
}

#define US_PER_BYTE (1000000ul / BAUD * 11)
// time to wait before engaging the channel (after other station finished to transmit)
#define ENGAGE_CHANNEL_TIMEOUT (US_PER_BYTE * 1)  
// additional time to wait after channel engaged to start transmit
// Consider that the glitch produced engaging the channel can be observed as a FRAMEERR by other stations
// So use a long time here to avoid FERR to consume valid data
#define START_TRANSMIT_TIMEOUT (US_PER_BYTE * 3)
// time to wait before releasing the channel = 2 bytes,
// but let's wait an additional byte since USART is free when still transmitting the last byte.
#define DISENGAGE_CHANNEL_TIMEOUT (US_PER_BYTE * (2 + 1))

bit rs485_over;
bit rs485_close;
bit rs485_master;
bit rs485_lastRc9;
bit rs485_skipData;
RS485_STATE rs485_state;

void rs485_init() {
    
}

void rs485_poll() {
    
}

void rs485_write(BOOL address, const BYTE* data, BYTE size) {
    
}

bit rs485_read(BYTE* data, BYTE size) {
    return 0;
}

BYTE rs485_readAvail() {
    return 0;
}

BYTE rs485_writeAvail() {
    return 0;
}

void rs485_waitDisengageTime() {
}
