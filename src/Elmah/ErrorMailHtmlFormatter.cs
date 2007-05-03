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

namespace Elmah
{
	#region Imports

	using System;

    using TextWriter = System.IO.TextWriter;
    using HttpUtility = System.Web.HttpUtility;
    using NameValueCollection = System.Collections.Specialized.NameValueCollection;
    using HtmlTextWriter = System.Web.UI.HtmlTextWriter;
    using Html32TextWriter = System.Web.UI.Html32TextWriter;
    using HtmlTextWriterTag = System.Web.UI.HtmlTextWriterTag;
    using HtmlTextWriterAttribute = System.Web.UI.HtmlTextWriterAttribute;

	#endregion

    /// <summary>
    /// Formats the HTML to display the details of a given error that is
    /// suitable for sending as the body of an e-mail message.
    /// </summary>
    
    public class ErrorMailHtmlFormatter : ErrorTextFormatter
    {
        private HtmlTextWriter _writer;
        private Error _error;

        /// <summary>
        /// Returns the text/html MIME type that is the format provided 
        /// by this <see cref="ErrorTextFormatter"/> implementation.
        /// </summary>

        public override string MimeType
        { 
            get { return "text/html"; }
        }

        /// <summary>
        /// Formats a complete HTML document describing the given 
        /// <see cref="Error"/> instance.
        /// </summary>

        public override void Format(TextWriter writer, Error error)
        {
            if (writer == null)
                throw new ArgumentNullException("writer");

            if (error == null)
                throw new ArgumentNullException("error");

            //
            // Create a HTML writer on top of the given writer and
            // write out the document. Note that the incoming 
            // writer and error parameters are promoted to members
            // during formatting in order to avoid passing them
            // as context to each downstream method responsible
            // for rendering a part of the document. The important 
            // assumption here is that Format will never be called
            // from more than one thread at the same time.
            //

            Html32TextWriter htmlWriter = new Html32TextWriter(writer);

            htmlWriter.RenderBeginTag(HtmlTextWriterTag.Html);

            _writer = htmlWriter;
            _error = error;

            try
            {
                RenderHead();
                RenderBody();
            }
            finally
            {
                _writer = null;
                _error = null;
            }

            htmlWriter.RenderEndTag(); // </html>
            htmlWriter.WriteLine();
        }

        /// <summary>
        /// Gets the <see cref="HtmlTextWriter"/> used for HTML formatting.
        /// </summary>
        /// <remarks>
        /// This property is only available to downstream methods in the
        /// context of the <see cref="Format"/> method call.
        /// </remarks>

        protected HtmlTextWriter Writer
        {
            get { return _writer; }
        }

        /// <summary>
        /// Gets the <see cref="Error"/> object for which a HTML document
        /// is being formatted.
        /// </summary>
        /// <remarks>
        /// This property is only available to downstream methods in the
        /// context of the <see cref="Format"/> method call.
        /// </remarks>

        protected Error Error
        {
            get { return _error; }
        }

        /// <summary>
        /// Renders the &lt;head&gt; section of the HTML document.
        /// </summary>
        
        protected virtual void RenderHead()
        {
            HtmlTextWriter writer = this.Writer;

            writer.RenderBeginTag(HtmlTextWriterTag.Head);
    
            //
            // Write the document title and style.
            //
    
            writer.RenderBeginTag(HtmlTextWriterTag.Title);
            writer.Write("Error: ");
            HttpUtility.HtmlEncode(this.Error.Message, writer);
            writer.RenderEndTag(); // </title>
            writer.WriteLine();

            RenderStyle();

            writer.RenderEndTag(); // </head>
            writer.WriteLine();
        }

        /// <summary>
        /// Renders the &lt;body&gt; section of the HTML document.
        /// </summary>

        protected virtual void RenderBody()
        {
            HtmlTextWriter writer = this.Writer;

            writer.RenderBeginTag(HtmlTextWriterTag.Body);

            RenderSummary();

            if (this.Error.Detail.Length != 0)
            {
                RenderDetail();   
            }

            RenderCollections();
            
            RenderFooter();

            writer.RenderEndTag(); // </body>
            writer.WriteLine();
        }

        /// <summary>
        /// Renders the footer content that appears at the end of the 
        /// HTML document body.
        /// </summary>

        protected virtual void RenderFooter()
        {
            HtmlTextWriter writer = this.Writer;

            writer.RenderBeginTag(HtmlTextWriterTag.P);
            PoweredBy poweredBy = new PoweredBy();
            poweredBy.RenderControl(writer);
            writer.RenderEndTag();
        }

        /// <summary>
        /// Renders the &lt;style&gt; element along with in-line styles
        /// used to format the body of the HTML document.
        /// </summary>

