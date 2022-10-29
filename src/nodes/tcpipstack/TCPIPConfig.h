#ifndef TCPIPCONFIG_STUB_H
#define TCPIPCONFIG_STUB_H

#include "../pch.h"

// This file is requested by TCP/IP stack and usually
// generated by a GUI

#define STACK_USE_ICMP_SERVER			// Ping query and response capability
#define STACK_USE_ICMP_CLIENT			// Ping transmission capability
//#define STACK_USE_AUTO_IP               // Dynamic link-layer IP address automatic configuration protocol
#define STACK_USE_DHCP_CLIENT			// Dynamic Host Configuration Protocol client for obtaining IP address and other parameters
//#define STACK_USE_TFTP_CLIENT			// Trivial File Transfer Protocol client
//#define STACK_USE_BERKELEY_API			// Berekely Sockets APIs are available

// =======================================================================
//   Network Addressing Options
// =======================================================================

/* Default Network Configuration
 *   These settings are only used if data is not found in EEPROM.
 *   To clear EEPROM, hold BUTTON0, reset the board, and continue
 *   holding until the LEDs flash.  Release, and reset again.
 */
//#define MY_DEFAULT_HOST_NAME			"MCHPBOARD"

#define MY_DEFAULT_MAC_BYTE1            (0x00)	// Use the default of 00-04-A3-00-00-00
#define MY_DEFAULT_MAC_BYTE2            (0x04)	// if using an ENCX24J600, MRF24WB0M, or
#define MY_DEFAULT_MAC_BYTE3            (0xA3)	// PIC32MX6XX/7XX internal Ethernet 
#define MY_DEFAULT_MAC_BYTE4            (0x01)	// controller and wish to use the 
#define MY_DEFAULT_MAC_BYTE5            (0x02)	// internal factory programmed MAC
#define MY_DEFAULT_MAC_BYTE6            (0x03)	// address instead.

#define MY_DEFAULT_IP_ADDR_BYTE1        (169ul)
#define MY_DEFAULT_IP_ADDR_BYTE2        (254ul)
#define MY_DEFAULT_IP_ADDR_BYTE3        (1ul)
#define MY_DEFAULT_IP_ADDR_BYTE4        (1ul)

#define MY_DEFAULT_MASK_BYTE1           (255ul)
#define MY_DEFAULT_MASK_BYTE2           (255ul)
#define MY_DEFAULT_MASK_BYTE3           (0ul)
#define MY_DEFAULT_MASK_BYTE4           (0ul)

#define MY_DEFAULT_GATE_BYTE1           (169ul)
#define MY_DEFAULT_GATE_BYTE2           (254ul)
#define MY_DEFAULT_GATE_BYTE3           (1ul)
#define MY_DEFAULT_GATE_BYTE4           (1ul)

#define MY_DEFAULT_PRIMARY_DNS_BYTE1	(169ul)
#define MY_DEFAULT_PRIMARY_DNS_BYTE2	(254ul)
#define MY_DEFAULT_PRIMARY_DNS_BYTE3	(1ul)
#define MY_DEFAULT_PRIMARY_DNS_BYTE4	(1ul)

#define MY_DEFAULT_SECONDARY_DNS_BYTE1	(0ul)
#define MY_DEFAULT_SECONDARY_DNS_BYTE2	(0ul)
#define MY_DEFAULT_SECONDARY_DNS_BYTE3	(0ul)
#define MY_DEFAULT_SECONDARY_DNS_BYTE4	(0ul)

// =======================================================================
//   Transport Layer Options
// =======================================================================

/* Transport Layer Configuration
 *   The following low level modules are automatically enabled
 *   based on module selections above.  If your custom module
 *   requires them otherwise, enable them here.
 */
#define STACK_USE_TCP
#define STACK_USE_UDP

/* Client Mode Configuration
 *   Uncomment following line if this stack will be used in CLIENT
 *   mode.  In CLIENT mode, some functions specific to client operation
 *   are enabled.
 */
#define STACK_CLIENT_MODE

/* TCP Socket Memory Allocation
 *   TCP needs memory to buffer incoming and outgoing data.  The
 *   amount and medium of storage can be allocated on a per-socket
 *   basis using the example below as a guide.
 */
	// Allocate how much total RAM (in bytes) you want to allocate
	// for use by your TCP TCBs, RX FIFOs, and TX FIFOs.
	#define TCP_ETH_RAM_SIZE				(3900ul)
	#define TCP_PIC_RAM_SIZE				(0ul)
	#define TCP_SPI_RAM_SIZE				(0ul)
	#define TCP_SPI_RAM_BASE_ADDRESS			(0x00)

	// Define names of socket types
	#define TCP_SOCKET_TYPES
		#define TCP_PURPOSE_GENERIC_TCP_CLIENT 0
		#define TCP_PURPOSE_GENERIC_TCP_SERVER 1
		#define TCP_PURPOSE_STREAM_TCP_SERVER 2
