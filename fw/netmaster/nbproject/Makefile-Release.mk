#
# Generated Makefile - do not edit!
#
# Edit the Makefile in the project folder instead (../Makefile). Each target
# has a -pre and a -post target defined where you can add customized code.
#
# This makefile implements configuration specific macros and targets.


# Environment
MKDIR=mkdir
CP=cp
GREP=grep
NM=nm
CCADMIN=CCadmin
RANLIB=ranlib
CC=gcc
CCC=g++
CXX=g++
FC=gfortran
AS=as

# Macros
CND_PLATFORM=GNU-Windows
CND_DLIB_EXT=dll
CND_CONF=Release
CND_DISTDIR=dist
CND_BUILDDIR=build

# Include project Makefile
include Makefile

# Object Directory
OBJECTDIR=${CND_BUILDDIR}/${CND_CONF}/${CND_PLATFORM}

# Object Files
OBJECTFILES= \
	${OBJECTDIR}/_ext/5c0/appio.o \
	${OBJECTDIR}/_ext/5c0/bus_server.o \
	${OBJECTDIR}/_ext/5c0/displaySink.o \
	${OBJECTDIR}/_ext/e5d2b957/hw_raspbian.o \
	${OBJECTDIR}/_ext/e5d2b957/ip_raspbian.o \
	${OBJECTDIR}/_ext/e5d2b957/tick_raspbian.o \
	${OBJECTDIR}/_ext/e5d2b957/uart_raspbian.o \
	${OBJECTDIR}/_ext/5c0/ip_client.o \
	${OBJECTDIR}/_ext/5c0/main.o \
	${OBJECTDIR}/_ext/5c0/persistence.o \
	${OBJECTDIR}/_ext/5c0/protocol.o \
	${OBJECTDIR}/_ext/5c0/rs485.o \
	${OBJECTDIR}/_ext/5c0/sinks.o


# C Compiler Flags
CFLAGS=

# CC Compiler Flags
CCFLAGS=
CXXFLAGS=

# Fortran Compiler Flags
FFLAGS=

# Assembler Flags
ASFLAGS=

# Link Libraries and Options
LDLIBSOPTIONS=

# Build Targets
.build-conf: ${BUILD_SUBPROJECTS}
	"${MAKE}"  -f nbproject/Makefile-${CND_CONF}.mk ${CND_DISTDIR}/${CND_CONF}/${CND_PLATFORM}/netmaster.exe

${CND_DISTDIR}/${CND_CONF}/${CND_PLATFORM}/netmaster.exe: ${OBJECTFILES}
	${MKDIR} -p ${CND_DISTDIR}/${CND_CONF}/${CND_PLATFORM}
	${LINK.c} -o ${CND_DISTDIR}/${CND_CONF}/${CND_PLATFORM}/netmaster ${OBJECTFILES} ${LDLIBSOPTIONS}

${OBJECTDIR}/_ext/5c0/appio.o: ../appio.c
	${MKDIR} -p ${OBJECTDIR}/_ext/5c0
	${RM} "$@.d"
	$(COMPILE.c) -O2 -MMD -MP -MF "$@.d" -o ${OBJECTDIR}/_ext/5c0/appio.o ../appio.c

${OBJECTDIR}/_ext/5c0/bus_server.o: ../bus_server.c
	${MKDIR} -p ${OBJECTDIR}/_ext/5c0
	${RM} "$@.d"
	$(COMPILE.c) -O2 -MMD -MP -MF "$@.d" -o ${OBJECTDIR}/_ext/5c0/bus_server.o ../bus_server.c

${OBJECTDIR}/_ext/5c0/displaySink.o: ../displaySink.c
	${MKDIR} -p ${OBJECTDIR}/_ext/5c0
	${RM} "$@.d"
	$(COMPILE.c) -O2 -MMD -MP -MF "$@.d" -o ${OBJECTDIR}/_ext/5c0/displaySink.o ../displaySink.c

