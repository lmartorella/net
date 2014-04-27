#
# Generated Makefile - do not edit!
#
# Edit the Makefile in the project folder instead (../Makefile). Each target
# has a -pre and a -post target defined where you can add customized code.
#
# This makefile implements configuration specific macros and targets.


# Include project Makefile
ifeq "${IGNORE_LOCAL}" "TRUE"
# do not include local makefile. User is passing all local related variables already
else
include Makefile
# Include makefile containing local settings
ifeq "$(wildcard nbproject/Makefile-local-default.mk)" "nbproject/Makefile-local-default.mk"
include nbproject/Makefile-local-default.mk
endif
endif

# Environment
MKDIR=gnumkdir -p
RM=rm -f 
MV=mv 
CP=cp 

# Macros
CND_CONF=default
ifeq ($(TYPE_IMAGE), DEBUG_RUN)
IMAGE_TYPE=debug
OUTPUT_SUFFIX=elf
DEBUGGABLE_SUFFIX=elf
FINAL_IMAGE=dist/${CND_CONF}/${IMAGE_TYPE}/bean.X.${IMAGE_TYPE}.${OUTPUT_SUFFIX}
else
IMAGE_TYPE=production
OUTPUT_SUFFIX=hex
DEBUGGABLE_SUFFIX=elf
FINAL_IMAGE=dist/${CND_CONF}/${IMAGE_TYPE}/bean.X.${IMAGE_TYPE}.${OUTPUT_SUFFIX}
endif

# Object Directory
OBJECTDIR=build/${CND_CONF}/${IMAGE_TYPE}

# Distribution Directory
DISTDIR=dist/${CND_CONF}/${IMAGE_TYPE}

# Source Files Quoted if spaced
SOURCEFILES_QUOTED_IF_SPACED=../hardware/cm1602.c ../hardware/eeprom.c ../hardware/fuses.c ../hardware/spi.c ../hardware/spiram.c ../hardware/vs1011e.c ../tcpipstack/TCPIPStack/ARP.c ../tcpipstack/TCPIPStack/DHCP.c ../tcpipstack/TCPIPStack/DNS.c ../tcpipstack/TCPIPStack/Delay.c ../tcpipstack/TCPIPStack/ETH97J60.c ../tcpipstack/TCPIPStack/Hashes.c ../tcpipstack/TCPIPStack/Helpers.c ../tcpipstack/TCPIPStack/ICMP.c ../tcpipstack/TCPIPStack/IP.c ../tcpipstack/TCPIPStack/StackTsk.c ../tcpipstack/TCPIPStack/TCP.c ../tcpipstack/TCPIPStack/Tick.c ../tcpipstack/TCPIPStack/UDP.c ../appio.c ../flasher.c ../main.c ../persistence.c ../protocol.c ../timers.c

# Object Files Quoted if spaced
OBJECTFILES_QUOTED_IF_SPACED=${OBJECTDIR}/_ext/439174825/cm1602.p1 ${OBJECTDIR}/_ext/439174825/eeprom.p1 ${OBJECTDIR}/_ext/439174825/fuses.p1 ${OBJECTDIR}/_ext/439174825/spi.p1 ${OBJECTDIR}/_ext/439174825/spiram.p1 ${OBJECTDIR}/_ext/439174825/vs1011e.p1 ${OBJECTDIR}/_ext/141785504/ARP.p1 ${OBJECTDIR}/_ext/141785504/DHCP.p1 ${OBJECTDIR}/_ext/141785504/DNS.p1 ${OBJECTDIR}/_ext/141785504/Delay.p1 ${OBJECTDIR}/_ext/141785504/ETH97J60.p1 ${OBJECTDIR}/_ext/141785504/Hashes.p1 ${OBJECTDIR}/_ext/141785504/Helpers.p1 ${OBJECTDIR}/_ext/141785504/ICMP.p1 ${OBJECTDIR}/_ext/141785504/IP.p1 ${OBJECTDIR}/_ext/141785504/StackTsk.p1 ${OBJECTDIR}/_ext/141785504/TCP.p1 ${OBJECTDIR}/_ext/141785504/Tick.p1 ${OBJECTDIR}/_ext/141785504/UDP.p1 ${OBJECTDIR}/_ext/1472/appio.p1 ${OBJECTDIR}/_ext/1472/flasher.p1 ${OBJECTDIR}/_ext/1472/main.p1 ${OBJECTDIR}/_ext/1472/persistence.p1 ${OBJECTDIR}/_ext/1472/protocol.p1 ${OBJECTDIR}/_ext/1472/timers.p1
POSSIBLE_DEPFILES=${OBJECTDIR}/_ext/439174825/cm1602.p1.d ${OBJECTDIR}/_ext/439174825/eeprom.p1.d ${OBJECTDIR}/_ext/439174825/fuses.p1.d ${OBJECTDIR}/_ext/439174825/spi.p1.d ${OBJECTDIR}/_ext/439174825/spiram.p1.d ${OBJECTDIR}/_ext/439174825/vs1011e.p1.d ${OBJECTDIR}/_ext/141785504/ARP.p1.d ${OBJECTDIR}/_ext/141785504/DHCP.p1.d ${OBJECTDIR}/_ext/141785504/DNS.p1.d ${OBJECTDIR}/_ext/141785504/Delay.p1.d ${OBJECTDIR}/_ext/141785504/ETH97J60.p1.d ${OBJECTDIR}/_ext/141785504/Hashes.p1.d ${OBJECTDIR}/_ext/141785504/Helpers.p1.d ${OBJECTDIR}/_ext/141785504/ICMP.p1.d ${OBJECTDIR}/_ext/141785504/IP.p1.d ${OBJECTDIR}/_ext/141785504/StackTsk.p1.d ${OBJECTDIR}/_ext/141785504/TCP.p1.d ${OBJECTDIR}/_ext/141785504/Tick.p1.d ${OBJECTDIR}/_ext/141785504/UDP.p1.d ${OBJECTDIR}/_ext/1472/appio.p1.d ${OBJECTDIR}/_ext/1472/flasher.p1.d ${OBJECTDIR}/_ext/1472/main.p1.d ${OBJECTDIR}/_ext/1472/persistence.p1.d ${OBJECTDIR}/_ext/1472/protocol.p1.d ${OBJECTDIR}/_ext/1472/timers.p1.d

