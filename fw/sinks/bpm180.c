#include "../pch.h"
#include "bpm180.h"

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
    STATE_ASK_ID,
    STATE_RCV_ID,
    STATE_ASK_CALIB_TABLE,
    STATE_RCV_CALIB_TABLE,
    STATE_ASK_TEMP,
    STATE_ASK_TEMP_2,
    STATE_RCV_TEMP,
    STATE_ASK_PRESS,
    STATE_ASK_PRESS_2,
    STATE_RCV_PRESS
} s_state;

static BYTE* s_buffer;

void bpm180_init() {
    s_state = STATE_IDLE;
    s_lastTime = TickGet();
}

// Buffer should be at least 22 bytes
void bpm180_askIdCalib(BYTE* buffer) {
    if (s_state != STATE_IDLE) {
        fatal("B.BSY");
    }
    s_buffer = buffer;
    s_buffer[0] = ADDR_DEVICE_ID;
    i2c_sendReceive7(REG_WRITE, 1, s_buffer);
    s_state = STATE_ASK_ID;
}

// Buffer should be at least 2 bytes
void bpm180_askTemp(BYTE* buffer) {
    if (s_state != STATE_IDLE) {
        fatal("B.BSY");
    }
    s_buffer = buffer;
    s_buffer[0] = ADDR_MSG_CONTROL;
    s_buffer[1] = MESSAGE_READ_TEMP;
    i2c_sendReceive7(REG_WRITE, 2, s_buffer);
    s_state = STATE_ASK_TEMP;
    s_lastTime = TickGet();
}

// Buffer should be at least 3 bytes
void bpm180_askPressure(BYTE* buffer) {
    // Ask pressure (max sampling)
    s_buffer[0] = ADDR_MSG_CONTROL;
    s_buffer[1] = MESSAGE_READ_PRESS_OSS_3;
    i2c_sendReceive7(REG_WRITE, 2, s_buffer);
    s_state = STATE_ASK_PRESS;
    s_lastTime = TickGet();
}

BOOL bpm180_poll() {
    if (s_state == STATE_IDLE) {
        return TRUE;
    }
    if ((TickGet() - s_lastTime) > TICK_SECOND) {
        fatal("B.LOCK");
    }
    
    int oss = 3;
    BOOL i2cIdle = i2c_poll();
    if (!i2cIdle) {
        return FALSE; 
    }
    
    switch (s_state) {
        case STATE_ASK_ID:
            // Start read buffer
            i2c_sendReceive7(REG_READ, 1, s_buffer);
            s_state = STATE_RCV_ID;
            break;
        case STATE_RCV_ID:
            if (s_buffer[0] != 0x55) {
                fatal("B.ID");
            }

            // Ask table
            s_buffer[0] = ADDR_CALIB0;
            i2c_sendReceive7(REG_WRITE, 1, s_buffer);
            s_state = STATE_ASK_CALIB_TABLE;
            break;
            
        case STATE_ASK_CALIB_TABLE:
            // Start read table
            i2c_sendReceive7(REG_READ, 22, s_buffer);
            s_state = STATE_RCV_CALIB_TABLE;
            break;

        case STATE_RCV_CALIB_TABLE:   
            s_state = STATE_IDLE;
            // Buffer ready!
            break;
        case STATE_ASK_TEMP:   
            if ((TickGet() - s_lastTime) > (TICKS_PER_MSECOND * 5)) {
                s_buffer[0] = ADDR_MSB;
                i2c_sendReceive7(REG_WRITE, 1, s_buffer);
                s_state = STATE_ASK_TEMP_2;
            }
            break;
        case STATE_ASK_TEMP_2:
            // Read results
            i2c_sendReceive7(REG_READ, 2, s_buffer);
            s_state = STATE_RCV_TEMP;
            break;
        case STATE_RCV_TEMP:
            s_state = STATE_IDLE;
            // Buffer ready!
            break;
        case STATE_ASK_PRESS:   
            if ((TickGet() - s_lastTime) > (TICKS_PER_MSECOND * 30)) {
                s_buffer[0] = ADDR_MSB;
                i2c_sendReceive7(REG_WRITE, 1, s_buffer);
                s_state = STATE_ASK_PRESS_2;
            }
            break;
        case STATE_ASK_PRESS_2:
            // Read results
            i2c_sendReceive7(REG_READ, 3, s_buffer);
            s_state = STATE_RCV_PRESS;
            break;
        case STATE_RCV_PRESS:
            s_state = STATE_IDLE;
            // Buffer ready!
            break;
    }
    return FALSE;
}

#endif
