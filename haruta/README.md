
====

Overview

## Description
This program can be used when you want to receive EPCs from RFID Reader.
As this program is under construction, please modify freely.
In the near future, we plan to build psql server on SASASE LAB's network.

## Requirement
psql, python 2.7, psycopg2 (python module)

## Usage
$python server.py

## Install
Please search and setup if you are windows user because I have only mac.
I think the fundamental setup flow is same, so please replace the path etc. for your environment. 

The explanation below are for mac (unix/linux) user.

1. PSQL install
You can use brew. The following command can be used.

$brew install psql

After installation, you have to initialize your psql. Please execute the following command.

$initdb /usr/local/var/postgres -E utf8

Next, you have to start psql and create DB. Please execute the following command.
For starting psql.
$ pg_ctl start -l logfile
(For stopping the psql service, $pg_ctl stop)

For creating DB named 'wine'.
$ createdb wine

In this time, you should register wine data, psql command can be used.

$psql wine

You can go to administrator's display and execute standard sql command.
Maybe your command line is like:

wine=#

To create table which is used for wineDB, please input the following command.

wine=# create table wine_database(id int primary key, epc varchar(1024), url varchar(1024));

After that, please download fixed_db.csv and copy data to table by the following command. Please change the path for your environment.

wine=# COPY table FROM '/path/to/fixed_db.csv' WITH CSV

You are ready to use server.py. (Maybe)
For python module installation, please execute the following command. 

$pip install psycopg2

Finally, in the server.py, please change 'psyconfig' and 'host' according to your environment.

## Contribution

## Licence

MIT Licence

## Author

[Shuichiro Haruta](haruta@sasase.ics.keio.ac.jp)
