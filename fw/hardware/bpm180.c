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
    STATE_ASK_ID,
    STATE_RCV_ID,
    STATE_ASK_CALIB_TABLE,
    STATE_RCV_CALIB_TABLE,
    STATE_ASK_TEMP,
    STATE_ASK_TEMP_2,
    STATE_RCV_TEMP
} s_state;

static BYTE s_buffer[16];
static int s_counter = 0;

static struct {
    short ac1;
    short ac2;
    short ac3;
    unsigned short ac4;
    unsigned short ac5;
    unsigned short ac6;
    short b1;
    short b2;
    short mb;
    short mc;
    short md;
} s_calibTable;

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
                s_buffer[0] = ADDR_DEVICE_ID;
                i2c_sendReceive7(REG_WRITE, 1, s_buffer);
                s_state = STATE_ASK_ID;
            }
            break;
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
            i2c_sendReceive7(REG_READ, 22, (void*)&s_calibTable);
            s_state = STATE_RCV_CALIB_TABLE;
            break;

        case STATE_RCV_CALIB_TABLE:   
            // Change endianness
            for (int i = 0; i < 11; i++) {
                BYTE b1 = ((BYTE*)&s_calibTable)[i * 2];
                ((BYTE*)&s_calibTable)[i * 2] = ((BYTE*)&s_calibTable)[i * 2 + 1];
                ((BYTE*)&s_calibTable)[i * 2 + 1] = b1;
            }
            
            // Ask temp
            s_buffer[0] = ADDR_MSG_CONTROL;
            s_buffer[1] = MESSAGE_READ_TEMP;
            i2c_sendReceive7(REG_WRITE, 2, s_buffer);
            s_state = STATE_ASK_TEMP;
            s_lastTime = TickGet();
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
            {
                // invert temp
                long uncompTemp = (s_buffer[0] << 8) + s_buffer[1];

                // Calc true temp
                long x1 = (uncompTemp - s_calibTable.ac6) * s_calibTable.ac5 / 0x8000l;
                long x2 = (long)s_calibTable.mc * 0x800l / (x1 + s_calibTable.md);
                long b5 = x1 + x2;
                float t = (b5 + 8l) / 160.0f;  // Temp in C

                // FOR ETH MASTER MODULE ONLY
                sprintf(s_buffer, "%02d: Temp %5.1f", (s_counter++), t);
                println(s_buffer);
            }
                        
            // DONE!
            s_state = STATE_IDLE;
            s_lastTime = TickGet();
            break;
    }
}

#endif
