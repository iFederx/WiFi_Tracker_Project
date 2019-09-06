﻿create table if not exists rooms (roomname varchar, xlength real, ylength real, pcount int);
create table if not exists stations (namemac varchar, roomname varchar, xpos real, ypos real, shortintrp varchar, longintrp varchar);
create table if not exists devicespositions (identifier varchar, mac varchar, roomname varchar, xpos real, ypos real, tm timestamp, outmovement bool);
create table if not exists requestedssids (identifier varchar, ssid varchar);
create table if not exists countstats (roomname varchar, count real, tm timestamp);

alter table requestedssids add constraint pc unique (identifier, ssid);
alter table rooms add constraint rnuq unique(roomname);
alter table stations add constraint stuq unique (namemac);