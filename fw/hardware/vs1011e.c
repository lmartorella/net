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

static const BYTE TEST_MEM[] = { 0x4d, 0xea, 0x6d, 0x54 };
static const BYTE TEST_SINE[] = { 0x53, 0xef, 0x6e };

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

static UINT16 _readCommand(SCI_COMMAND addr)
{
    VS1011_PORTBITS.VS1011_XCS = 0;

    spi_shift(SCI_READ); //8
    spi_shift(addr);      //8
    UINT16 ret = spi_shift16(0);   //16

    VS1011_PORTBITS.VS1011_XCS = 1;
    // Wait for command exec
    //while (!VS1011_PORTBITS.VS1011_DREQ);
    return ret;
}

static void _reset()
{
    VS1011_PORTBITS.VS1011_RESET = 0;
    // Maintain the RESET for 2 XTALI
    wait40us();

    // Deassert RESET
    VS1011_PORTBITS.VS1011_RESET = 1;

    // Now wait for 50000 XTALI = ~2000us < WDT
    while (!VS1011_PORTBITS.VS1011_DREQ);
}

static void _enterTestMode()
{
    CLRWDT();
    _reset();
    UINT16 mode = SM_TEST_ENABLE | SM_SDINEW_ENABLE | SM_DACT_RISING | SM_SDIORD_MSBFIRST;
    _writeCommand(SCI_MODE, mode);
}

static BOOL _memoryTest()
{
    _enterTestMode();

    VS1011_PORTBITS.VS1011_XDCS = 0;
    spi_shift_array(TEST_MEM, sizeof(TEST_MEM));
    spi_shift_repeat(0, 4);
    VS1011_PORTBITS.VS1011_XDCS = 1;

    // The test take 200.000 CLKI = ~8ms
    UINT16 hwRes;
    do
    {
        hwRes = _readCommand(SCI_HDAT0);
    } while (!(hwRes & 0x8000));

    CLRWDT();
    return hwRes == 0x807f;
}

void vs1011_sineTest(UINT16 freq)
{
    UINT32 fk = freq * 128ul / 48000ul;
    BYTE cmd = (1 << 5) | (((BYTE)fk) & 0x1f);
    _enterTestMode();

    VS1011_PORTBITS.VS1011_XDCS = 0;
    spi_shift_array(TEST_SINE, sizeof(TEST_SINE));
    spi_shift(cmd);
    spi_shift_repeat(0, 4);
    VS1011_PORTBITS.VS1011_XDCS = 1;
}

void vs1011_volume(BYTE left, BYTE right)
{
    if (left == 0xff) left = 0xfe;
    if (right == 0xff) right = 0xfe;
    _writeCommand(SCI_VOL, (left << 8) + right);
}

VS1011_MODEL vs1011_init(void)
{
    // Reset the device
    VS1011_PORTBITS.VS1011_RESET = 1;
    VS1011_TRISBITS.VS1011_RESET = 0;

    // enable cs out and deassert
    VS1011_PORTBITS.VS1011_XCS = 1;
    VS1011_PORTBITS.VS1011_XDCS = 1;
    VS1011_TRISBITS.VS1011_XCS = 0;
    VS1011_TRISBITS.VS1011_XDCS = 0;

    // Start SDI tests
    if (!_memoryTest())
    {
        return VS1011_MODEL_HWFAIL;
    }

    // Now set native mode
    _reset();
    UINT16 mode = SM_DIFF_NORMAL | SM_LAYER12_ALLOW | SM_STREAM_ENABLE | SM_SDINEW_ENABLE
            | SM_DACT_RISING | SM_SDIORD_MSBFIRST;
    _writeCommand(SCI_MODE, mode);

    // Now change the XTALI frequency to the one set in fuses
    UINT16 frq = VS1011_XTALI / 2 + (VS1011_CLK_DOUBLING * 0x8000);
    _writeCommand(SCI_CLOCKF, frq);

    // Ask version
    UINT16 v = _readCommand(SCI_STATUS);
    return (VS1011_MODEL)((v >> 4) & 0x7);
}