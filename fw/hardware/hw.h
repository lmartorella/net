#ifndef HW_H
#define	HW_H

#ifdef _CONF_MCU_ETH_CARD
#include "fuses_mcu_card.h"
#elif _CONF_MINI_BEAN
#include "fuses_mini_bean.h"
#else
#error Missing configuration
#endif

// The smallest type capable of representing all values in the enumeration type.
enum RESET_REASON
{
	RESET_POWER = 1,  // Power-on reset
	RESET_BROWNOUT,
	RESET_CONFIGMISMATCH,
	RESET_WATCHDOG,
	RESET_STACKFAIL,
	RESET_MCLR,
	RESET_EXC
};
extern enum RESET_REASON _reason;

void enableInterrupts();
void sys_storeResetReason();

#endif	/* HW_H */

