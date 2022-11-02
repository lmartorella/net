#include <net/net.h>

void main()
{
    // Analyze RESET reason
    sys_storeResetReason();

    // Init Ticks on timer0 (low prio) module
    timers_init();
    io_init();
    led_init();

    pers_load();

    bus_prim_init();
    prot_init();
    rs485_init();
                
    sinks_init();
        
    sys_enableInterrupts();

    // I'm alive
    while (true) {
        // If something requires strict polling, uses 0.3ms polling for bus operations,
        // otherwise rest the CPU using 0.1 sec polling
        usleep(300);
        rs485_interrupt();
        
        _Bool active = bus_prim_poll();
        active = prot_prim_poll() || active;
        active = rs485_poll() || active;

        active = sinks_poll() || active;
        
        active = pers_poll() || active;

        if (!active) {
            ip_waitEvent();
        }
    }
}