# Object Files
OBJECTFILES=${OBJECTDIR}/_ext/439174825/cm1602.p1 ${OBJECTDIR}/_ext/439174825/eeprom.p1 ${OBJECTDIR}/_ext/439174825/fuses.p1 ${OBJECTDIR}/_ext/439174825/spi.p1 ${OBJECTDIR}/_ext/439174825/spiram.p1 ${OBJECTDIR}/_ext/439174825/vs1011e.p1 ${OBJECTDIR}/_ext/141785504/ARP.p1 ${OBJECTDIR}/_ext/141785504/DHCP.p1 ${OBJECTDIR}/_ext/141785504/DNS.p1 ${OBJECTDIR}/_ext/141785504/Delay.p1 ${OBJECTDIR}/_ext/141785504/ETH97J60.p1 ${OBJECTDIR}/_ext/141785504/Hashes.p1 ${OBJECTDIR}/_ext/141785504/Helpers.p1 ${OBJECTDIR}/_ext/141785504/ICMP.p1 ${OBJECTDIR}/_ext/141785504/IP.p1 ${OBJECTDIR}/_ext/141785504/StackTsk.p1 ${OBJECTDIR}/_ext/141785504/TCP.p1 ${OBJECTDIR}/_ext/141785504/Tick.p1 ${OBJECTDIR}/_ext/141785504/UDP.p1 ${OBJECTDIR}/_ext/1472/appio.p1 ${OBJECTDIR}/_ext/1472/flasher.p1 ${OBJECTDIR}/_ext/1472/main.p1 ${OBJECTDIR}/_ext/1472/persistence.p1 ${OBJECTDIR}/_ext/1472/protocol.p1 ${OBJECTDIR}/_ext/1472/timers.p1

# Source Files
SOURCEFILES=../hardware/cm1602.c ../hardware/eeprom.c ../hardware/fuses.c ../hardware/spi.c ../hardware/spiram.c ../hardware/vs1011e.c ../tcpipstack/TCPIPStack/ARP.c ../tcpipstack/TCPIPStack/DHCP.c ../tcpipstack/TCPIPStack/DNS.c ../tcpipstack/TCPIPStack/Delay.c ../tcpipstack/TCPIPStack/ETH97J60.c ../tcpipstack/TCPIPStack/Hashes.c ../tcpipstack/TCPIPStack/Helpers.c ../tcpipstack/TCPIPStack/ICMP.c ../tcpipstack/TCPIPStack/IP.c ../tcpipstack/TCPIPStack/StackTsk.c ../tcpipstack/TCPIPStack/TCP.c ../tcpipstack/TCPIPStack/Tick.c ../tcpipstack/TCPIPStack/UDP.c ../appio.c ../flasher.c ../main.c ../persistence.c ../protocol.c ../timers.c


CFLAGS=
ASFLAGS=
LDLIBSOPTIONS=

############# Tool locations ##########################################
# If you copy a project from one host to another, the path where the  #
# compiler is installed may be different.                             #
# If you open this project with MPLAB X in the new host, this         #
# makefile will be regenerated and the paths will be corrected.       #
#######################################################################
# fixDeps replaces a bunch of sed/cat/printf statements that slow down the build
FIXDEPS=fixDeps

.build-conf:  ${BUILD_SUBPROJECTS}
	${MAKE} ${MAKE_OPTIONS} -f nbproject/Makefile-default.mk dist/${CND_CONF}/${IMAGE_TYPE}/bean.X.${IMAGE_TYPE}.${OUTPUT_SUFFIX}

