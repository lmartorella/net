#ifndef IP_RASPBIAN_H
#define IP_RASPBIAN_H

typedef WORD UDP_PORT;
typedef BYTE UDP_SOCKET;
typedef BYTE TCP_SOCKET;

typedef struct {
    DWORD MyIPAddr;
} AppConfig_t;
extern AppConfig_t AppConfig;

void TCPDiscard(TCP_SOCKET socket);
void TCPDisconnect(TCP_SOCKET socket);
WORD TCPIsGetReady(TCP_SOCKET socket);
BOOL TCPIsConnected(TCP_SOCKET socket);
void TCPFlush(TCP_SOCKET socket);
void TCPGetArray(TCP_SOCKET socket, BYTE* buf, WORD size);
void TCPPutArray(TCP_SOCKET socket, const BYTE* buf, WORD size);

UDP_SOCKET UDPOpenEx(DWORD remoteHost, BYTE remoteHostType, UDP_PORT localPort, UDP_PORT remotePort);
#define UDP_OPEN_NODE_INFO	4u
#define INVALID_UDP_SOCKET      (0xffu)		// Indicates a UDP socket that is not valid
WORD UDPIsPutReady(UDP_SOCKET s);
BYTE* UDPPutString(const BYTE *strData);
WORD UDPPutArray(const BYTE *cData, WORD wDataLen);
BOOL UDPPutW(WORD w);
void UDPFlush(void);

TCP_SOCKET TCPOpen(DWORD dwRemoteHost, BYTE vRemoteHostType, WORD wPort, BYTE vSocketPurpose);
#define TCP_OPEN_SERVER		0u
#define TCP_PURPOSE_GENERIC_TCP_SERVER 1
#define INVALID_SOCKET      (0xFE)	// The socket is invalid or could not be opened

BOOL DHCPIsBound(BYTE vInterface);

#endif /* IP_RASPBIAN_H */

