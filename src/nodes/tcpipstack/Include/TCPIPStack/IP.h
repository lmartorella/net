/*********************************************************************
 *
 *                  IP Defs for Microchip TCP/IP Stack
 *
 *********************************************************************
 * FileName:        IP.h
 * Dependencies:    StackTsk.h
 *                  MAC.h
 * Processor:       PIC18, PIC24F, PIC24H, dsPIC30F, dsPIC33F, PIC32
 * Compiler:        Microchip C32 v1.05 or higher
 *					Microchip C30 v3.12 or higher
 *					Microchip C18 v3.30 or higher
 *					HI-TECH PICC-18 PRO 9.63PL2 or higher
 * Company:         Microchip Technology, Inc.
 *
 * Software License Agreement
 *
 * Copyright (C) 2002-2009 Microchip Technology Inc.  All rights
 * reserved.
 *
 * Microchip licenses to you the right to use, modify, copy, and
 * distribute:
 * (i)  the Software when embedded on a Microchip microcontroller or
 *      digital signal controller product ("Device") which is
 *      integrated into Licensee's product; or
 * (ii) ONLY the Software driver source files ENC28J60.c, ENC28J60.h,
 *		ENCX24J600.c and ENCX24J600.h ported to a non-Microchip device
 *		used in conjunction with a Microchip ethernet controller for
 *		the sole purpose of interfacing with the ethernet controller.
 *
 * You should refer to the license agreement accompanying this
 * Software for additional information regarding your rights and
 * obligations.
 *
 * THE SOFTWARE AND DOCUMENTATION ARE PROVIDED "AS IS" WITHOUT
 * WARRANTY OF ANY KIND, EITHER EXPRESS OR IMPLIED, INCLUDING WITHOUT
 * LIMITATION, ANY WARRANTY OF MERCHANTABILITY, FITNESS FOR A
 * PARTICULAR PURPOSE, TITLE AND NON-INFRINGEMENT. IN NO EVENT SHALL
 * MICROCHIP BE LIABLE FOR ANY INCIDENTAL, SPECIAL, INDIRECT OR
 * CONSEQUENTIAL DAMAGES, LOST PROFITS OR LOST DATA, COST OF
 * PROCUREMENT OF SUBSTITUTE GOODS, TECHNOLOGY OR SERVICES, ANY CLAIMS
 * BY THIRD PARTIES (INCLUDING BUT NOT LIMITED TO ANY DEFENSE
 * THEREOF), ANY CLAIMS FOR INDEMNITY OR CONTRIBUTION, OR OTHER
 * SIMILAR COSTS, WHETHER ASSERTED ON THE BASIS OF CONTRACT, TORT
 * (INCLUDING NEGLIGENCE), BREACH OF WARRANTY, OR OTHERWISE.
 *
 *
 * Author               Date    Comment
 *~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
 * Nilesh Rajbharti     4/27/01 Original        (Rev 1.0)
 * Nilesh Rajbharti     2/9/02  Cleanup
 * Nilesh Rajbharti     5/22/02 Rev 2.0 (See version.log for detail)
 ********************************************************************/
#ifndef __IP_H
#define __IP_H


#define IP_PROT_ICMP    (1u)
#define IP_PROT_TCP     (6u)
#define IP_PROT_UDP     (17u)


// IP packet header definition
typedef struct _IP_HEADER
{
    uint8_t    VersionIHL;
    uint8_t    TypeOfService;
    uint16_t    TotalLength;
    uint16_t    Identification;
    uint16_t    FragmentInfo;
    uint8_t    TimeToLive;
    uint8_t    Protocol;
    uint16_t    HeaderChecksum;
    IP_ADDR SourceAddress;
    IP_ADDR DestAddress;
} IP_HEADER;

// IP Pseudo header as defined by RFC 793 (needed for TCP and UDP 
// checksum calculations/verification)
typedef struct _PSEUDO_HEADER
{
    IP_ADDR SourceAddress;
    IP_ADDR DestAddress;
    uint8_t Zero;
    uint8_t Protocol;
    uint16_t Length;
} PSEUDO_HEADER;

#define SwapPseudoHeader(h)  (h.Length = swaps(h.Length))


