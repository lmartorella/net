#ifndef HW_H
#define	HW_H

#ifdef _CONF_MCU_ETH_CARD
#include "fuses_mcu_card.h"
#elif _CONF_MINI_BEAN
#include "fuses_mini_bean.h"
#else
#error Missing configuration
#endif

void enableInterrupts();

#endif	/* HW_H */

