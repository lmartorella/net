#ifndef HW_H
#define	HW_H

#ifdef _CONF_TEST_ETH_CARD
#include "fuses_test_eth_card.h"
#define _IS_ETH_CARD

#elif defined(_CONF_MCU_CARD)
#include "fuses_mcu_card.h"
#define _IS_ETH_CARD

#elif defined(_CONF_TEST_MINI_BEAN)
#include "fuses_test_mini_bean.h"
#define _IS_PIC16F628_CARD

#elif defined(_CONF_SOLAR_BEAN)
#include "fuses_solar_bean.h"
#define _IS_PIC16F628_CARD

#elif defined(_CONF_TEST_ETH_CARD_SLAVE)
#include "fuses_test_eth_card_slave.h"
#define _IS_ETH_CARD

#else
#error Missing configuration
#endif

void enableInterrupts();

#endif	/* HW_H */

