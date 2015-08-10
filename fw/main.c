#include "hardware/fuses.h"
#include "hardware/utilities.h"
#include "hardware/cm1602.h"
#include "hardware/spiram.h"
#include "hardware/spi.h"
#include "hardware/vs1011e.h"
#include "hardware/ip.h"
#include "protocol.h"
#include "timers.h"
#include "appio.h"
#include "audioSink.h"
#include <stdio.h>

static const char* msg1 = "Hi world! ";

static void enableInterrupts(void)
{
	// Enable low/high interrupt mode
	RCONbits.IPEN = 1;		
	INTCONbits.GIEL = 1;
	INTCONbits.GIEH = 1;
}

void main()
{
    const char* errMsg;
    
    // Analyze RESET reason
    sys_storeResetReason();

    // reset display
    cm1602_reset();
    cm1602_clear();
    cm1602_setEntryMode(MODE_INCREMENT | MODE_SHIFTOFF);
    cm1602_enable(ENABLE_DISPLAY | ENABLE_CURSOR | ENABLE_CURSORBLINK);

    cm1602_setDdramAddr(0);
    cm1602_writeStr(msg1);
    errMsg = sys_getResetReasonStr();
    cm1602_writeStr(errMsg);
    if (sys_isResetReasonExc())
    {
        cm1602_setDdramAddr(0x40);
        cm1602_writeStr(sys_getLastFatal());
    }

    wait1s();
    println("Spi");

    // Enable SPI
    // from 23k256 datasheet and figure 20.3 of PIC datasheet
    // CKP = 0, CKE = 1
    // Output: data changed at clock falling.
    // Input: data sampled at clock rising.
    spi_init(SPI_SMP_END | SPI_CKE_IDLE | SPI_CKP_LOW | SPI_SSPM_CLK_F4);

    sram_init();
    vs1011_init();

    wait1s();
    clearlnUp();
    
    enableInterrupts();
    timers_init();

    ip_prot_init();
    
    // I'm alive
    while (1)
    {
            TIMER_RES timers = timers_check();

            if (timers.timer_10ms)
            {
               ip_prot_poll();
            }

            if (timers.timer_1s)
            {
                ip_prot_slowTimer();
            }

#if HAS_VS1011
            audio_pollMp3Player();
#endif
            ClrWdt();
    }
}

