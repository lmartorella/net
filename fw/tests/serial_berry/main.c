#define _GNU_SOURCE

#include <stdlib.h>
#include <stdio.h>
#include <stdint.h>
#include <string.h>
#include <unistd.h>
#include <math.h>
#include <sys/types.h>
#include <sys/stat.h>
#include <fcntl.h>
#include <sys/types.h>
#include <sys/mman.h>
#include <errno.h> 

enum {
    REG_DR = 0x00,
    REG_RSRECR = 0x01,
    REG_FR = 0x06,
    REG_IBRD = 0x09,
    REG_FBRD = 0x0a,
    REG_LCRH = 0x0b,
    REG_CR = 0x0c,
    REG_IFLS = 0x0d,
    REG_IMSC = 0x0e,
    REG_RIS = 0x0f,
    REG_MIS = 0x10,
    REG_ICR = 0x11,
    REG_DMACR = 0x12
};

#define REG_FR_TXFE 0x80
#define REG_FR_RXFF 0x40
#define REG_FR_TXFF 0x20
#define REG_FR_RXFE 0x10
#define REG_FR_BUSY 0x08

#define REG_LCRH_SPS   0x80
#define REG_LCRH_WLEN8   0x60
#define REG_LCRH_FEN   0x10
#define REG_LCRH_EPS   0x04
#define REG_LCRH_PEN   0x02

#define REG_CR_RXE   0x200
#define REG_CR_TXE   0x100
#define REG_CR_UARTEN   0x01


static void fatal(const char* err) {
    fprintf(stderr, "%s: %d\n", err, errno);
    exit(1);
}

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

static void uart_init(Mmap* map, int baud, uint32_t parity) {
    double fb = 48000000.0 / (16.0 * baud);
    uint32_t ibrd = (uint32_t)floor(fb);
    uint32_t fbrd = (uint32_t)(fmod(fb, 1.0) * 64);

    // 1. Disable UART
    mmap_wr(map, REG_CR, 0);
    // 2. Wait for the end of transmission or reception of the current character
    while (mmap_rd(map, REG_FR) & REG_FR_BUSY);
    // 3. Flush the transmit FIFO by setting the FEN bit to 0 in the Line Control Register, UART_LCRH
    mmap_wr(map, REG_LCRH, 0);
    // 4. Reprogram the Control Register, UART_LCR, writing xBRD registers and the LCRH at the end (strobe)
    mmap_wr(map, REG_IBRD, ibrd);
    mmap_wr(map, REG_FBRD, fbrd);
    mmap_wr(map, REG_LCRH, REG_LCRH_SPS | REG_LCRH_WLEN8 | REG_LCRH_FEN | parity | REG_LCRH_PEN);
    // 5. Enable the UART.
    mmap_wr(map, REG_CR, REG_CR_TXE | REG_CR_UARTEN);
}

typedef enum {
    GPIO_MODE_INPUT = 0,
    GPIO_MODE_OUTPUT = 1
} GPIO_MODE;

#define GPFSEL0 0
#define GPSET0 7
#define GPCLR0 10

static void gpio_setMode(Mmap* map, unsigned gpio, GPIO_MODE mode)
{
   int reg = GPFSEL0 + (gpio / 10);
   int shift = (gpio % 10) * 3;
   map->ptr[reg] = (map->ptr[reg] & ~(7 << shift)) | (mode << shift);
}

static void gpio_write(Mmap* map, unsigned gpio, unsigned value)
{
    int bank = gpio >> 5;
    int bit = (1 << (gpio & 0x1F));
    if (!value) {
        map->ptr[GPCLR0 + bank] = bit;
    }
    else {
        map->ptr[GPSET0 + bank] = bit;
    }
}

int main() {
    const uint32_t pi_peri_phys = 0x20000000;

    Mmap* uartMap = mmap_create((pi_peri_phys + 0x00201000), 0x90);
    Mmap* gpioMap = mmap_create((pi_peri_phys + 0x00200000), 0xB4);

    // Disable interrupts
    mmap_wr(uartMap, REG_IMSC, 0);
    mmap_wr(uartMap, REG_ICR, 0);
    
    uart_init(uartMap, 19200, REG_LCRH_EPS);
    
    // Enable write channel
    gpio_setMode(gpioMap, 2, GPIO_MODE_OUTPUT);
    gpio_write(gpioMap, 2, 1);
   
    // Writes packet
    mmap_wr(uartMap, REG_DR, 0x55);
    mmap_wr(uartMap, REG_DR, 0xAA);
    mmap_wr(uartMap, REG_DR, 0x01);
    mmap_wr(uartMap, REG_DR, 0x00);
    mmap_wr(uartMap, REG_DR, 0x33);

    printf("Packet with REG_LCRH_EPS written, 1 byte\n");
    
//    init(0);

    // Writes 0x1
//    mmap_wr(REG_DR, 1);
    
    mmap_destroy(uartMap);
    mmap_destroy(gpioMap);
    return 0;
}

