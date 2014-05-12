#include "../hardware/fuses.h"
#include "../hardware/utilities.h"
#include "../hardware/cm1602.h"
#include "../hardware/spi.h"
#include "../hardware/vs1011e.h"
#include "../appio.h"
#include <stdio.h>


void main()
{
    cm1602_reset();
    cm1602_clear();
    cm1602_setEntryMode(MODE_INCREMENT | MODE_SHIFTOFF);
    cm1602_enable(ENABLE_DISPLAY | ENABLE_CURSOR | ENABLE_CURSORBLINK);
    
    printlnUp("Starting MP3...");

    // Enable SPI
    // from 23k256 datasheet and figure 20.3 of PIC datasheet
    // CKP = 0, CKE = 1
    // Output: data changed at clock falling.
    // Input: data sampled at clock rising.
    spi_init(SPI_SMP_END | SPI_CKE_IDLE | SPI_CKP_LOW | SPI_SSPM_CLK_F4);

    // Init chip
    VS1011_MODEL model = vs1011_init();
    switch (model)
    {
        case VS1011_MODEL_VS1001:
            println("VS1001");
            break;
        case VS1011_MODEL_VS1011:
            println("VS1011");
            break;
        case VS1011_MODEL_VS1011E:
            println("VS1002/11E");
            break;
        case VS1011_MODEL_VS1003:
            println("VS1003");
            break;
        case VS1011_MODEL_HWFAIL:
            println("HW FAIL");
            break;
        default:
            println("UNKNOWN");
            break;
    }
    //println("OK");

    SLEEP();
}
