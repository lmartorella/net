#include "pch.h"
#include "hardware/cm1602.h"
#include "hardware/spiram.h"
#include "hardware/spi.h"
#include "hardware/vs1011e.h"
#include "hardware/tick.h"
#include "ip_protocol.h"
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

#ifdef MINIBEAN_TEST_APP
static int s_led = 0;
static BOOL s_rc9;
static int s_size;
static void led_check_off() {
    if ((++s_led) > 10){
        PORTBbits.RB0 = 0;
    }
}
static void led_on() {
    s_led = 0;
    PORTBbits.RB0 = 1;
}
static void led_init() {
    TRISBbits.TRISB0 = 0;
}
#endif



void main()
{
#ifdef HAS_CM1602
    const char* errMsg;
#endif
   
    // Analyze RESET reason
    sys_storeResetReason();

    // Init Ticks on timer0 (low prio) module
    TickInit();

#ifdef MINIBEAN_TEST_APP
    led_init();
#endif    

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
    
#ifdef HAS_CM1602
    wait1s();
    clearlnUp();
#endif
    
    enableInterrupts();
    
#ifdef HAS_IP
    ip_prot_init();
#endif

#ifdef HAS_RS485
    rs485_init();
#endif
    
#ifdef MCU_TEST_APP
    rs485_startRead();
#endif
    
    // I'm alive
    while (1) {   
        TIMER_RES timers = timers_check();
        if (timers.timer_10ms) {
#ifdef HAS_IP
           ip_prot_poll();
#endif          
           
#ifdef MINIBEAN_TEST_APP
           // Wait 100ms to shut down the led
           led_check_off();
#endif
        }
        
        if (timers.timer_1ms){
#ifdef HAS_RS485
           rs485_poll();
#endif
        }
        
        if (timers.timer_1s)
        {
#ifdef MCU_TEST_APP
            int size;
            BOOL rc9;
            char msg[16];
            BYTE* data;
            if (rs485_getError()) {
                println("Rc err");
                rs485_startRead();
            }
            else {
                data = rs485_read(&size, &rc9);
                if (data) {
                    strncpy(msg + 1, data, size);
                    msg[size + 1] = 0;
                    msg[0] = rc9 ? ':' : '.';
                    println(msg);
                }
            }
#endif

#ifdef HAS_IP
            ip_prot_slowTimer();
#endif

#ifdef MINIBEAN_TEST_APP
            static BOOL b9 = FALSE;
            rs485_write(b9, "Hello!", 6);
            b9 = !b9;
            led_on();
#endif
        }
            
#if HAS_VS1011
        audio_pollMp3Player();
#endif
        CLRWDT();
    }
}

