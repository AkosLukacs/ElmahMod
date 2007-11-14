                                 ELMAH README

Upgrading

   If  you are using the Microsoft SQL Server for your error log then you
   should  re-create  the  ELMAH_Error  table,  its  indicies and related
   stored  procedures  using the supplied SQL script (see Database.sql in
   your distribution). The script does not contain DDL DROP statements so
   you  will have to drop the table and stored procedures manually before
   applying  the  script.  If you wish to preserve the logged error data,
   you should consider archiving it in a backup.
