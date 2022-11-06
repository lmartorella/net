cmake_minimum_required(VERSION 3.0.0)
project(netmaster-linux VERSION 0.1.0)

include(CTest)
enable_testing()

add_definitions(-D_CONF_POSIX -DHAS_RS485_BUS_PRIMARY)

add_executable(netmaster-linux sinks.c main.c)
target_link_libraries(netmaster-linux net m)
target_compile_options(netmaster-linux PRIVATE -Wno-switch -Wno-duplicate-decl-specifier -Wno-main-return-type)

set(CPACK_PROJECT_NAME ${PROJECT_NAME})
set(CPACK_PROJECT_VERSION ${PROJECT_VERSION})
include(CPack)
