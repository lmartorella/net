#include "../hardware/fuses.h"
#include "../hardware/utilities.h"
#include "../hardware/cm1602.h"
#include "../hardware/spiram.h"
#include "../hardware/spi.h"
#include <stdio.h>

static void _clr(BYTE addr)
{
	char i;
	cm1602_setDdramAddr(addr);
	for (i = 0; i < 16; i++)
	{
		cm1602_write(' ');
	}
	cm1602_setDdramAddr(addr);
}

static void _print(const char* str, BYTE addr)
{
	_clr(addr);
	cm1602_writeStr(str);
	ClrWdt();
}

static void reset()
{
    // reset display
    cm1602_reset();
    cm1602_clear();
    cm1602_setEntryMode(MODE_INCREMENT | MODE_SHIFTOFF);
    cm1602_enable(ENABLE_DISPLAY | ENABLE_CURSOR | ENABLE_CURSORBLINK);
}

// Test all 4 banks, return the ADDR of the failing test
// or -1 if no fails
void _sram_test(void)
{
    char msg[16];
	// To use a pseudo-random seq string that spans on all banks
	// write first, read then
	BYTE b, br;
	UINT32 addr;
        BYTE i1, i2, i3;

        // Write all
        _clr(0x40);
        addr = 0x0;
        b = 0;
        for (i1 = 0; i1 < 2; i1++)
        {
            i2 = 0;
            do
            {
                i3 = 0;
                do
                {
                    b += 251; // largest prime < 256
                    sram_write(&b, addr, 1);
                    addr++;
                    i3++;
                } while (i3 != 0);
                i2++;

                if ((i2 % 0x20) == 0)
                {
                    cm1602_write('.');
                }

            } while (i2 != 0);
        }

        // READ all
        _clr(0x40);
        addr = 0x0;
        b = 0;
        for (i1 = 0; i1 < 2; i1++)
        {
            i2 = 0;
            do
            {
                i3 = 0;
                do
                {
                    b += 251; // largest prime < 256
                    sram_read(&br, addr, 1);
                    if (br != b)
                    {
                        sprintf(msg, "FAIL #%8lX", addr);
                        _print(msg, 0x0);
                        sprintf(msg, "EXP: %2X, RD: %2X", (int)b, (int)br);
                        _print(msg, 0x40);
                        return;
                    }
                    addr++;
                    i3++;
                } while (i3 != 0);
                i2++;

                if ((i2 % 0x20) == 0)
                {
                    cm1602_write('%');
                }

            } while (i2 != 0);
        }

    _print("PASS", 0x40);
}

void main()
{
    reset();

    // Write #1/#2 on both rows
    _print("Running test...", 0);

    // Enable SPI
    // from 23k256 datasheet and figure 20.3 of PIC datasheet
    // CKP = 0, CKE = 1
    // Output: data sampled at clock falling.
    // Input: data sampled at clock falling, at the end of the cycle.
    spi_init(SPI_SMP_MIDDLE | SPI_CKE_IDLE | SPI_CKP_LOW | SPI_SSPM_CLK_F16);

    sram_init();

    _sram_test();

    while (1) CLRWDT();
}
