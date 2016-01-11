#include "pch.h"
#include "hardware/cm1602.h"
#include "hardware/spiram.h"
#include "hardware/spi.h"
#include "hardware/vs1011e.h"
#include "ip_protocol.h"
#include "appio.h"
#include "audioSink.h"

static const char* msg1 = "Hi world! ";

void interrupt low_priority low_isr(void)
{
    ip_prot_tickUpdate();
}

void main()
{
    const char* errMsg;
    
    // Analyze RESET reason
    sys_storeResetReason();

#ifdef HAS_CM1602
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
#endif

#ifdef HAS_SPI
    println("Spi");
    
    // Enable SPI
    // from 23k256 datasheet and figure 20.3 of PIC datasheet
    // CKP = 0, CKE = 1
    // Output: data changed at clock falling.
    // Input: data sampled at clock rising.
    spi_init(SPI_SMP_END | SPI_CKE_IDLE | SPI_CKP_LOW | SPI_SSPM_CLK_F4);
#endif
    
#ifdef HAS_SPI_RAM
    sram_init();
#endif
    
#ifdef HAS_VS1011
    vs1011_init();
#endif
    
    wait1s();
    clearlnUp();
    
    enableInterrupts();
    
#ifdef HAS_IP
    ip_prot_init();
#endif
    
    // I'm alive
    while (1)
    {
#ifdef HAS_IP
            TIMER_RES timers = ip_timers_check();
            if (timers.timer_10ms)
            {
               ip_prot_poll();
            }
            if (timers.timer_1s)
            {
                ip_prot_slowTimer();
            }
#endif
            
#if HAS_VS1011
            audio_pollMp3Player();
#endif
            CLRWDT();
    }
}

