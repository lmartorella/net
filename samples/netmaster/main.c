#include <net/net.h>

void main()
{
    net_init();

    // I'm alive
    while (true) {
        // If something requires strict polling, uses 0.3ms polling for bus operations,
        // otherwise rest the CPU using 0.1 sec polling
        usleep(300);
        rs485_interrupt();
        
        _Bool active = net_poll();

        if (!active) {
            ip_waitEvent();
        }
    }
}
