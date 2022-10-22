cmake_minimum_required(VERSION 3.0.0)
project(netmaster-linux VERSION 0.1.0)

include(CTest)
enable_testing()

add_definitions(-D_CONF_LINUX)

file(GLOB COMMON_SRC "../../src/nodes/*.c")
file(GLOB LINUX_SRC "../../src/nodes/hardware/linux/*.c")
file(GLOB RASPBERRY_SRC "../../src/nodes/hardware/linux/raspbian/*.c")
add_executable(netmaster-linux ${LINUX_SRC} ${COMMON_SRC} ${RASPBERRY_SRC} sinks.c main.c)
target_link_libraries(netmaster-linux m)
target_compile_options(netmaster-linux PRIVATE -Wno-switch -Wno-duplicate-decl-specifier -Wno-main-return-type)

set(CPACK_PROJECT_NAME ${PROJECT_NAME})
set(CPACK_PROJECT_VERSION ${PROJECT_VERSION})
include(CPack)
