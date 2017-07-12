#ifndef FUSES_RASPBIAN_H
#define FUSES_RASPBIAN_H

#define HAS_IP
#define HAS_BUS
#define HAS_RS485
#define RS485_BAUD 19200
#define RS485_BUF_SIZE 128

#define SERVER_CONTROL_UDP_PORT 17007
#define CLIENT_TCP_PORT 20000


typedef DWORD TICK_TYPE;
#define TICKS_PER_SECOND (4000000u)

void CLRWDT();
void enableInterrupts();
void rom_poll();

#define StackTask()
#define StackApplications()

#define fatal(str) fprintf(stderr, "%s", str);exit(1);

#endif /* FUSES_RASPBIAN_H */