/*********************************************************************
 * Function:        _Bool IPIsTxReady(_Bool HighPriority)
 *
 * PreCondition:    None
 *
 * Input:           None
 *
 * Output:          true if transmit buffer is empty
 *                  false if transmit buffer is not empty
 *
 * Side Effects:    None
 *
 * Note:            None
 *
 ********************************************************************/
#define IPIsTxReady()       MACIsTxReady()


/*********************************************************************
 * Macro:           IPSetTxBuffer(a, b)
 *
 * PreCondition:    None
 *
 * Input:           a       - Buffer identifier
 *                  b       - Offset
 *
 * Output:          Next Read/Write access to transmit buffer 'a'
 *                  set to offset 'b'
 *
 * Side Effects:    None
 *
 * Note:            None
 *
 ********************************************************************/
#define IPSetTxBuffer(b) MACSetWritePtr((ETH_POINTER)BASE_TX_ADDR + (b) + sizeof(ETHER_HEADER) + sizeof(IP_HEADER))



/*********************************************************************
 * Function:        uint16_t IPPutHeader(   IP_ADDR *Dest,
 *                                      uint8_t    Protocol,
 *                                      uint16_t    Identifier,
 *                                      uint16_t    DataLen)
 *
 * PreCondition:    IPIsTxReady() == true
 *
 * Input:           Src         - Destination node address
 *                  Protocol    - Current packet protocol
 *                  Identifier  - Current packet identifier
 *                  DataLen     - Current packet data length
 *
 * Output:          Handle to current packet - For use by
 *                  IPSendByte() function.
 *
 * Side Effects:    None
 *
 * Note:            Only one IP message can be transmitted at any
 *                  time.
 *                  Caller may not transmit and receive a message
 *                  at the same time.
 *
 ********************************************************************/
uint16_t    IPPutHeader(NODE_INFO *remote,
                    uint8_t protocol,
                    uint16_t len);


/*********************************************************************
 * Function:        _Bool IPGetHeader( IP_ADDR    *localIP,
 *                                    NODE_INFO  *remote,
 *                                    uint8_t        *Protocol,
 *                                    uint16_t        *len)
 *
 * PreCondition:    MACGetHeader() == true
 *
 * Input:           localIP     - Local node IP Address as received
 *                                in current IP header.
 *                                If this information is not required
 *                                caller may pass NULL value.
 *                  remote      - Remote node info
 *                  Protocol    - Current packet protocol
 *                  len         - Current packet data length
 *
 * Output:          true, if valid packet was received
 *                  false otherwise
 *
 * Side Effects:    None
 *
 * Note:            Only one IP message can be received.
 *                  Caller may not transmit and receive a message
 *                  at the same time.
 *
 ********************************************************************/
_Bool IPGetHeader(IP_ADDR *localIP,
                 NODE_INFO *remote,
                 uint8_t *protocol,
                 uint16_t *len);


/*********************************************************************
 * Macro:           IPDiscard()
 *
 * PreCondition:    MACGetHeader() == true
 *
 * Input:           None
 *
 * Output:          Current packet is discarded and buffer is
 *                  freed-up
 *
 * Side Effects:    None
 *
 * Note:            None
 *
 ********************************************************************/
#define IPDiscard()         MACDiscard()



/*********************************************************************
 * Macro:           IPGetArray(a, b)
 *
 * PreCondition:    MACGetHeader() == true
 *
 * Input:           a       - Data buffer
 *                  b       - Buffer length
 *
 * Output:          None
 *
 * Side Effects:    None
 *
 * Note:            Data is copied from IP data to given buffer
 *
 ********************************************************************/
#define IPGetArray(a, b)    MACGetArray(a, b)




/*********************************************************************
 * Function:        IPSetRxBuffer(uint16_t Offset)
 *
 * PreCondition:    IPHeaderLen must have been intialized by 
 *					IPGetHeader() or IPPutHeader()
 *
 * Input:           Offset from beginning of IP data field
 *
 * Output:          Next Read/Write access to receive buffer is
 *                  set to Offset 
 *
 * Side Effects:    None
 *
 * Note:            None
 *
 ********************************************************************/
void IPSetRxBuffer(uint16_t Offset);





#endif



