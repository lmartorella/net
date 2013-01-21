#ifndef _UTILS_BEAN_H_
#define _UTILS_BEAN_H_

typedef unsigned char BYTE;
typedef unsigned short UINT16;
typedef unsigned short WORD;
typedef unsigned short long UINT24;
typedef unsigned long UINT32;
typedef unsigned long DWORD;

typedef struct 
{
	DWORD d1;
	DWORD d2;
} QWORD;

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