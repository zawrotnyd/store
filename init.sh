#!/bin/sh
cmd="psql --quiet -U postgres -d dude"
dropdb --if-exists dude
createdb -O postgres dude
$cmd -f ../tables.sql
$cmd -f ../test_data.sql

#sh reset.sh

