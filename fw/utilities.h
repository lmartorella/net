#ifndef _UTILS_BEAN_H_
#define _UTILS_BEAN_H_

extern void wait2ms(void);
extern void wait30ms(void);
extern void wait40us(void);

#define _PASTEB(X, PNAME) X ## PNAME ## bits
#define portBits(PNAME) _PASTEB(PORT, PNAME)
#define trisBits(PNAME) _PASTEB(TRIS, PNAME)

#endif