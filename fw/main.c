#include "pch.h"
#include "hardware/cm1602.h"
#include "hardware/spiram.h"
#include "hardware/spi.h"
#include "hardware/vs1011e.h"
#include "hardware/tick.h"
#include "hardware/max232.h"
#include "ip_client.h"
#include "appio.h"
#include "audioSink.h"
#include "hardware/rs485.h"
#include "hardware/dht11.h"
#include "hardware/digio.h"

void interrupt PRIO_TYPE low_isr(void)
{
    // Update tick timers at ~Khz freq
    TickUpdate();
#ifdef HAS_RS485
    rs485_interrupt();
#endif
}

static const char* g_resetReasonMsgs[] = { 
                "N/A",
				"POR",
				"BOR",
				"CFG",
				"WDT",
				"STK",
				"MCL",
				"EXC"  };

void main()
{
    // Analyze RESET reason
    sys_storeResetReason();

    max232_init();
    memcpy(max232_buffer1, g_resetReasonMsgs[g_resetReason], 4);
    max232_buffer1[3] = ' ';
    max232_sendReceive(4);

    max232_buffer1[0] = 'S';
    max232_buffer1[1] = 't';
    max232_buffer1[2] = 'i';
    max232_buffer1[3] = 'l';
    max232_buffer1[4] = 'l';
    max232_buffer1[5] = ' ';
    max232_buffer1[6] = 'a';
    max232_buffer1[7] = 'l';
    max232_buffer1[8] = 'i';
    max232_buffer1[9] = 'v';
    max232_buffer1[10] = 'e';
    max232_buffer1[11] = '.';
    max232_buffer1[12] = ' ';
    int l = 13;
    while (1) {
        l = max232_sendReceive(l);
    }   

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

#if defined(HAS_MAX232) || defined(HAS_MAX232_SOFTWARE)
    max232_init();
#endif

#ifdef BUSPOWER_PORT
    // Enable bus power to slaves
    BUSPOWER_TRIS = 0;
    BUSPOWER_PORT = 1;
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

