#include "../pch.h"
#include "../protocol.h"

#ifdef HAS_DHT11

static bit tempSink_read();
static bit tempSink_write();

const Sink g_tempSink = {
    "TEMP",
    &sink_nullFunc,
    &tempSink_write
};

void dht11_init()
{
    // Port high, pullups, output
    DHT11_PORT_PULLUPS = 0;
    DHT11_PORT = 1;
    DHT11_PORT_TRIS = 1;
    
    US_TIMER_INIT();
    
    // wait for sensor to stabilize
    //wait1s();
}

static BYTE readByte()
{
    CLRWDT();
    BYTE res = 0;
    for (char i = 0; i < 8; i++)
    {
        res <<= 1;
        US_TIMER = 0;
        while (!DHT11_PORT)
            if (US_TIMER > 200) return 0xff;

        US_TIMER = 0;
        while (DHT11_PORT)
            if (US_TIMER > 200) return 0xff;

        if (US_TIMER > 40)
            res |= 1;
    }   
    return res;
}

bit dht11_read(BYTE* buffer)
{
    DHT11_PORT_TRIS = 0; // Data port is output
    DHT11_PORT = 0;     // low
    
    // Low for at least 18us
    for (int i = 0; i < 25; i++) {
        __delay_ms(1); 
        CLRWDT();
    }
    DHT11_PORT = 1;

    // Delay 30us
    US_TIMER = 0;
    while (US_TIMER < 30);
    
    DHT11_PORT_TRIS = 1; // Data port is input
    
    // Check response
    US_TIMER = 0;
    while (!DHT11_PORT) 
        if (US_TIMER > 200) return FALSE;
    US_TIMER = 0;
    while (DHT11_PORT) 
        if (US_TIMER > 200) return FALSE;
    
    for (char i = 0; i < 5; i++, buffer++) {
        *buffer = readByte();
    }
    return TRUE;
}

static bit tempSink_write() {
    BYTE data[6];
    data[0] = dht11_read(data + 1);
    prot_control_write(data, 6);

    // Finish data
    return FALSE;
}

#endif