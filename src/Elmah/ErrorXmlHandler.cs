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

    using System.Net;
    using System.Web;
    using System.Xml;

    #endregion

    /// <summary>
    /// Renders an error as an XML document.
    /// </summary>

    internal sealed class ErrorXmlHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            HttpResponse response = context.Response;
            response.ContentType = "application/xml";

            //
            // Retrieve the ID of the requested error and read it from 
            // the store.
            //

            string errorId = Mask.NullString(context.Request.QueryString["id"]);

            if (errorId.Length == 0)
                throw new ApplicationException("Missing error identifier specification.");

            ErrorLogEntry entry = ErrorLog.GetDefault(context).GetError(errorId);

            //
            // Perhaps the error has been deleted from the store? Whatever
            // the reason, pretend it does not exist.
            //

            if (entry == null)
            {
                throw new HttpException((int) HttpStatusCode.NotFound, 
                    string.Format("Error with ID '{0}' not found.", errorId));
            }

            //
            // Stream out the error as formatted XML.
            //

#if NET_2_0
            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Indent = true;
            settings.NewLineOnAttributes = true;
            settings.CheckCharacters = false;
            XmlWriter writer = XmlWriter.Create(response.Output, settings);
#else
            XmlTextWriter writer = new XmlTextWriter(response.Output);
            writer.Formatting = Formatting.Indented;
#endif

            writer.WriteStartDocument();
            writer.WriteStartElement("error");
            entry.Error.ToXml(writer);
            writer.WriteEndElement(/* error */);
            writer.WriteEndDocument();
            writer.Flush();
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}