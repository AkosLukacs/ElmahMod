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
    using System.Web;
    using ContentSyndication;

    using XmlSerializer = System.Xml.Serialization.XmlSerializer;
    using ArrayList = System.Collections.ArrayList;

    #endregion

    /// <summary>
    /// Renders a XML using the RSS 0.91 vocabulary that displays, at most,
    /// the 15 most recent errors recorded in the error log.
    /// </summary>

    internal sealed class ErrorRssHandler : IHttpHandler
    {
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/xml";

            //
            // Get the last set of errors for this application.
            //

            const int pageSize = 15;
            ArrayList errorEntryList = new ArrayList(pageSize);
            ErrorLog log = ErrorLog.GetDefault(context);
            log.GetErrors(0, pageSize, errorEntryList);

            //
            // We'll be emitting RSS vesion 0.91.
            //

            RichSiteSummary rss = new RichSiteSummary();
            rss.version = "0.91";

            //
            // Set up the RSS channel.
            //
            
            Channel channel = new Channel();
            channel.title = "Error log of " + log.ApplicationName + " on " + Environment.MachineName;
            channel.description = "Log of recent errors";
            channel.language = "en";
            channel.link = context.Request.Url.GetLeftPart(UriPartial.Authority) + 
                context.Request.ServerVariables["URL"];

            rss.channel = channel;

            //
            // For each error, build a simple channel item. Only the title, 
            // description, link and pubDate fields are populated.
            //

            channel.item = new Item[errorEntryList.Count];

            for (int index = 0; index < errorEntryList.Count; index++)
            {
                ErrorLogEntry errorEntry = (ErrorLogEntry) errorEntryList[index];
                Error error = errorEntry.Error;

                Item item = new Item();

                item.title = error.Message;
                item.description = "An error of type " + error.Type + " occurred. " + error.Message;
                item.link = channel.link + "/detail?id=" + HttpUtility.UrlEncode(errorEntry.Id);
                item.pubDate = error.Time.ToUniversalTime().ToString("r");

                channel.item[index] = item;
            }

            //
            // Stream out the RSS XML.
            //

            XmlSerializer serializer = new XmlSerializer(typeof(RichSiteSummary));
            serializer.Serialize(context.Response.Output, rss);
        }

        public bool IsReusable
        {
            get { return false; }
        }
    }
}
