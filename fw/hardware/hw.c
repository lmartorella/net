#ifdef _CONF_TEST_ETH_CARD
#include "fuses_eth_card.inc"
#elif _CONF_MINI_BEAN
#include "fuses_mini_bean.inc"
#else
#error Missing configuration
#endif

