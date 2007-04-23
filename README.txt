                                 ELMAH README

   This document contains important information about compiling the ELMAH
   binaries   and  installation  (MSI)  package.  The  compilation  is  a
   three-phase  process.  In  the  first  phase,  you  compile  the ELMAH
   binaries.  In  the second phase, you compile XML documentation into an
   HTML  Help file. This is needed by the third phase, which packages the
   binaries,  sources  and  the  help  file  into  an  installation (MSI)
   package.

   The following components are required for a successful compilation:

     * Microsoft .NET Framework 1.1

     * Microsoft Visual Studio 2003

     * [1]NDoc (Code Documentation Generator for .NET)

     * Orca  (see the [2]Microsoft KB article Q255905 for instructions on
       obtaining and installing Orca)

   Below  are  the  detailed  steps  for compiling the ELMAH binaries and
   installation   (MSI)   package.  Please  follow  these  steps  in  the
   prescribed order:

    1. Open the solution file Elmah.sln in Microsoft Visual Studio 2003.

    2. From the main menu, select Build and then Configuration Manager.

    3. Once  the Configuration Manager dialog box appears, select Release
       as the active solution configuration and click OK.

    4. Select  the GotDotNet.Elmah project from the Solution Explorer and
       then build the project only (not the entire solution).

    5. Open the file ELMAH.ndoc in NDoc.

    6. On  the  main NDoc screen, select MSDN from the Documentation Type
       combo box.

    7. From the main NDoc menu, select Documentation and then Build. This
       will  produce  compiled  HTML Help file from the XML documentation
       file produced by building ELMAH binaries in step 4.

    8. Return  to the ELMAH solution opened up in Microsoft Visual Studio
       2003 and this time build the Setup project.

    9. Run  Orca  (MSI  database editor) and open the ELMAHSetup.msi file
       produced by the previous step.

   10. Once  the  MSI  is  open,  select View from the main menu and then
       Summary Information.

   11. When  Edit  Summary  Information  dialog  opens,  navigate  to the
       Package  Code  text  box  and  replace  the  GUID  with  the value
       {5B73DC36-9803-4F05-9C00-E7EEE0585FB6} and click OK. If you do not
       carry  out  this  step,  then installing the MSI on a system where
       ELMAH  was  already  installed  will generate the message "Another
       version of this product is already installed" and stall the setup.
       Normally

   12. Close Orca and when prompted to save the changes, select Yes.

   Finished!

References

   1. http://ndoc.sourceforge.net/
   2. http://support.microsoft.com/kb/255905