MP_PROCESSOR_OPTION=18F87J60
# ------------------------------------------------------------------------------------
# Rules for buildStep: compile
ifeq ($(TYPE_IMAGE), DEBUG_RUN)
${OBJECTDIR}/_ext/439174825/cm1602.p1: ../hardware/cm1602.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/439174825 
	@${RM} ${OBJECTDIR}/_ext/439174825/cm1602.p1.d 
	@${RM} ${OBJECTDIR}/_ext/439174825/cm1602.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=none  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/439174825/cm1602.p1  ../hardware/cm1602.c 
	@-${MV} ${OBJECTDIR}/_ext/439174825/cm1602.d ${OBJECTDIR}/_ext/439174825/cm1602.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/439174825/cm1602.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/439174825/eeprom.p1: ../hardware/eeprom.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/439174825 
	@${RM} ${OBJECTDIR}/_ext/439174825/eeprom.p1.d 
	@${RM} ${OBJECTDIR}/_ext/439174825/eeprom.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=none  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/439174825/eeprom.p1  ../hardware/eeprom.c 
	@-${MV} ${OBJECTDIR}/_ext/439174825/eeprom.d ${OBJECTDIR}/_ext/439174825/eeprom.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/439174825/eeprom.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/439174825/fuses.p1: ../hardware/fuses.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/439174825 
	@${RM} ${OBJECTDIR}/_ext/439174825/fuses.p1.d 
	@${RM} ${OBJECTDIR}/_ext/439174825/fuses.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=none  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/439174825/fuses.p1  ../hardware/fuses.c 
	@-${MV} ${OBJECTDIR}/_ext/439174825/fuses.d ${OBJECTDIR}/_ext/439174825/fuses.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/439174825/fuses.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/439174825/spi.p1: ../hardware/spi.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/439174825 
	@${RM} ${OBJECTDIR}/_ext/439174825/spi.p1.d 
	@${RM} ${OBJECTDIR}/_ext/439174825/spi.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=none  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/439174825/spi.p1  ../hardware/spi.c 
	@-${MV} ${OBJECTDIR}/_ext/439174825/spi.d ${OBJECTDIR}/_ext/439174825/spi.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/439174825/spi.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/439174825/spiram.p1: ../hardware/spiram.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/439174825 
	@${RM} ${OBJECTDIR}/_ext/439174825/spiram.p1.d 
	@${RM} ${OBJECTDIR}/_ext/439174825/spiram.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=none  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/439174825/spiram.p1  ../hardware/spiram.c 
	@-${MV} ${OBJECTDIR}/_ext/439174825/spiram.d ${OBJECTDIR}/_ext/439174825/spiram.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/439174825/spiram.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/439174825/vs1011e.p1: ../hardware/vs1011e.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/439174825 
	@${RM} ${OBJECTDIR}/_ext/439174825/vs1011e.p1.d 
	@${RM} ${OBJECTDIR}/_ext/439174825/vs1011e.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=none  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/439174825/vs1011e.p1  ../hardware/vs1011e.c 
	@-${MV} ${OBJECTDIR}/_ext/439174825/vs1011e.d ${OBJECTDIR}/_ext/439174825/vs1011e.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/439174825/vs1011e.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/141785504/ARP.p1: ../tcpipstack/TCPIPStack/ARP.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/141785504 
	@${RM} ${OBJECTDIR}/_ext/141785504/ARP.p1.d 
	@${RM} ${OBJECTDIR}/_ext/141785504/ARP.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=none  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/141785504/ARP.p1  ../tcpipstack/TCPIPStack/ARP.c 
	@-${MV} ${OBJECTDIR}/_ext/141785504/ARP.d ${OBJECTDIR}/_ext/141785504/ARP.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/141785504/ARP.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/141785504/DHCP.p1: ../tcpipstack/TCPIPStack/DHCP.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/141785504 
	@${RM} ${OBJECTDIR}/_ext/141785504/DHCP.p1.d 
	@${RM} ${OBJECTDIR}/_ext/141785504/DHCP.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=none  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/141785504/DHCP.p1  ../tcpipstack/TCPIPStack/DHCP.c 
	@-${MV} ${OBJECTDIR}/_ext/141785504/DHCP.d ${OBJECTDIR}/_ext/141785504/DHCP.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/141785504/DHCP.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/141785504/DNS.p1: ../tcpipstack/TCPIPStack/DNS.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/141785504 
	@${RM} ${OBJECTDIR}/_ext/141785504/DNS.p1.d 
	@${RM} ${OBJECTDIR}/_ext/141785504/DNS.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=none  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/141785504/DNS.p1  ../tcpipstack/TCPIPStack/DNS.c 
	@-${MV} ${OBJECTDIR}/_ext/141785504/DNS.d ${OBJECTDIR}/_ext/141785504/DNS.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/141785504/DNS.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/141785504/Delay.p1: ../tcpipstack/TCPIPStack/Delay.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/141785504 
	@${RM} ${OBJECTDIR}/_ext/141785504/Delay.p1.d 
	@${RM} ${OBJECTDIR}/_ext/141785504/Delay.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=none  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/141785504/Delay.p1  ../tcpipstack/TCPIPStack/Delay.c 
	@-${MV} ${OBJECTDIR}/_ext/141785504/Delay.d ${OBJECTDIR}/_ext/141785504/Delay.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/141785504/Delay.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/141785504/ETH97J60.p1: ../tcpipstack/TCPIPStack/ETH97J60.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/141785504 
	@${RM} ${OBJECTDIR}/_ext/141785504/ETH97J60.p1.d 
	@${RM} ${OBJECTDIR}/_ext/141785504/ETH97J60.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=none  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/141785504/ETH97J60.p1  ../tcpipstack/TCPIPStack/ETH97J60.c 
	@-${MV} ${OBJECTDIR}/_ext/141785504/ETH97J60.d ${OBJECTDIR}/_ext/141785504/ETH97J60.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/141785504/ETH97J60.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/141785504/Hashes.p1: ../tcpipstack/TCPIPStack/Hashes.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/141785504 
	@${RM} ${OBJECTDIR}/_ext/141785504/Hashes.p1.d 
	@${RM} ${OBJECTDIR}/_ext/141785504/Hashes.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=none  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/141785504/Hashes.p1  ../tcpipstack/TCPIPStack/Hashes.c 
	@-${MV} ${OBJECTDIR}/_ext/141785504/Hashes.d ${OBJECTDIR}/_ext/141785504/Hashes.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/141785504/Hashes.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/141785504/Helpers.p1: ../tcpipstack/TCPIPStack/Helpers.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/141785504 
	@${RM} ${OBJECTDIR}/_ext/141785504/Helpers.p1.d 
	@${RM} ${OBJECTDIR}/_ext/141785504/Helpers.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=none  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/141785504/Helpers.p1  ../tcpipstack/TCPIPStack/Helpers.c 
	@-${MV} ${OBJECTDIR}/_ext/141785504/Helpers.d ${OBJECTDIR}/_ext/141785504/Helpers.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/141785504/Helpers.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/141785504/ICMP.p1: ../tcpipstack/TCPIPStack/ICMP.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/141785504 
	@${RM} ${OBJECTDIR}/_ext/141785504/ICMP.p1.d 
	@${RM} ${OBJECTDIR}/_ext/141785504/ICMP.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=none  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/141785504/ICMP.p1  ../tcpipstack/TCPIPStack/ICMP.c 
	@-${MV} ${OBJECTDIR}/_ext/141785504/ICMP.d ${OBJECTDIR}/_ext/141785504/ICMP.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/141785504/ICMP.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/141785504/IP.p1: ../tcpipstack/TCPIPStack/IP.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/141785504 
	@${RM} ${OBJECTDIR}/_ext/141785504/IP.p1.d 
	@${RM} ${OBJECTDIR}/_ext/141785504/IP.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=none  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/141785504/IP.p1  ../tcpipstack/TCPIPStack/IP.c 
	@-${MV} ${OBJECTDIR}/_ext/141785504/IP.d ${OBJECTDIR}/_ext/141785504/IP.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/141785504/IP.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/141785504/StackTsk.p1: ../tcpipstack/TCPIPStack/StackTsk.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/141785504 
	@${RM} ${OBJECTDIR}/_ext/141785504/StackTsk.p1.d 
	@${RM} ${OBJECTDIR}/_ext/141785504/StackTsk.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=none  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/141785504/StackTsk.p1  ../tcpipstack/TCPIPStack/StackTsk.c 
	@-${MV} ${OBJECTDIR}/_ext/141785504/StackTsk.d ${OBJECTDIR}/_ext/141785504/StackTsk.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/141785504/StackTsk.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/141785504/TCP.p1: ../tcpipstack/TCPIPStack/TCP.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/141785504 
	@${RM} ${OBJECTDIR}/_ext/141785504/TCP.p1.d 
	@${RM} ${OBJECTDIR}/_ext/141785504/TCP.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=none  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/141785504/TCP.p1  ../tcpipstack/TCPIPStack/TCP.c 
	@-${MV} ${OBJECTDIR}/_ext/141785504/TCP.d ${OBJECTDIR}/_ext/141785504/TCP.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/141785504/TCP.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/141785504/Tick.p1: ../tcpipstack/TCPIPStack/Tick.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/141785504 
	@${RM} ${OBJECTDIR}/_ext/141785504/Tick.p1.d 
	@${RM} ${OBJECTDIR}/_ext/141785504/Tick.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=none  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/141785504/Tick.p1  ../tcpipstack/TCPIPStack/Tick.c 
	@-${MV} ${OBJECTDIR}/_ext/141785504/Tick.d ${OBJECTDIR}/_ext/141785504/Tick.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/141785504/Tick.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/141785504/UDP.p1: ../tcpipstack/TCPIPStack/UDP.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/141785504 
	@${RM} ${OBJECTDIR}/_ext/141785504/UDP.p1.d 
	@${RM} ${OBJECTDIR}/_ext/141785504/UDP.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=none  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/141785504/UDP.p1  ../tcpipstack/TCPIPStack/UDP.c 
	@-${MV} ${OBJECTDIR}/_ext/141785504/UDP.d ${OBJECTDIR}/_ext/141785504/UDP.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/141785504/UDP.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/1472/appio.p1: ../appio.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/1472 
	@${RM} ${OBJECTDIR}/_ext/1472/appio.p1.d 
	@${RM} ${OBJECTDIR}/_ext/1472/appio.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=none  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/1472/appio.p1  ../appio.c 
	@-${MV} ${OBJECTDIR}/_ext/1472/appio.d ${OBJECTDIR}/_ext/1472/appio.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/1472/appio.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/1472/flasher.p1: ../flasher.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/1472 
	@${RM} ${OBJECTDIR}/_ext/1472/flasher.p1.d 
	@${RM} ${OBJECTDIR}/_ext/1472/flasher.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=none  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/1472/flasher.p1  ../flasher.c 
	@-${MV} ${OBJECTDIR}/_ext/1472/flasher.d ${OBJECTDIR}/_ext/1472/flasher.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/1472/flasher.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/1472/main.p1: ../main.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/1472 
	@${RM} ${OBJECTDIR}/_ext/1472/main.p1.d 
	@${RM} ${OBJECTDIR}/_ext/1472/main.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=none  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/1472/main.p1  ../main.c 
	@-${MV} ${OBJECTDIR}/_ext/1472/main.d ${OBJECTDIR}/_ext/1472/main.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/1472/main.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/1472/persistence.p1: ../persistence.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/1472 
	@${RM} ${OBJECTDIR}/_ext/1472/persistence.p1.d 
	@${RM} ${OBJECTDIR}/_ext/1472/persistence.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=none  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/1472/persistence.p1  ../persistence.c 
	@-${MV} ${OBJECTDIR}/_ext/1472/persistence.d ${OBJECTDIR}/_ext/1472/persistence.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/1472/persistence.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/1472/protocol.p1: ../protocol.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/1472 
	@${RM} ${OBJECTDIR}/_ext/1472/protocol.p1.d 
	@${RM} ${OBJECTDIR}/_ext/1472/protocol.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=none  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/1472/protocol.p1  ../protocol.c 
	@-${MV} ${OBJECTDIR}/_ext/1472/protocol.d ${OBJECTDIR}/_ext/1472/protocol.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/1472/protocol.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/1472/timers.p1: ../timers.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/1472 
	@${RM} ${OBJECTDIR}/_ext/1472/timers.p1.d 
	@${RM} ${OBJECTDIR}/_ext/1472/timers.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  -D__DEBUG=1 --debugger=none  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/1472/timers.p1  ../timers.c 
	@-${MV} ${OBJECTDIR}/_ext/1472/timers.d ${OBJECTDIR}/_ext/1472/timers.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/1472/timers.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
