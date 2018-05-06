#include "../pch.h"
#include "bpm180.h"
#include "i2c.h"
#include "./appio.h"

// BPM180 I2C module to read barometric data (air pressure)
#ifdef HAS_BPM180

static TICK_TYPE s_lastTime;
#define REG_READ 0xef
#define REG_WRITE 0xee

#define MESSAGE_READ_TEMP 0x2e
#define MESSAGE_READ_PRESS_OSS_0 0x34
#define MESSAGE_READ_PRESS_OSS_1 0x74
#define MESSAGE_READ_PRESS_OSS_2 0xb4
#define MESSAGE_READ_PRESS_OSS_3 0xf4

#define ADDR_CALIB0 0xaa
#define ADDR_CALIB21 0xbf
#define ADDR_DEVICE_ID 0xd0
#define ADDR_MSG_CONTROL 0xf4
#define ADDR_MSB 0xf6
#define ADDR_LSB 0xf7
#define ADDR_XLSB 0xf8

// BPM180 state machine
static enum {
    STATE_IDLE,
    STATE_WRITE_BUFFER,
    STATE_READ_BUFFER
} s_state;

static BYTE s_buffer[16];
static int s_counter = 0;

void bpm180_init() {
    s_state = STATE_IDLE;
    i2c_init();
    s_lastTime = TickGet();
}

void bpm180_poll() {
    BOOL i2cIdle = i2c_poll();
    if (!i2cIdle) {
        return; 
    }
    
    switch (s_state) {
        case STATE_IDLE:
            // Start reading?
            if ((TickGet() - s_lastTime) > (TICK_SECOND * 2)) {
                s_lastTime = TickGet();

                s_buffer[0] = ADDR_DEVICE_ID;
                i2c_sendReceive7(REG_WRITE, 1, s_buffer);
                s_state = STATE_WRITE_BUFFER;
            }
            break;
        case STATE_WRITE_BUFFER:
            // Start read buffer
            i2c_sendReceive7(REG_READ, 1, s_buffer);
            s_state = STATE_READ_BUFFER;
            break;
        case STATE_READ_BUFFER:
            if (s_buffer[0] != 0x55) {
                fatal("B.DID");
            }
            
            // FOR ETH
            sprintf(s_buffer, "%02d: ID 0x%02x", (s_counter++), s_buffer[0]);
            println(s_buffer);
            
            // DONE!
            s_state = STATE_IDLE;
            break;
    }
    
}

#endif
