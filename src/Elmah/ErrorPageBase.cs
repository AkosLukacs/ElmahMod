#region License, Terms and Author(s)
//
// ELMAH - Error Logging Modules and Handlers for ASP.NET
// Copyright (c) 2007 Atif Aziz. All rights reserved.
//
//  Author(s):
//
//      Atif Aziz, http://www.raboof.com
//
// This library is free software; you can redistribute it and/or modify it 
// under the terms of the New BSD License, a copy of which should have 
// been delivered along with this distribution.
//
// THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS 
// "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT 
// LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A 
// PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT 
// OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, 
// SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT 
// LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, 
// DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY 
// THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT 
// (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE 
// OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
//
#endregion

[assembly: Elmah.Scc("$Id$")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Web.UI;

    using CultureInfo = System.Globalization.CultureInfo;
    
    #endregion

    /// <summary>
    /// Provides the base implementation and layout for most pages that render 
    /// HTML for the error log.
    /// </summary>

    internal abstract class ErrorPageBase : Page
    {
        private string _title;

        protected string BasePageName
        {
            get { return this.Request.ServerVariables["URL"]; }
        }

        protected virtual ErrorLog ErrorLog
        {
            get { return ErrorLog.Default; }
        }

        protected virtual string PageTitle
        {
            get { return Mask.NullString(_title); }
            set { _title = value; }
        }

        protected virtual string ApplicationName
        {
            get { return this.ErrorLog.ApplicationName; }
        }

        protected virtual void RenderDocumentStart(HtmlTextWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException("writer");

            writer.RenderBeginTag(HtmlTextWriterTag.Html);  // <html>
            
            writer.RenderBeginTag(HtmlTextWriterTag.Head);  // <head>
            RenderHead(writer);
            writer.RenderEndTag();                          // </head>
            writer.WriteLine();

            writer.RenderBeginTag(HtmlTextWriterTag.Body);  // <body>
        }

        protected virtual void RenderHead(HtmlTextWriter writer)
        {
            //
            // Write the document title.
            //

            writer.RenderBeginTag(HtmlTextWriterTag.Title);
            Server.HtmlEncode(this.PageTitle, writer);
            writer.RenderEndTag();
            writer.WriteLine();

            //
            // Write a <link> tag to relate the style sheet.
            //

            writer.AddAttribute("rel", "stylesheet");
            writer.AddAttribute(HtmlTextWriterAttribute.Type, "text/css");
            writer.AddAttribute(HtmlTextWriterAttribute.Href, this.BasePageName + "/stylesheet");
            writer.RenderBeginTag(HtmlTextWriterTag.Link);
            writer.RenderEndTag();
            writer.WriteLine();
        }

        protected virtual void RenderDocumentEnd(HtmlTextWriter writer)
        {
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "Footer");
            writer.RenderBeginTag(HtmlTextWriterTag.P); // <p>

            //
            // Write the powered-by signature, that includes version information.
            //

            PoweredBy poweredBy = new PoweredBy();
            poweredBy.RenderControl(writer);

            //
            // Write out server date, time and time zone details.
            //

            DateTime now = DateTime.Now;

            writer.Write("Server date is ");
            this.Server.HtmlEncode(now.ToString("D", CultureInfo.InvariantCulture), writer);

            writer.Write(". Server time is ");
            this.Server.HtmlEncode(now.ToString("T", CultureInfo.InvariantCulture), writer);

            writer.Write(". All dates and times displayed are in the ");
            writer.Write(TimeZone.CurrentTimeZone.IsDaylightSavingTime(now) ?
                TimeZone.CurrentTimeZone.DaylightName : TimeZone.CurrentTimeZone.StandardName);
            writer.Write(" zone. ");

            //
            // Write out the source of the log.
            //

            writer.Write("This log is provided by the ");
            this.Server.HtmlEncode(this.ErrorLog.Name, writer);
            writer.Write('.');

            writer.RenderEndTag(); // </p>

            writer.RenderEndTag(); // </body>
            writer.WriteLine();

            writer.RenderEndTag(); // </html>
            writer.WriteLine();
        }

        protected override void Render(HtmlTextWriter writer)
        {
            RenderDocumentStart(writer);
            RenderContents(writer);
            RenderDocumentEnd(writer);
        }

        protected virtual void RenderContents(HtmlTextWriter writer)
        {
            base.Render(writer);
        }
    }
}
