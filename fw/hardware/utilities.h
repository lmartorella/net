#ifndef _UTILS_BEAN_H_
#define _UTILS_BEAN_H_

typedef unsigned char BYTE;
typedef unsigned short UINT16;
typedef struct 
{
	BYTE b[16];
} GUID;

typedef struct 
{
	BYTE major;
	BYTE minor;
} VERSION;

extern void wait2ms(void);
extern void wait30ms(void);
extern void wait40us(void);

#endif