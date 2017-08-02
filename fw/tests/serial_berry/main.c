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
    REG_DR = 0,
    REG_RSRECR = 1,
    REG_FR = 6,
    REG_IBRD = 9,
    REG_FBRD = 0xa,
    REG_LCRH = 0xb,
    REG_CR = 0xc,
    REG_IFLS = 0xd,
    REG_IMSC = 0xe,
    REG_RIS = 0xf,
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

static void* mmap_vmap;
static uint32_t mmap_size;
static int mmap_fdMem;
static volatile uint32_t* mmap_ptr;

static void mmap_create(uint32_t base, uint32_t len) {
    uint32_t page_size = 0x4000;//getpagesize();
    
    // Round first byte to page boundary
    uint32_t addr0 = (base / page_size) * page_size;
    uint32_t offs = base - addr0;
    // Round last byte to page boundary
    uint32_t addr1 = ((base + len - 1) / page_size) * page_size;
    mmap_size = (addr1 - addr0) + page_size;
 
    mmap_fdMem = open("/dev/mem", O_RDWR | O_SYNC);
    if (mmap_fdMem < 0) {
       fatal("Mem open");
    }
    char* mmap_vmap = mmap(NULL, mmap_size, PROT_READ | PROT_WRITE | PROT_EXEC, MAP_SHARED | MAP_LOCKED, mmap_fdMem, addr0);
    if (mmap_vmap == MAP_FAILED) { 
       fatal("Mem map");
    }
    mmap_ptr = (uint32_t*)(mmap_vmap + offs);
}

static void mmap_destroy() {
    if (munmap(mmap_vmap, mmap_size) != 0) {
       fatal("Mem unmap");
    }
    if (close(mmap_fdMem) != 0) {
       fatal("Mem close");
    }
}

static void mmap_wr(int regaddr, uint32_t value) {
    mmap_ptr[regaddr] = value;
}

static uint32_t mmap_rd(int regaddr) {
    return mmap_ptr[regaddr];
}

uint32_t ibrd, fbrd;

static void init(uint32_t parity) {
    // 1. Disable UART
    mmap_wr(REG_CR, 0);
    // 2. Wait for the end of transmission or reception of the current character
    while (mmap_rd(REG_FR) & REG_FR_BUSY);
    // 3. Flush the transmit FIFO by setting the FEN bit to 0 in the Line Control Register, UART_LCRH
    mmap_wr(REG_LCRH, 0);
    // 4. Reprogram the Control Register, UART_LCR, writing xBRD registers and the LCRH at the end (strobe)
    mmap_wr(REG_IBRD, ibrd);
    mmap_wr(REG_FBRD, fbrd);
    mmap_wr(REG_LCRH, REG_LCRH_SPS | REG_LCRH_WLEN8 | REG_LCRH_FEN | parity | REG_LCRH_PEN);
    // 5. Enable the UART.
    mmap_wr(REG_CR, REG_CR_TXE | REG_CR_UARTEN);
}

int main() {
    const uint32_t pi_peri_phys = 0x20000000;
    const uint32_t UART_BASE = (pi_peri_phys + 0x00201000);
    const uint32_t UART_LEN = 0x90;

    mmap_create(UART_BASE, UART_LEN);

    double fb = 48000000.0 / (16 * 19200.0);
    ibrd = (uint32_t)floor(fb);
    fbrd = (uint32_t)(fmod(fb, 1.0) * 64);

    // Disable interrupts
    mmap_wr(REG_IMSC, 0);
    mmap_wr(REG_ICR, 0);
    
    init(REG_LCRH_EPS);
    
    // Writes 0x1
    mmap_wr(REG_DR, 1);

    init(0);

    // Writes 0x1
    mmap_wr(REG_DR, 1);
    
    return 0;
}