else
${OBJECTDIR}/_ext/439174825/cm1602.p1: ../hardware/cm1602.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/439174825 
	@${RM} ${OBJECTDIR}/_ext/439174825/cm1602.p1.d 
	@${RM} ${OBJECTDIR}/_ext/439174825/cm1602.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/439174825/cm1602.p1  ../hardware/cm1602.c 
	@-${MV} ${OBJECTDIR}/_ext/439174825/cm1602.d ${OBJECTDIR}/_ext/439174825/cm1602.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/439174825/cm1602.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/439174825/eeprom.p1: ../hardware/eeprom.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/439174825 
	@${RM} ${OBJECTDIR}/_ext/439174825/eeprom.p1.d 
	@${RM} ${OBJECTDIR}/_ext/439174825/eeprom.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/439174825/eeprom.p1  ../hardware/eeprom.c 
	@-${MV} ${OBJECTDIR}/_ext/439174825/eeprom.d ${OBJECTDIR}/_ext/439174825/eeprom.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/439174825/eeprom.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/439174825/fuses.p1: ../hardware/fuses.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/439174825 
	@${RM} ${OBJECTDIR}/_ext/439174825/fuses.p1.d 
	@${RM} ${OBJECTDIR}/_ext/439174825/fuses.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/439174825/fuses.p1  ../hardware/fuses.c 
	@-${MV} ${OBJECTDIR}/_ext/439174825/fuses.d ${OBJECTDIR}/_ext/439174825/fuses.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/439174825/fuses.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/439174825/spi.p1: ../hardware/spi.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/439174825 
	@${RM} ${OBJECTDIR}/_ext/439174825/spi.p1.d 
	@${RM} ${OBJECTDIR}/_ext/439174825/spi.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/439174825/spi.p1  ../hardware/spi.c 
	@-${MV} ${OBJECTDIR}/_ext/439174825/spi.d ${OBJECTDIR}/_ext/439174825/spi.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/439174825/spi.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/439174825/spiram.p1: ../hardware/spiram.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/439174825 
	@${RM} ${OBJECTDIR}/_ext/439174825/spiram.p1.d 
	@${RM} ${OBJECTDIR}/_ext/439174825/spiram.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/439174825/spiram.p1  ../hardware/spiram.c 
	@-${MV} ${OBJECTDIR}/_ext/439174825/spiram.d ${OBJECTDIR}/_ext/439174825/spiram.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/439174825/spiram.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/439174825/vs1011e.p1: ../hardware/vs1011e.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/439174825 
	@${RM} ${OBJECTDIR}/_ext/439174825/vs1011e.p1.d 
	@${RM} ${OBJECTDIR}/_ext/439174825/vs1011e.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/439174825/vs1011e.p1  ../hardware/vs1011e.c 
	@-${MV} ${OBJECTDIR}/_ext/439174825/vs1011e.d ${OBJECTDIR}/_ext/439174825/vs1011e.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/439174825/vs1011e.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/141785504/ARP.p1: ../tcpipstack/TCPIPStack/ARP.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/141785504 
	@${RM} ${OBJECTDIR}/_ext/141785504/ARP.p1.d 
	@${RM} ${OBJECTDIR}/_ext/141785504/ARP.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/141785504/ARP.p1  ../tcpipstack/TCPIPStack/ARP.c 
	@-${MV} ${OBJECTDIR}/_ext/141785504/ARP.d ${OBJECTDIR}/_ext/141785504/ARP.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/141785504/ARP.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/141785504/DHCP.p1: ../tcpipstack/TCPIPStack/DHCP.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/141785504 
	@${RM} ${OBJECTDIR}/_ext/141785504/DHCP.p1.d 
	@${RM} ${OBJECTDIR}/_ext/141785504/DHCP.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/141785504/DHCP.p1  ../tcpipstack/TCPIPStack/DHCP.c 
	@-${MV} ${OBJECTDIR}/_ext/141785504/DHCP.d ${OBJECTDIR}/_ext/141785504/DHCP.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/141785504/DHCP.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/141785504/DNS.p1: ../tcpipstack/TCPIPStack/DNS.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/141785504 
	@${RM} ${OBJECTDIR}/_ext/141785504/DNS.p1.d 
	@${RM} ${OBJECTDIR}/_ext/141785504/DNS.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/141785504/DNS.p1  ../tcpipstack/TCPIPStack/DNS.c 
	@-${MV} ${OBJECTDIR}/_ext/141785504/DNS.d ${OBJECTDIR}/_ext/141785504/DNS.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/141785504/DNS.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/141785504/Delay.p1: ../tcpipstack/TCPIPStack/Delay.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/141785504 
	@${RM} ${OBJECTDIR}/_ext/141785504/Delay.p1.d 
	@${RM} ${OBJECTDIR}/_ext/141785504/Delay.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/141785504/Delay.p1  ../tcpipstack/TCPIPStack/Delay.c 
	@-${MV} ${OBJECTDIR}/_ext/141785504/Delay.d ${OBJECTDIR}/_ext/141785504/Delay.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/141785504/Delay.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/141785504/ETH97J60.p1: ../tcpipstack/TCPIPStack/ETH97J60.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/141785504 
	@${RM} ${OBJECTDIR}/_ext/141785504/ETH97J60.p1.d 
	@${RM} ${OBJECTDIR}/_ext/141785504/ETH97J60.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/141785504/ETH97J60.p1  ../tcpipstack/TCPIPStack/ETH97J60.c 
	@-${MV} ${OBJECTDIR}/_ext/141785504/ETH97J60.d ${OBJECTDIR}/_ext/141785504/ETH97J60.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/141785504/ETH97J60.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/141785504/Hashes.p1: ../tcpipstack/TCPIPStack/Hashes.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/141785504 
	@${RM} ${OBJECTDIR}/_ext/141785504/Hashes.p1.d 
	@${RM} ${OBJECTDIR}/_ext/141785504/Hashes.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/141785504/Hashes.p1  ../tcpipstack/TCPIPStack/Hashes.c 
	@-${MV} ${OBJECTDIR}/_ext/141785504/Hashes.d ${OBJECTDIR}/_ext/141785504/Hashes.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/141785504/Hashes.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/141785504/Helpers.p1: ../tcpipstack/TCPIPStack/Helpers.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/141785504 
	@${RM} ${OBJECTDIR}/_ext/141785504/Helpers.p1.d 
	@${RM} ${OBJECTDIR}/_ext/141785504/Helpers.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/141785504/Helpers.p1  ../tcpipstack/TCPIPStack/Helpers.c 
	@-${MV} ${OBJECTDIR}/_ext/141785504/Helpers.d ${OBJECTDIR}/_ext/141785504/Helpers.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/141785504/Helpers.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/141785504/ICMP.p1: ../tcpipstack/TCPIPStack/ICMP.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/141785504 
	@${RM} ${OBJECTDIR}/_ext/141785504/ICMP.p1.d 
	@${RM} ${OBJECTDIR}/_ext/141785504/ICMP.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/141785504/ICMP.p1  ../tcpipstack/TCPIPStack/ICMP.c 
	@-${MV} ${OBJECTDIR}/_ext/141785504/ICMP.d ${OBJECTDIR}/_ext/141785504/ICMP.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/141785504/ICMP.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/141785504/IP.p1: ../tcpipstack/TCPIPStack/IP.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/141785504 
	@${RM} ${OBJECTDIR}/_ext/141785504/IP.p1.d 
	@${RM} ${OBJECTDIR}/_ext/141785504/IP.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/141785504/IP.p1  ../tcpipstack/TCPIPStack/IP.c 
	@-${MV} ${OBJECTDIR}/_ext/141785504/IP.d ${OBJECTDIR}/_ext/141785504/IP.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/141785504/IP.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/141785504/StackTsk.p1: ../tcpipstack/TCPIPStack/StackTsk.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/141785504 
	@${RM} ${OBJECTDIR}/_ext/141785504/StackTsk.p1.d 
	@${RM} ${OBJECTDIR}/_ext/141785504/StackTsk.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/141785504/StackTsk.p1  ../tcpipstack/TCPIPStack/StackTsk.c 
	@-${MV} ${OBJECTDIR}/_ext/141785504/StackTsk.d ${OBJECTDIR}/_ext/141785504/StackTsk.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/141785504/StackTsk.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/141785504/TCP.p1: ../tcpipstack/TCPIPStack/TCP.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/141785504 
	@${RM} ${OBJECTDIR}/_ext/141785504/TCP.p1.d 
	@${RM} ${OBJECTDIR}/_ext/141785504/TCP.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/141785504/TCP.p1  ../tcpipstack/TCPIPStack/TCP.c 
	@-${MV} ${OBJECTDIR}/_ext/141785504/TCP.d ${OBJECTDIR}/_ext/141785504/TCP.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/141785504/TCP.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/141785504/Tick.p1: ../tcpipstack/TCPIPStack/Tick.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/141785504 
	@${RM} ${OBJECTDIR}/_ext/141785504/Tick.p1.d 
	@${RM} ${OBJECTDIR}/_ext/141785504/Tick.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/141785504/Tick.p1  ../tcpipstack/TCPIPStack/Tick.c 
	@-${MV} ${OBJECTDIR}/_ext/141785504/Tick.d ${OBJECTDIR}/_ext/141785504/Tick.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/141785504/Tick.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/141785504/UDP.p1: ../tcpipstack/TCPIPStack/UDP.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/141785504 
	@${RM} ${OBJECTDIR}/_ext/141785504/UDP.p1.d 
	@${RM} ${OBJECTDIR}/_ext/141785504/UDP.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/141785504/UDP.p1  ../tcpipstack/TCPIPStack/UDP.c 
	@-${MV} ${OBJECTDIR}/_ext/141785504/UDP.d ${OBJECTDIR}/_ext/141785504/UDP.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/141785504/UDP.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/1472/appio.p1: ../appio.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/1472 
	@${RM} ${OBJECTDIR}/_ext/1472/appio.p1.d 
	@${RM} ${OBJECTDIR}/_ext/1472/appio.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/1472/appio.p1  ../appio.c 
	@-${MV} ${OBJECTDIR}/_ext/1472/appio.d ${OBJECTDIR}/_ext/1472/appio.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/1472/appio.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/1472/flasher.p1: ../flasher.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/1472 
	@${RM} ${OBJECTDIR}/_ext/1472/flasher.p1.d 
	@${RM} ${OBJECTDIR}/_ext/1472/flasher.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/1472/flasher.p1  ../flasher.c 
	@-${MV} ${OBJECTDIR}/_ext/1472/flasher.d ${OBJECTDIR}/_ext/1472/flasher.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/1472/flasher.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/1472/main.p1: ../main.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/1472 
	@${RM} ${OBJECTDIR}/_ext/1472/main.p1.d 
	@${RM} ${OBJECTDIR}/_ext/1472/main.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/1472/main.p1  ../main.c 
	@-${MV} ${OBJECTDIR}/_ext/1472/main.d ${OBJECTDIR}/_ext/1472/main.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/1472/main.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/1472/persistence.p1: ../persistence.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/1472 
	@${RM} ${OBJECTDIR}/_ext/1472/persistence.p1.d 
	@${RM} ${OBJECTDIR}/_ext/1472/persistence.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/1472/persistence.p1  ../persistence.c 
	@-${MV} ${OBJECTDIR}/_ext/1472/persistence.d ${OBJECTDIR}/_ext/1472/persistence.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/1472/persistence.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/1472/protocol.p1: ../protocol.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/1472 
	@${RM} ${OBJECTDIR}/_ext/1472/protocol.p1.d 
	@${RM} ${OBJECTDIR}/_ext/1472/protocol.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/1472/protocol.p1  ../protocol.c 
	@-${MV} ${OBJECTDIR}/_ext/1472/protocol.d ${OBJECTDIR}/_ext/1472/protocol.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/1472/protocol.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