//		#define TCP_PURPOSE_TELNET 2
//		#define TCP_PURPOSE_FTP_COMMAND 3
//		#define TCP_PURPOSE_FTP_DATA 4
//		#define TCP_PURPOSE_TCP_PERFORMANCE_TX 5
//		#define TCP_PURPOSE_TCP_PERFORMANCE_RX 6
//		#define TCP_PURPOSE_UART_2_TCP_BRIDGE 7
//		#define TCP_PURPOSE_HTTP_SERVER 8
//		#define TCP_PURPOSE_DEFAULT 9
//		#define TCP_PURPOSE_BERKELEY_SERVER 10
//		#define TCP_PURPOSE_BERKELEY_CLIENT 11
	#define END_OF_TCP_SOCKET_TYPES

	#if defined(__TCP_C)
		// Define what types of sockets are needed, how many of
		// each to include, where their TCB, TX FIFO, and RX FIFO
		// should be stored, and how big the RX and TX FIFOs should
		// be.  Making this initializer bigger or smaller defines
		// how many total TCP sockets are available.
		//
		// Each socket requires up to 56 bytes of PIC RAM and
		// 48+(TX FIFO size)+(RX FIFO size) bytes of TCP_*_RAM each.
		//
		// Note: The RX FIFO must be at least 1 byte in order to
		// receive SYN and FIN messages required by TCP.  The TX
		// FIFO can be zero if desired.
		#define TCP_CONFIGURATION
		ROM struct
		{
			uint8_t vSocketPurpose;
			//uint8_t vMemoryMedium;
			uint16_t wTXBufferSize;
			uint16_t wRXBufferSize;
		} TCPSocketInitializer[] =
		{
            // Control port
			{TCP_PURPOSE_GENERIC_TCP_SERVER, /*TCP_ETH_RAM, */128, 128},
			//{TCP_PURPOSE_GENERIC_TCP_CLIENT, TCP_ETH_RAM, 125, 100},
			// Sinks
			//{TCP_PURPOSE_TELNET, TCP_ETH_RAM, 200, 150},
			//{TCP_PURPOSE_TELNET, TCP_ETH_RAM, 200, 150},
			//{TCP_PURPOSE_TELNET, TCP_ETH_RAM, 200, 150},
			//{TCP_PURPOSE_FTP_COMMAND, TCP_ETH_RAM, 100, 40},
			//{TCP_PURPOSE_FTP_DATA, TCP_ETH_RAM, 0, 128},
			//{TCP_PURPOSE_TCP_PERFORMANCE_TX, TCP_ETH_RAM, 200, 1},
			//{TCP_PURPOSE_TCP_PERFORMANCE_RX, TCP_ETH_RAM, 40, 1500},
			//{TCP_PURPOSE_UART_2_TCP_BRIDGE, TCP_ETH_RAM, 256, 256},
			//{TCP_PURPOSE_HTTP_SERVER, TCP_ETH_RAM, 200, 200},
			//{TCP_PURPOSE_HTTP_SERVER, TCP_ETH_RAM, 200, 200},
			//{TCP_PURPOSE_DEFAULT, TCP_ETH_RAM, 200, 200},
			//{TCP_PURPOSE_BERKELEY_SERVER, TCP_ETH_RAM, 25, 20},
			//{TCP_PURPOSE_BERKELEY_CLIENT, TCP_ETH_RAM, 125, 100},
		};
		#define END_OF_TCP_CONFIGURATION
	#endif

/* UDP Socket Configuration
 *   Define the maximum number of available UDP Sockets, and whether
 *   or not to include a checksum on packets being transmitted.
 */
#define MAX_UDP_SOCKETS     (2u)    // DHCP consumes 1 socket!
//#define UDP_USE_TX_CHECKSUM		// This slows UDP TX performance by nearly 50%, except when using the ENCX24J600 or PIC32MX6XX/7XX, which have a super fast DMA and incurs virtually no speed pentalty.

/* Berkeley API Sockets Configuration
 *   Note that each Berkeley socket internally uses one TCP or UDP socket
 *   defined by MAX_UDP_SOCKETS and the TCPSocketInitializer[] array.
 *   Therefore, this number MUST be less than or equal to MAX_UDP_SOCKETS + the
 *   number of TCP sockets defined by the TCPSocketInitializer[] array
 *   (i.e. sizeof(TCPSocketInitializer)/sizeof(TCPSocketInitializer[0])).
 *   This define has no effect if STACK_USE_BERKELEY_API is not defined and
 *   Berkeley Sockets are disabled.  Set this value as low as your application
 *   requires to avoid waisting RAM.
 */
//#define BSD_SOCKET_COUNT (5u)

// =======================================================================
//   Application-Specific Options
// =======================================================================

// -- HTTP2 Server options -----------------------------------------------

	// Maximum numbers of simultaneous HTTP connections allowed.
	// Each connection consumes 2 bytes of RAM and a TCP socket
	//#define MAX_HTTP_CONNECTIONS	(0u)

#endif

