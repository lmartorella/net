#ifndef FUSES_RASPBIAN_H
#define FUSES_RASPBIAN_H

// Define IP and protocol
#define HAS_IP
#define HAS_BUS

// But not rs485
#undef HAS_RS485

#define SERVER_CONTROL_UDP_PORT 17008
#define CLIENT_TCP_PORT 20000


typedef DWORD TICK_TYPE;
// Using gettime
#define TICKS_PER_SECOND (1000000u)

void CLRWDT();
void enableInterrupts();
void rom_poll();
void fatal(const char* str);

#endif /* FUSES_RASPBIAN_H */