${OBJECTDIR}/_ext/1472/timers.p1: ../timers.c  nbproject/Makefile-${CND_CONF}.mk
	@${MKDIR} ${OBJECTDIR}/_ext/1472 
	@${RM} ${OBJECTDIR}/_ext/1472/timers.p1.d 
	@${RM} ${OBJECTDIR}/_ext/1472/timers.p1 
	${MP_CC} --pass1 $(MP_EXTRA_CC_PRE) --chip=$(MP_PROCESSOR_OPTION) -Q -G  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: (%%n) %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s" --MSGDISABLE=1427    -o${OBJECTDIR}/_ext/1472/timers.p1  ../timers.c 
	@-${MV} ${OBJECTDIR}/_ext/1472/timers.d ${OBJECTDIR}/_ext/1472/timers.p1.d 
	@${FIXDEPS} ${OBJECTDIR}/_ext/1472/timers.p1.d $(SILENT) -rsi ${MP_CC_DIR}../  
	
endif

# ------------------------------------------------------------------------------------
# Rules for buildStep: assemble
ifeq ($(TYPE_IMAGE), DEBUG_RUN)
else
endif

# ------------------------------------------------------------------------------------
# Rules for buildStep: link
ifeq ($(TYPE_IMAGE), DEBUG_RUN)
dist/${CND_CONF}/${IMAGE_TYPE}/bean.X.${IMAGE_TYPE}.${OUTPUT_SUFFIX}: ${OBJECTFILES}  nbproject/Makefile-${CND_CONF}.mk    
	@${MKDIR} dist/${CND_CONF}/${IMAGE_TYPE} 
	${MP_CC} $(MP_EXTRA_LD_PRE) --chip=$(MP_PROCESSOR_OPTION) -G -mdist/${CND_CONF}/${IMAGE_TYPE}/bean.X.${IMAGE_TYPE}.map  -D__DEBUG=1 --debugger=none  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s"        -odist/${CND_CONF}/${IMAGE_TYPE}/bean.X.${IMAGE_TYPE}.${DEBUGGABLE_SUFFIX}  ${OBJECTFILES_QUOTED_IF_SPACED}     
	@${RM} dist/${CND_CONF}/${IMAGE_TYPE}/bean.X.${IMAGE_TYPE}.hex 
	
