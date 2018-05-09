#include "../pch.h"
#include "../sinks/bpm180.h"
#include "../appio.h"

// BPM180 I2C module to read barometric data (air pressure)
#ifdef HAS_BPM180_APP

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
static unsigned long s_ut;
// Uncompensate pressure
static unsigned long s_up;

// app state machine
static enum {
    STATE_IDLE,
    STATE_CALIB_TABLE,
    STATE_TEMP,
    STATE_PRESS
} s_state;

static TICK_TYPE s_lastTime;

void bpm180_app_init() {
    bpm180_init();
    s_lastTime = TickGet();
}

void bpm180_app_poll() {
    int oss = 3;
    BOOL i2cIdle = bpm180_poll();
    if (!i2cIdle) {
        return; 
    }
    
    switch (s_state) {
        case STATE_IDLE:
            // Start reading?
            if ((TickGet() - s_lastTime) > (TICK_SECOND * 2)) {
                bpm180_askIdCalib((void*)&s_calibTable);
                s_state = STATE_CALIB_TABLE;
            }
            break;
        case STATE_CALIB_TABLE:   
            // Change endianness
            for (int i = 0; i < 11; i++) {
                BYTE b1 = ((BYTE*)&s_calibTable)[i * 2];
                ((BYTE*)&s_calibTable)[i * 2] = ((BYTE*)&s_calibTable)[i * 2 + 1];
                ((BYTE*)&s_calibTable)[i * 2 + 1] = b1;
            }
            
            bpm180_askTemp(s_buffer);
            s_state = STATE_TEMP;
            break;
        case STATE_TEMP:
            // invert temp
            s_ut = ((unsigned long)s_buffer[0] << 8) | (unsigned long)s_buffer[1];

            bpm180_askPressure(s_buffer);
            s_state = STATE_PRESS;
            break;
        case STATE_PRESS:   
            // invert press
            s_up = (((unsigned long)s_buffer[0] << 16) | ((unsigned long)s_buffer[1] << 8) | (unsigned long)s_buffer[2]) >> (8 - oss);

            float temp, press;
            long b5;
            {
                // Calc true temp
                long x1 = (((long)s_ut - (long)s_calibTable.ac6) * (long)s_calibTable.ac5) >> 15;
                if (x1 == 0 && s_calibTable.md == 0) {
                    fatal("B.DIV");
                }
                long x2 = ((long)s_calibTable.mc << 11) / (x1 + s_calibTable.md);
                b5 = x1 + x2;
                short t = (b5 + 8) >> 4;  // Temp in 0.1C
                temp = t / 10.0;
            }
            {
                long b6 = b5 - 4000;
                long x1 = ((long)s_calibTable.b2 * ((b6 * b6) >> 12)) >> 11;
                long x2 = ((long)s_calibTable.ac2 * b6) >> 11;
                long x3 = x1 + x2;
                long b3 = ((((long)s_calibTable.ac1 * 4 + x3) << oss) + 2) >> 2;
                x1 = ((long)s_calibTable.ac3 * b6) >> 13;
                x2 = ((long)s_calibTable.b1 * ((b6 * b6) >> 12)) >> 16;
                x3 = ((x1 + x2) + 2) >> 2;
                unsigned long b4 = ((long)s_calibTable.ac4 * (unsigned long)(x3 + 32768)) >> 15;
                unsigned long b7 = ((unsigned long)(s_up - b3) * (50000ul >> oss));
                if (b4 == 0) {
                    fatal("B.DIV2");
                }
                long p;
                if (b7 < 0x80000000) {
                    p = (b7 << 1) / b4; 
                } else {
                    p = (b7 / b4) << 1;
                }
                x1 = (((p >> 8) * (p >> 8)) * 3038) >> 16;
                x2 = (p * -7357) >> 16;                
                p += (x1 + x2 + 3791) >> 4; // in Pascal
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
