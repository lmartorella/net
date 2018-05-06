#include "../pch.h"
#include "i2c.h"

#ifdef HAS_I2C

// I2C status bytes
static BYTE s_addr;
static BYTE* s_dest;
static BYTE* s_buf;
typedef enum {
    DIR_SEND = 0,
    DIR_RECEIVE = 1,
} I2C_DIRECTION;

// I2C state machine
static enum {
    STATE_IDLE,
    STATE_START,
    STATE_ADDR,
    STATE_TXDATA,
    STATE_RXDATA,
    STATE_ACK,
    STATE_STOP
} s_state;

void i2c_init() {
    // Ports as inputs
    TRISCbits.TRISC3 = 1;
    TRISCbits.TRISC4 = 1;
    
    // Baud generator
    SSPADD = 62; // FOsc = 25Mkz -> 100kHz
    
    // Setup I2C
    SSPSTATbits.SMP = 0;
    SSPSTATbits.CKE = 0;
    SSPCON1bits.SSPM = 8; // I2C master
    SSPCON1bits.SSPEN = 1;  

    s_state = STATE_IDLE;
}

void i2c_sendReceive7(BYTE addr, BYTE size, BYTE* buf) {
    // Check if MSSP module is in use
    BYTE mask = _SSPCON2_SEN_MASK | _SSPCON2_RSEN_MASK | _SSPCON2_PEN_MASK | _SSPCON2_RCEN_MASK | _SSPCON2_ACKEN_MASK;
    if ((SSPCON2 & mask) || s_state != STATE_IDLE) {
        fatal("I2.U");
    }
    PIR1bits.SSP1IF = 0;

    // Store regs
    s_addr = addr;
    s_buf = buf;
    s_dest = buf + size;
    
    // Start bit!
    // Start
    SSPCON2bits.SEN = 1; 
    s_state = STATE_START;
}

BOOL i2c_poll() {
loop:
    if (s_state == STATE_IDLE) {
        return TRUE; 
    }

    // Something happened?
    if (!PIR1bits.SSPIF) {
        return FALSE;
    }
    
    PIR1bits.SSPIF = 0;
    if (SSPCON1bits.WCOL) {
        fatal("I2.CL");
    }
    if (SSPCON1bits.SSPOV) {
        fatal("I2.OV");
    }
    
    switch (s_state) {
        case STATE_START:
            // Send address
            SSPBUF = s_addr;
            s_state = STATE_ADDR;
            break;
        case STATE_ADDR:
            if (SSPCON2bits.ACKSTAT) {
                // ACK not received. Err.
                fatal("I2.AA");
            }
            
            // Start send/receive
            if ((s_addr & 0x1) == DIR_RECEIVE) {
                SSPCON2bits.RCEN = 1;
                s_state = STATE_RXDATA;
            } else {
                SSPBUF = *s_buf;
                s_buf++;
                s_state = STATE_TXDATA;
            }
            break;
        case STATE_RXDATA:
            if (!SSPSTATbits.BF) {
                fatal("I.BF");
            }
            *s_buf = SSPBUF;
            s_buf++;
            // Again?
            if (s_buf >= s_dest) {
                // Finish: send NACK
                SSPCON2bits.ACKDT = 1;
            } else {
                // Again: send ACK
                SSPCON2bits.ACKDT = 0;
            }
            SSPCON2bits.ACKEN = 1;
            s_state = STATE_ACK;
            break;
        case STATE_TXDATA:
            if (SSPCON2bits.ACKSTAT) {
                // ACK not received? Err. (even the last byte, see BPM180 specs)
                fatal("I2.AI");
            }
            if (s_buf >= s_dest) {
                // Send STOP
                SSPCON2bits.PEN = 1;
                s_state = STATE_STOP;
            } else {
                // TX again
                SSPBUF = *s_buf;
                s_buf++;
            }
            break;
        case STATE_ACK:
            if (s_buf >= s_dest) {
                // Send STOP
                SSPCON2bits.PEN = 1;
                s_state = STATE_STOP;
            } else {
                SSPCON2bits.RCEN = 1;
                s_state = STATE_RXDATA;
            }
            break;
        case STATE_STOP:
            s_state = STATE_IDLE;
            break;
    }
    // It is possible that the IF flag is ready right now
    goto loop;
}
        
#endif
