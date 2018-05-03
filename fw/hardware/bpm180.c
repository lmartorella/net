#include "../pch.h"
#include "bpm180.h"
#include "i2c.h"

// BPM180 I2C module to read barometric data (air pressure)
#ifdef HAS_BPM180

static TICK_TYPE s_lastTime;

void bpm180_init() {
    i2c_init();
    s_lastTime = TickGet();
}

void bpm180_poll() {
    i2c_poll();
    
    if ((TickGet() - s_lastTime) > (TICK_SECOND * 2)) {
        s_lastTime = TickGet();
        
        i2c_sendReceive(0x77, 1, [reg], I2C_DIR_SEND);
        // Then at the next cycle
        i2c_sendReceive(0x78, 2, [regs], I2C_DIR_RECEIVE);
    }
}

#endif
