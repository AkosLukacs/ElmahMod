                                 ELMAH README

   Please read this document carefully before using this release of ELMAH
   as it contains important information.

   For peer help and support, vists the [1]ELMAH Discussion Group. Report
   bugs  and issues to the [2]issue tracker on the [3]project site. Avoid
   using   the   issue  tracker  to  seek  help  with  problems  you  are
   experiencing  in  installing  or  running  ELMAH.  That  is  what  the
   discussion group is for.

Version 1.0 BETA 3 Notes

  Upgrading from ELMAH 1.0 BETA 2(a)

   If  you are using the Microsoft SQL Server for your error log then you
   should  re-create  the  stored  procedures  found  in the supplied SQL
   script  (see  SQLServer.sql).  The script does not contain DDL DROP or
   ALTER  statements  so  you  will  have  to  drop the stored procedures
   manually  before  applying  the  CREATE PROCEDURE parts of the script.
   Other  than  that, there have been no changes to the schema since BETA
   2a so existing data in your logs can be left as it is.

   The  Oracle error log is new in BETA 3, but if you have been compiling
   ELMAH  from  sources  between  BETA  2 and 3 and using Oracle for your
   error  log  then  you  should  re-create  the  ELMAH$Error  table, its
   indicies  and  related  package  using  the  supplied  SQL script (see
   Oracle.sql in your distribution). The script does not contain any DROP
   statements  so  you  will  have to drop the table and package manually
   before  applying  the script. If you wish to preserve the logged error
   data,  you  should  consider archiving it in a backup. Please read the
   comments  in  this  script  file  carefully  for  hints  on  users and
   synonyms.

Version 1.0 BETA 2(a) Notes

  Upgrading from GDN-ELMAH or ELMAH 1.0 BETA 1

   The  configuration  sections  and entries have changed slightly if you
   are  using  GDN-ELMAH,  which  is  the  original  that was released on
   GotDotNet.  Consult the samples/web.config file to see examples of how
   the configuration looks like now.

   If  you are using the Microsoft SQL Server for your error log then you
   should  re-create  the  ELMAH_Error  table,  its  indicies and related
   stored  procedures  using the supplied SQL script (see Database.sql in
   your distribution). The script does not contain DDL DROP statements so
   you  will have to drop the table and stored procedures manually before
   applying  the  script.  If you wish to preserve the logged error data,
   you should consider archiving it in a backup.

References

   1. http://groups.google.com/group/elmah
   2. http://code.google.com/p/elmah/issues/list
   3. http://elmah.googlecode.com/
