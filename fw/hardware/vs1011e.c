#include "vs1011e.h"
#include "utilities.h"
#include "spi.h"

typedef enum
{
    SCI_MODE = 0x0,
    SCI_STATUS = 0x1,
    SCI_BASS = 0x2,
    SCI_CLOCKF = 0x3,
    SCI_DECODE_TIME = 0x4,
    SCI_AUDATA = 0x5,
    SCI_WRAM = 0x6,
    SCI_WRAMADDR = 0x7,
    SCI_HDAT0 = 0x8,
    SCI_HDAT1 = 0x9,
    SCI_AIADRR = 0xa,
    SCI_VOL = 0xb,
    SCI_AICTRL0 = 0xc,
    SCI_AICTRL1 = 0xd,
    SCI_AICTRL2 = 0xe,
    SCI_AICTRL3 = 0xf,
} SCI_COMMAND;

typedef enum
{
    SM_DIFF_NORMAL = 0,
    SM_DIFF_INVERTED = 1,
    SM_LAYER12_ALLOW = 2,
    SM_RESET = 4,
    SM_OUTOFWAV = 8,
    SM_TEST_ENABLE = 0x20,
    SM_STREAM_ENABLE = 0x40,
    SM_DACT_RISING = 0,
    SM_DACT_FALLING = 0x100,
    SM_SDIORD_MSBFIRST = 0,
    SM_SDIORD_LSBFIRST = 0x200,
    SM_SDISHARE_ENABLE = 0x400,
    SM_SDINEW_ENABLE = 0x800
} SCI_MODE_BITS;

typedef enum
{
    SCI_READ = 0x3,
    SCI_WRITE = 0x2
} SPI_COMMAND;

static void _writeCommand(SCI_COMMAND addr, UINT16 command)
{
    VS1011_PORTBITS.VS1011_XCS = 0;
    
    spi_shift(SCI_WRITE); //8
    spi_shift(addr);      //8
    spi_shift16(command);   //16

    VS1011_PORTBITS.VS1011_XCS = 1;
    // Wait for command exec
    while (!VS1011_PORTBITS.VS1011_DREQ);
}

void vs1011_init(void)
{
    // Reset the device
    VS1011_PORTBITS.VS1011_RESET = 0;
    VS1011_TRISBITS.VS1011_RESET = 0;

    // enable cs out and deassert
    VS1011_PORTBITS.VS1011_XCS = 1;
    VS1011_PORTBITS.VS1011_XDCS = 1;
    VS1011_TRISBITS.VS1011_XCS = 0;
    VS1011_TRISBITS.VS1011_XDCS = 0;

    // Maintain the RESET for 2 XTALI
    wait40us();

    // Deassert RESET
    VS1011_PORTBITS.VS1011_RESET = 1;

    // Now wait for 50000 XTALI = ~2000us < WDT
    while (!VS1011_PORTBITS.VS1011_DREQ);

    // Now set native mode
    UINT16 mode = SM_DIFF_NORMAL | SM_LAYER12_ALLOW | SM_STREAM_ENABLE | SM_SDINEW_ENABLE
            | SM_DACT_RISING | SM_SDIORD_MSBFIRST;

    _writeCommand(SCI_MODE, mode);
}