#ifndef _I2C_INCLUDE_
#define _I2C_INCLUDE_

#ifdef HAS_I2C

void i2c_init();
void i2c_poll();

typedef enum {
    I2C_DIR_SEND,
    I2C_DIR_RECEIVE,
} I2C_DIRECTION;

void i2c_sendReceive(BYTE addr, BYTE size, BYTE* buf, I2C_DIRECTION direction);

#endif
#endif
