#include "pch.h"
#include "hardware/cm1602.h"
#include "hardware/spiram.h"
#include "hardware/spi.h"
#include "hardware/vs1011e.h"
#include "hardware/tick.h"
#include "ip_client.h"
#include "appio.h"
#include "audioSink.h"
#include "hardware/rs485.h"

#ifdef HAS_CM1602
static const char* msg1 = "Hi world! ";
#endif

void interrupt PRIO_TYPE low_isr(void)
{
    // Update tick timers at ~Khz freq
    TickUpdate();
#ifdef HAS_RS485
    rs485_interrupt();
#endif
}

void main()
{
#ifdef HAS_CM1602
    const char* errMsg;
#endif
   
    // Analyze RESET reason
    sys_storeResetReason();

    // Init Ticks on timer0 (low prio) module
    timers_init();

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
        cm1602_writeStr(s_lastErr);
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
    
#ifdef HAS_CM1602
    wait1s();
    clearlnUp();
#endif
    
    enableInterrupts();

    prot_init();

#ifdef HAS_RS485
    rs485_init();
#endif
        
    // I'm alive
    while (1) {   
        prot_poll();
        rs485_poll();
            
#if HAS_VS1011
        audio_pollMp3Player();
#endif
        CLRWDT();
    }
}