else
dist/${CND_CONF}/${IMAGE_TYPE}/bean.X.${IMAGE_TYPE}.${OUTPUT_SUFFIX}: ${OBJECTFILES}  nbproject/Makefile-${CND_CONF}.mk   
	@${MKDIR} dist/${CND_CONF}/${IMAGE_TYPE} 
	${MP_CC} $(MP_EXTRA_LD_PRE) --chip=$(MP_PROCESSOR_OPTION) -G -mdist/${CND_CONF}/${IMAGE_TYPE}/bean.X.${IMAGE_TYPE}.map  --double=32 --float=32 --emi=wordwrite --opt=default,+asm,+asmfile,-speed,+space,-debug --addrqual=ignore --mode=free -P -N255 -I"../tcpipstack" -I"../tcpipstack/Include" -I"../tcpipstack/Include/TCPIPStack" --warn=0 --cci --asmlist --summary=default,-psect,-class,+mem,-hex,-file --output=default,-inhx032 --runtime=default,+clear,+init,-keep,-no_startup,-download,+config,+clib,+plib --output=-mcof,+elf:multilocs --stack=compiled:auto:auto:auto "--errformat=%%f:%%l: error: %%s" "--warnformat=%%f:%%l: warning: (%%n) %%s" "--msgformat=%%f:%%l: advisory: (%%n) %%s"     -odist/${CND_CONF}/${IMAGE_TYPE}/bean.X.${IMAGE_TYPE}.${DEBUGGABLE_SUFFIX}  ${OBJECTFILES_QUOTED_IF_SPACED}     
	
endif


# Subprojects
.build-subprojects:


# Subprojects
.clean-subprojects:

# Clean Targets
.clean-conf: ${CLEAN_SUBPROJECTS}
	${RM} -r build/default
	${RM} -r dist/default

# Enable dependency checking
.dep.inc: .depcheck-impl

DEPFILES=$(shell mplabwildcard ${POSSIBLE_DEPFILES})
ifneq (${DEPFILES},)
include ${DEPFILES}
endif
