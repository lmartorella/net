#include "pch.h"
#include "hardware/rs485.h"
#include "protocol.h"

#ifdef HAS_BUS_CLIENT

BOOL prot_started = TRUE;

BOOL prot_control_readW(WORD* w) {
    BOOL rc9;
    return rs485_read((BYTE*)w, 2, &rc9);
}

BOOL prot_control_read(void* data, WORD size){
    BOOL rc9;
    return rs485_read((BYTE*)data, size, &rc9);
}

void prot_control_writeW(WORD w){
    rs485_write(FALSE, (BYTE*)w, 2);
}

void prot_control_write(const void* data, WORD size){
    rs485_write(FALSE, (BYTE*)data, size);
}

void prot_control_flush() {
    // Do nothing
}

void prot_control_close(){
}

BOOL prot_control_isListening() {
    return TRUE;
}

WORD prot_control_getDataSize() {
    return rs485_readAvail();
}

void prot_slowTimer() {
    // Do nothing
}

#endif