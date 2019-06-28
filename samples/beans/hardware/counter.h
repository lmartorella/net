#ifndef DIG_COUNTER_H
#define	DIG_COUNTER_H

#ifdef HAS_DIGITAL_COUNTER

void dcnt_interrupt();
void dcnt_init();
void dcnt_poll();

/**
 * The system persistence record
 */
typedef struct
{
    // Used by counter
    DWORD dcnt_counter;
} PersistentData2;

static EEPROM_MODIFIER PersistentData s_persistentData2 = {
    0
};

typedef struct {
    // Copied from persistence to better control atomicity of accesses. 
    // Ticks.
    DWORD counter;
    // Should be enough for 200lt/min. Tick/secs.
    WORD flow;
} DCNT_DATA;
void dcnt_getDataCopy(DCNT_DATA* data);

#endif

#endif	/* COUNTER_H */

