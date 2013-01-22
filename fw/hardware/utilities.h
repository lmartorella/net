#ifndef _UTILS_BEAN_H_
#define _UTILS_BEAN_H_

#include <GenericTypeDefs.h>

typedef struct 
{
	DWORD data1;
	WORD data2;
	WORD data3;
	QWORD data4;
} GUID;

extern void wait2ms(void);
extern void wait30ms(void);
extern void wait40us(void);

#endif