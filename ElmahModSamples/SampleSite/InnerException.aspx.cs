using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace ElmahModSampleSite
{
   public partial class InnerException1 : System.Web.UI.Page
   {
      protected void Page_Load(object sender, EventArgs e)
      {
         ThrowException dataAccess = new ThrowException();

         try
         {
            dataAccess.ThrowWithInner();
         }
         catch (Exception ex)
         {

            Elmah.ErrorSignal.FromCurrentContext().Raise(ex, HttpContext.Current);
         }
      }
   }
}