        protected virtual void RenderStyle()
        {
            HtmlTextWriter writer = this.Writer;

            writer.RenderBeginTag(HtmlTextWriterTag.Style);

            writer.WriteLine(@"
                body { font-family: verdana, arial, helvetic; font-size: x-small; } 
                td, th, pre { font-size: x-small; } 
                #errorDetail { padding: 1em; background-color: #FFFFCC; } 
                #errorMessage { font-size: medium; font-style: italic; color: maroon; }
                h1 { font-size: small; }");
            
            writer.RenderEndTag(); // </style>
            writer.WriteLine();
        }

        /// <summary>
        /// Renders the details about the <see cref="Error" /> object in
        /// body of the HTML document.
        /// </summary>

        protected virtual void RenderDetail()
        {
            HtmlTextWriter writer = this.Writer;

            //
            // Write the full text of the error.
            //
    
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "errorDetail");
            writer.RenderBeginTag(HtmlTextWriterTag.Pre);
            writer.InnerWriter.Flush();
            HttpUtility.HtmlEncode(this.Error.Detail, writer.InnerWriter);
            writer.RenderEndTag(); // </pre>
            writer.WriteLine();
        }

        /// <summary>
        /// Renders a summary about the <see cref="Error"/> object in
        /// body of the HTML document.
        /// </summary>

        protected virtual void RenderSummary()
        {
            HtmlTextWriter writer = this.Writer;
            Error error = this.Error;

            //
            // Write the error type and message.
            //
    
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "errorMessage");
            writer.RenderBeginTag(HtmlTextWriterTag.P);
            HttpUtility.HtmlEncode(error.Type, writer);
            writer.Write(": ");
            HttpUtility.HtmlEncode(error.Message, writer);
            writer.RenderEndTag(); // </p>
            writer.WriteLine();
    
            //
            // Write out the time, in UTC, at which the error was generated.
            // 
    
            if (error.Time != DateTime.MinValue)
            {            
                writer.RenderBeginTag(HtmlTextWriterTag.P);
                writer.Write("Generated: ");
                HttpUtility.HtmlEncode(error.Time.ToUniversalTime().ToString("r"), writer);
                writer.RenderEndTag(); // </p>
                writer.WriteLine();
            }
        }

        /// <summary>
        /// Renders the diagnostic collections of the <see cref="Error" /> object in
        /// body of the HTML document.
        /// </summary>

        protected virtual void RenderCollections()
        {
            RenderCollection(this.Error.ServerVariables, "Server Variables");
        }

        /// <summary>
        /// Renders a collection as a table in HTML document body.
        /// </summary>
        /// <remarks>
        /// This method is called by <see cref="RenderCollections"/> to 
        /// format a diagnostic collection from <see cref="Error"/> object.
        /// </remarks>

        protected virtual void RenderCollection(NameValueCollection collection, string caption)
        {
            if (collection == null || collection.Count == 0)
            {
                return;
            }

            HtmlTextWriter writer = this.Writer;

            writer.RenderBeginTag(HtmlTextWriterTag.H1);
            HttpUtility.HtmlEncode(caption, writer);
            writer.RenderEndTag(); // </h1>
            writer.WriteLine();

            //
            // Write a table with each key in the left column
            // and its value in the right column.
            //

            writer.AddAttribute(HtmlTextWriterAttribute.Cellpadding, "5");
            writer.AddAttribute(HtmlTextWriterAttribute.Cellspacing, "0");
            writer.AddAttribute(HtmlTextWriterAttribute.Border, "1");
            writer.AddAttribute(HtmlTextWriterAttribute.Width, "100%");
            writer.RenderBeginTag(HtmlTextWriterTag.Table);

            //
            // Write the column headings.
            //

            writer.AddAttribute(HtmlTextWriterAttribute.Valign, "top");
            writer.RenderBeginTag(HtmlTextWriterTag.Tr);

            writer.AddAttribute(HtmlTextWriterAttribute.Align, "left");
            writer.RenderBeginTag(HtmlTextWriterTag.Th);
            writer.Write("Name");
            writer.RenderEndTag(); // </th>

            writer.AddAttribute(HtmlTextWriterAttribute.Align, "left");
            writer.RenderBeginTag(HtmlTextWriterTag.Th);
            writer.Write("Value");
            writer.RenderEndTag(); // </th>

            writer.RenderEndTag(); // </tr>

            //
            // Write the main body of the table containing keys
            // and values in a two-column layout.
            //

            foreach (string key in collection.Keys)
            {
                writer.AddAttribute(HtmlTextWriterAttribute.Valign, "top");
                writer.RenderBeginTag(HtmlTextWriterTag.Tr);
                
                writer.RenderBeginTag(HtmlTextWriterTag.Td);
                HttpUtility.HtmlEncode(key, writer);
                writer.RenderEndTag(); // </td>
                
                writer.RenderBeginTag(HtmlTextWriterTag.Td);

                string value = Mask.NullString(collection[key]);

                if (value.Length != 0)
                {
                    HttpUtility.HtmlEncode(value, writer);
                }
                else
                {
                    writer.Write("&nbsp;");
                }

                writer.RenderEndTag(); // </td>

                writer.RenderEndTag(); // </tr>
            }

            writer.RenderEndTag(); // </table>
            writer.WriteLine();
        }
    }
}
