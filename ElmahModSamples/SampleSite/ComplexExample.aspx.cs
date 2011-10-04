using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace ElmahModSampleSite
{
   public partial class ComplexExample : System.Web.UI.Page
   {
      protected void Page_Load(object sender, EventArgs e)
      {
         ThrowException dataAccess = new ThrowException();

         try
         {
            dataAccess.ThrowComplex("this will be complex!");
         }
         catch (Exception ex)
         {

            Elmah.ErrorSignal.FromCurrentContext().Raise(ex, HttpContext.Current);
         }
      }
   }
}