${OBJECTDIR}/_ext/e5d2b957/hw_raspbian.o: ../hardware/hw_raspbian.c
	${MKDIR} -p ${OBJECTDIR}/_ext/e5d2b957
	${RM} "$@.d"
	$(COMPILE.c) -O2 -MMD -MP -MF "$@.d" -o ${OBJECTDIR}/_ext/e5d2b957/hw_raspbian.o ../hardware/hw_raspbian.c

${OBJECTDIR}/_ext/e5d2b957/ip_raspbian.o: ../hardware/ip_raspbian.c
	${MKDIR} -p ${OBJECTDIR}/_ext/e5d2b957
	${RM} "$@.d"
	$(COMPILE.c) -O2 -MMD -MP -MF "$@.d" -o ${OBJECTDIR}/_ext/e5d2b957/ip_raspbian.o ../hardware/ip_raspbian.c

${OBJECTDIR}/_ext/e5d2b957/tick_raspbian.o: ../hardware/tick_raspbian.c
	${MKDIR} -p ${OBJECTDIR}/_ext/e5d2b957
	${RM} "$@.d"
	$(COMPILE.c) -O2 -MMD -MP -MF "$@.d" -o ${OBJECTDIR}/_ext/e5d2b957/tick_raspbian.o ../hardware/tick_raspbian.c

${OBJECTDIR}/_ext/e5d2b957/uart_raspbian.o: ../hardware/uart_raspbian.c
	${MKDIR} -p ${OBJECTDIR}/_ext/e5d2b957
	${RM} "$@.d"
	$(COMPILE.c) -O2 -MMD -MP -MF "$@.d" -o ${OBJECTDIR}/_ext/e5d2b957/uart_raspbian.o ../hardware/uart_raspbian.c

${OBJECTDIR}/_ext/5c0/ip_client.o: ../ip_client.c
	${MKDIR} -p ${OBJECTDIR}/_ext/5c0
	${RM} "$@.d"
	$(COMPILE.c) -O2 -MMD -MP -MF "$@.d" -o ${OBJECTDIR}/_ext/5c0/ip_client.o ../ip_client.c

${OBJECTDIR}/_ext/5c0/main.o: ../main.c
	${MKDIR} -p ${OBJECTDIR}/_ext/5c0
	${RM} "$@.d"
	$(COMPILE.c) -O2 -MMD -MP -MF "$@.d" -o ${OBJECTDIR}/_ext/5c0/main.o ../main.c

${OBJECTDIR}/_ext/5c0/persistence.o: ../persistence.c
	${MKDIR} -p ${OBJECTDIR}/_ext/5c0
	${RM} "$@.d"
	$(COMPILE.c) -O2 -MMD -MP -MF "$@.d" -o ${OBJECTDIR}/_ext/5c0/persistence.o ../persistence.c

${OBJECTDIR}/_ext/5c0/protocol.o: ../protocol.c
	${MKDIR} -p ${OBJECTDIR}/_ext/5c0
	${RM} "$@.d"
	$(COMPILE.c) -O2 -MMD -MP -MF "$@.d" -o ${OBJECTDIR}/_ext/5c0/protocol.o ../protocol.c

${OBJECTDIR}/_ext/5c0/rs485.o: ../rs485.c
	${MKDIR} -p ${OBJECTDIR}/_ext/5c0
	${RM} "$@.d"
	$(COMPILE.c) -O2 -MMD -MP -MF "$@.d" -o ${OBJECTDIR}/_ext/5c0/rs485.o ../rs485.c

${OBJECTDIR}/_ext/5c0/sinks.o: ../sinks.c
	${MKDIR} -p ${OBJECTDIR}/_ext/5c0
	${RM} "$@.d"
	$(COMPILE.c) -O2 -MMD -MP -MF "$@.d" -o ${OBJECTDIR}/_ext/5c0/sinks.o ../sinks.c

# Subprojects
.build-subprojects:

# Clean Targets
.clean-conf: ${CLEAN_SUBPROJECTS}
	${RM} -r ${CND_BUILDDIR}/${CND_CONF}

# Subprojects
.clean-subprojects:

# Enable dependency checking
.dep.inc: .depcheck-impl

include .dep.inc
