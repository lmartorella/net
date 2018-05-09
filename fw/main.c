#include "pch.h"
#include "ip_client.h"
#include "appio.h"
#include "persistence.h"
#include "protocol.h"
#include "apps/apps.h"

#ifdef __XC8
void interrupt PRIO_TYPE low_isr()
{
    // Update tick timers at ~Khz freq
    TickUpdate();
#ifdef HAS_RS485
    rs485_interrupt();
#endif
}
#endif

void main()
{
    // Analyze RESET reason
    sys_storeResetReason();

#ifdef HAS_MAX232_SOFTWARE
    max232_init();
#endif

    // Init Ticks on timer0 (low prio) module
    timers_init();
    appio_init();

    pers_init();

#if defined(HAS_SPI) && defined(HAS_I2C)
#error Cannot enable both SPI and I2C
#endif
    
#ifdef HAS_SPI
    println("Spi");
    
    // Enable SPI
    // from 23k256 datasheet and figure 20.3 of PIC datasheet
    // CKP = 0, CKE = 1
    // Output: data changed at clock falling.
    // Input: data sampled at clock rising.
    spi_init(SPI_SMP_END | SPI_CKE_IDLE | SPI_CKP_LOW | SPI_SSPM_CLK_F4);
#elif defined(HAS_I2C)
    i2c_init();
#endif
    
#ifdef HAS_SPI_RAM
    sram_init();
#endif
    
#ifdef HAS_DHT11
    dht11_init();
#endif

#ifdef HAS_DIGIO
    digio_init();
#endif

#ifdef BUSPOWER_PORT
    // Enable bus power to slaves
    BUSPOWER_TRIS = 0;
    BUSPOWER_PORT = 1;
#endif
    
#ifdef HAS_RS485
    rs485_init();
#endif

#ifdef HAS_BUS
    prot_init();
#endif
    
    apps_init();
    
    enableInterrupts();

    // I'm alive
    while (1) {   
        CLRWDT();
        
#ifdef _CONF_RASPBIAN
        usleep(300);
        rs485_interrupt();
#endif
        
#if defined(HAS_BUS_CLIENT) || defined(HAS_BUS_SERVER)
        bus_poll();
#endif
#ifdef HAS_BUS
        prot_poll();
#endif
#ifdef HAS_RS485
        rs485_poll();
#endif
        pers_poll();
        
        apps_poll();
    }
}

