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
#include "hardware/dht11.h"

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
    // Analyze RESET reason
    sys_storeResetReason();

    // Init Ticks on timer0 (low prio) module
    timers_init();
    appio_init();

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

#ifdef HAS_DHT11
    dht11_init();
#endif

#ifdef HAS_DIGIO
    digio_init();
#endif

    enableInterrupts();

#ifdef HAS_RS485
    rs485_init();
#endif

    prot_init();
        
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

