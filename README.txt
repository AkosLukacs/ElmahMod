                                 ELMAH README

Upgrading

   If  you are using the Microsoft SQL Server for your error log then you
   should  re-create  the  ELMAH_Error  table,  its  indicies and related
   stored  procedures  using the supplied SQL script (see Database.sql in
   your distribution). The script does not contain DDL DROP statements so
   you  will have to drop the table and stored procedures manually before
   applying  the  script.  If you wish to preserve the logged error data,
   you should consider archiving it in a backup.

   If  you are using Oracle for your error log then you should  re-create
   the  ELMAH$Error  table,  its  indicies and related package using  the
   supplied SQL script (see Oracle.sql in your distribution). The  script
   does  not contain  any DROP  statements so  you  will have to drop the 
   table and package  manually before applying  the  script.  If you wish
   to preserve the logged error data, you should consider archiving it in
   a backup. Please  read the comments in this script file carefully  for
   hints on users and synonyms.
