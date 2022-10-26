#include "../../src/nodes/pch.h"
#include "../../src/nodes/ip_client.h"
#include "../../src/nodes/appio.h"
#include "../../src/nodes/persistence.h"
#include "../../src/nodes/protocol.h"
#include "../../src/nodes/sinks.h"
#include "../../src/nodes/bus_primary.h"
#include "../../src/nodes/rs485.h"
#include "../../src/nodes/timers.h"

void main()
{
    // Analyze RESET reason
    sys_storeResetReason();

    // Init Ticks on timer0 (low prio) module
    timers_init();
    io_init();

    pers_load();

    prot_init();
    rs485_init();
                
    sinks_init();
        
    enableInterrupts();

    // I'm alive
    while (1) {   
        CLRWDT();

        usleep(300);
        rs485_interrupt();
        
        bus_prim_poll();
        prot_poll();
        rs485_poll();

        sinks_poll();
        
        pers_poll();
    }
}
