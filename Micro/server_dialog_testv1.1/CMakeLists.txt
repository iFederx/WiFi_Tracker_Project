cmake_minimum_required(VERSION 3.9)
project(sync_dialog)

set(CMAKE_CXX_STANDARD 11)

add_executable(sync_dialog
        Client/Client.c
        Client/Client_register.c
        Client/Client_timestamp.c
        Server/Server_register.c
        Server/Server_timestamp.c
        errlib.c
        errlib.h
        sockwrap.c
        sockwrap.h)
