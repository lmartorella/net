#include "../pch.h"
#include "hw.h"

#ifdef _IS_ETH_CARD
#include "fuses_eth_card.inc"
#elif defined(_IS_PIC16F628_CARD)
#include "fuses_pic16f628.inc"
#else
#error Missing configuration
#endif

