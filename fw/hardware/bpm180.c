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
    STATE_RCV_TEMP,
    STATE_ASK_PRESS,
    STATE_ASK_PRESS_2,
    STATE_RCV_PRESS
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
// Uncompensate temp
static short s_ut;
// Uncompensate pressure
static short s_up;

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
            // invert temp
            s_ut = (s_buffer[0] << 8) + s_buffer[1];

            // Ask pressure (min sampling)
            s_buffer[0] = ADDR_MSG_CONTROL;
            s_buffer[1] = MESSAGE_READ_PRESS_OSS_0;
            i2c_sendReceive7(REG_WRITE, 2, s_buffer);
            s_state = STATE_ASK_PRESS;
            s_lastTime = TickGet();
            break;
        case STATE_ASK_PRESS:   
            if ((TickGet() - s_lastTime) > (TICKS_PER_MSECOND * 5)) {
                s_buffer[0] = ADDR_MSB;
                i2c_sendReceive7(REG_WRITE, 1, s_buffer);
                s_state = STATE_ASK_PRESS_2;
            }
            break;
        case STATE_ASK_PRESS_2:
            // Read results
            i2c_sendReceive7(REG_READ, 2, s_buffer);
            s_state = STATE_RCV_PRESS;
            break;
        case STATE_RCV_PRESS:
            // invert temp
            s_up = (s_buffer[0] << 8) + s_buffer[1];

            float temp, press;
            long b5;
            {
                // Calc true temp
                long x1 = ((long)s_ut - s_calibTable.ac6) * s_calibTable.ac5 / 0x8000l;
                long x2 = (long)s_calibTable.mc * 2048l / (x1 + s_calibTable.md);
                b5 = x1 + x2;
                long t = (b5 + 8l) / 16l;  // Temp in 0.1C
                temp = t / 10.0;
            }
            {
                int oss = 0;
                long b6 = b5 - 4000;
                long x1 = ((long)s_calibTable.b2 * (b6 * b6 / 4096l)) / 2048l;
                long x2 = (long)s_calibTable.ac2 * b6 / 2048l;
                long x3 = x1 + x2;
                long b3 = ((((long)s_calibTable.ac1 * 4 + x3) << oss) + 2) / 4l;
                x1 = (long)s_calibTable.ac3 * b6 / 8192l;
                x2 = ((long)s_calibTable.b1 * (b6 * b6 / 4096l)) / 65536l;
                x3 = ((x1 + x2) + 2) / 4l;
                unsigned long b4 = (long)s_calibTable.ac4 * (unsigned long)(x3 + 32768ul) / 32768l;
                unsigned long b7 = ((unsigned long)s_up - b3) * (50000ul >> oss);
                long p;
                if (b7 < 0x80000000) {
                   p = (b7 * 2) / b4; 
                } else {
                    p = (b7 / b4) * 2;
                }
                x1 = (p / 256) * (p / 256);
                x1 = (x1 * 3038) / 65536l;
                x2 = (-7357l * p) / 65536l;                
                p = p + (x1 + x2 + 3791l) / 16l; // in Pascal
                press = p / 100.0; // in hPa
            }
            
            // FOR ETH MASTER MODULE ONLY
            sprintf(s_buffer, "%4.1f'C %6.1fhPa", temp, press);
            println(s_buffer);
                        
            // DONE!
            s_state = STATE_IDLE;
            s_lastTime = TickGet();
            break;
    }
}

#endif
