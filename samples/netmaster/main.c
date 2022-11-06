#include <net/net.h>

#ifdef DEBUG
// 17008 is the debug port
#define SERVER_CONTROL_UDP_PORT 17008
#else
// 17007 is the release port
#define SERVER_CONTROL_UDP_PORT 17007
#endif

void main()
{
    net_init(SERVER_CONTROL_UDP_PORT);

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
