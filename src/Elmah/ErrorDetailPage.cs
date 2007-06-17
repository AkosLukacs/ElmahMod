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
    using System.Web.UI.WebControls;
    using System.Web.Mail;

    using NameValueCollection = System.Collections.Specialized.NameValueCollection;
    using Comparer = System.Collections.Comparer;
    using StringWriter = System.IO.StringWriter;

    #endregion

    /// <summary>
    /// Renders an HTML page displaying details about an error from the 
    /// error log.
    /// </summary>

    internal sealed class ErrorDetailPage : ErrorPageBase
    {
        private ErrorLogEntry _errorEntry;

        protected override void OnLoad(EventArgs e)
        {
            //
            // Retrieve the ID of the error to display and read it from 
            // the store.
            //

            string errorId = Mask.NullString(this.Request.QueryString["id"]);

            if (errorId.Length == 0)
                return;

            _errorEntry = this.ErrorLog.GetError(errorId);

            //
            // Perhaps the error has been deleted from the store? Whatever
            // the reason, bail out silently.
            //

            if (_errorEntry == null)
                return;

            //
            // Setup the title of the page.
            //

            this.PageTitle = string.Format("Error: {0} [{1}]", _errorEntry.Error.Type, _errorEntry.Id);

            base.OnLoad(e);
        }

        protected override void RenderContents(HtmlTextWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException("writer");

            if (_errorEntry != null)
            {
                RenderError(writer);
            }
            else
            {
                RenderNoError(writer);
            }
        }

        private void RenderNoError(HtmlTextWriter writer)
        {
            Debug.Assert(writer != null);

            writer.RenderBeginTag(HtmlTextWriterTag.P);
            writer.Write("Error not found in log.");
            writer.RenderEndTag(); // </p>
            writer.WriteLine();
        }

        private void RenderError(HtmlTextWriter writer)
        {
            Debug.Assert(writer != null);

            Error error = _errorEntry.Error;

            //
            // Write out the page title containing error type and message.
            //

            writer.AddAttribute(HtmlTextWriterAttribute.Id, "PageTitle");
            writer.RenderBeginTag(HtmlTextWriterTag.H1);
            Server.HtmlEncode(error.Message, writer);
            writer.RenderEndTag(); // </p>
            writer.WriteLine();

            writer.AddAttribute(HtmlTextWriterAttribute.Id, "ErrorTitle");
            writer.RenderBeginTag(HtmlTextWriterTag.P);

            writer.AddAttribute(HtmlTextWriterAttribute.Id, "ErrorType");
            writer.RenderBeginTag(HtmlTextWriterTag.Span);
            Server.HtmlEncode(error.Type, writer);
            writer.RenderEndTag(); // </span>

            writer.AddAttribute(HtmlTextWriterAttribute.Id, "ErrorTypeMessageSeparator");
            writer.RenderBeginTag(HtmlTextWriterTag.Span);
            writer.Write(": ");
            writer.RenderEndTag(); // </span>

            writer.AddAttribute(HtmlTextWriterAttribute.Id, "ErrorMessage");
            writer.RenderBeginTag(HtmlTextWriterTag.Span);
            Server.HtmlEncode(error.Message, writer);
            writer.RenderEndTag(); // </span>

            writer.RenderEndTag(); // </p>
            writer.WriteLine();

            //
            // Do we have details, like the stack trace? If so, then write 
            // them out in a pre-formatted (pre) element. 
            // NOTE: There is an assumption here that detail will always
            // contain a stack trace. If it doesn't then pre-formatting 
            // might not be the right thing to do here.
            //

            if (error.Detail.Length != 0)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Id, "ErrorDetail");
                writer.RenderBeginTag(HtmlTextWriterTag.Pre);
                writer.Flush();
                Server.HtmlEncode(error.Detail, writer.InnerWriter);
                writer.RenderEndTag(); // </pre>
                writer.WriteLine();
            }

            //
            // Write out the error log time. This will be in the local
            // time zone of the server. Would be a good idea to indicate
            // it here for the user.
            //

            writer.AddAttribute(HtmlTextWriterAttribute.Id, "ErrorLogTime");
            writer.RenderBeginTag(HtmlTextWriterTag.P);
            Server.HtmlEncode(string.Format("Logged on {0} at {1}",
                error.Time.ToLongDateString(),
                error.Time.ToLongTimeString()), writer);
            writer.RenderEndTag(); // </p>
            writer.WriteLine();

            //
            // Do we have an HTML formatted message from ASP.NET? If yes,
            // then write out a link to it instead of embedding it 
            // with the rest of the content since it is an entire HTML
            // document in itself.
            //

            if (error.WebHostHtmlMessage.Length != 0)
            {
                string htmlUrl = this.BasePageName + "/html?id=" + _errorEntry.Id;

                writer.RenderBeginTag(HtmlTextWriterTag.P);
            
                writer.AddAttribute(HtmlTextWriterAttribute.Href, htmlUrl);
                writer.RenderBeginTag(HtmlTextWriterTag.A);
                writer.Write("See ASP.NET error message in full view");
                writer.RenderEndTag(); // </a>
            
                writer.RenderEndTag(); // </p>
                writer.WriteLine();
            }

            //
            // If this error has context, then write it out.
            // ServerVariables are good enough for most purposes, so
            // we only write those out at this time.
            //

            RenderCollection(writer, error.ServerVariables, 
                "ServerVariables", "Server Variables");

            base.RenderContents(writer);
        }

        private void RenderCollection(HtmlTextWriter writer,
            NameValueCollection collection, string id, string title)
        {
            Debug.Assert(writer != null);
            Debug.AssertStringNotEmpty(id);
            Debug.AssertStringNotEmpty(title);

            //
            // If the collection isn't there or it's empty, then bail out.
            //
        
            if (collection == null || collection.Count == 0)
                return;

            //
            // Surround the entire section with a <div> element.
            //

            writer.AddAttribute(HtmlTextWriterAttribute.Id, id);
            writer.RenderBeginTag(HtmlTextWriterTag.Div);

            //
            // Write out the table caption.
            //

            writer.AddAttribute(HtmlTextWriterAttribute.Class, "table-caption");
            writer.RenderBeginTag(HtmlTextWriterTag.P);
            this.Server.HtmlEncode(title, writer);
            writer.RenderEndTag(); // </p>
            writer.WriteLine();

            //
            // Some values can be large and add scroll bars to the page
            // as well as ruin some formatting. So we encapsulate the
            // table into a scrollable view that is controlled via the 
            // style sheet.
            //

            writer.AddAttribute(HtmlTextWriterAttribute.Class, "scroll-view");
            writer.RenderBeginTag(HtmlTextWriterTag.Div);

            //
            // Create a table to display the name/value pairs of the
            // collection in 2 columns.
            //

            Table table = new Table();
            table.CellSpacing = 0;

            //
            // Create the header row and columns.
            //

            TableRow headRow = new TableRow();
            
            TableHeaderCell headCell;

            headCell = new TableHeaderCell();
            headCell.Wrap = false;
            headCell.Text = "Name";
            headCell.CssClass = "name-col";

            headRow.Cells.Add(headCell);

            headCell = new TableHeaderCell();
            headCell.Wrap = false;
            headCell.Text = "Value";
            headCell.CssClass = "value-col";

            headRow.Cells.Add(headCell);

            table.Rows.Add(headRow);

            //
            // Create a row for each entry in the collection.
            //

            string[] keys = collection.AllKeys;
            Array.Sort(keys, Comparer.DefaultInvariant);

            for (int keyIndex = 0; keyIndex < keys.Length; keyIndex++)
            {
                string key = keys[keyIndex];

                TableRow bodyRow = new TableRow();
                bodyRow.CssClass = keyIndex % 2 == 0 ? "even-row" : "odd-row";

                TableCell cell;

                //
                // Create the key column.
                //

                cell = new TableCell();
                cell.Text = Server.HtmlEncode(key);
                cell.CssClass = "key-col";

                bodyRow.Cells.Add(cell);

                //
                // Create the value column.
                //

                cell = new TableCell();
                cell.Text = Server.HtmlEncode(collection[key]);
                cell.CssClass = "value-col";

                bodyRow.Cells.Add(cell);

                table.Rows.Add(bodyRow);
            }

            //
            // Write out the table and close container tags.
            //

            table.RenderControl(writer);

            writer.RenderEndTag(); // </div>
            writer.WriteLine();

            writer.RenderEndTag(); // </div>
            writer.WriteLine();
        }
    }
}
