#include "../pch.h"
#include "i2c.h"

#ifdef HAS_I2C

// I2C status bytes
static BYTE s_addr;
static BYTE s_size;
static BYTE* s_buf;
static I2C_DIRECTION s_direction;
static BYTE s_ptr;

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

void i2c_sendReceive(BYTE addr, BYTE size, BYTE* buf, I2C_DIRECTION direction) {
    // Check if MSSP module is in use
    if ((SSPCON2 & (SSPCON2bits_t.SEN | SSPCON2bits_t.RSEN | SSPCON2bits_t.PEN | SSPCON2bits_t.RCEN | SSPCON2bits_t.ACKEN)) || s_state != STATE_IDLE) {
        fatal("I2.U");
    }
    PIR1bits.SSP1IF = 0;

    // Store regs
    s_addr = addr;
    s_size = size;
    s_buf = buf;
    s_direction = direction;
    s_ptr = 0;
    
    // Start bit!
    // Start
    SSPCON2bits.SEN = 1; 
    s_state = STATE_START;
}

void i2c_poll() {
loop:
    // Something happened?
    if (!PIR1bits.SSPIF) {
        return;
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
            if (s_direction == DIR_RECEIVE) {
                SSPCON2bits.RCEN = 1;
                s_state = STATE_RXDATA;
            } else {
                SSPBUF = s_buf[s_ptr++];
                s_state = STATE_TXDATA;
            }
            break;
        case STATE_RXDATA:
            s_buf[s_ptr++] = SSPBUF;
            // Again?
            if (s_ptr == s_size) {
                // Send NACK
                SSPCON2bits.ACKDT = 0;
            } else {
                // Send ACK
                SSPCON2bits.ACKDT = 1;
            }
            SSPCON2bits.ACKEN = 1;
            s_state = STATE_ACK;
            break;
        case STATE_TXDATA:
            if (s_ptr == s_size) {
                if (SSPCON2bits.ACKSTAT) {
                    fatal("I2.AF");
                }
                // Send STOP
                SSPCON2bits.PEN = 1;
                s_state = STATE_STOP;
            } else {
                if (!SSPCON2bits.ACKSTAT) {
                    fatal("I2.AI");
                }
                // TX again
                SSPBUF = s_buf[s_ptr++];
            }
            break;
        case STATE_ACK:
            if (s_ptr == s_size) {
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
