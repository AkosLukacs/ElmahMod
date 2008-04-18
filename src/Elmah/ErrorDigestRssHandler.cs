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
    using System.IO;
    using System.Text;
    using System.Web;
    using System.Web.UI;
    using ContentSyndication;

    using XmlSerializer = System.Xml.Serialization.XmlSerializer;
    using ArrayList = System.Collections.ArrayList;

    #endregion

    /// <summary>
    /// Renders an RSS feed that is a daily digest of the most recently 
    /// recorded errors in the error log. The feed spans at most 15
    /// days on which errors occurred.
    /// </summary>

    internal sealed class ErrorDigestRssHandler : IHttpHandler
    {
        private HttpContext _context;

        public void ProcessRequest(HttpContext context)
        {
            _context = context;
            Render();
        }

        public bool IsReusable
        {
            get { return false; }
        }

        private HttpRequest Request
        {
            get { return _context.Request; }
        }

        private HttpResponse Response
        {
            get { return _context.Response; }
        }

        private HttpServerUtility Server
        {
            get { return _context.Server; }
        }

        private void Render()
        {
            Response.ContentType = "application/xml";

            ErrorLog log = ErrorLog.GetDefault(_context);

            //
            // We'll be emitting RSS vesion 0.91.
            //

            RichSiteSummary rss = new RichSiteSummary();
            rss.version = "0.91";

            //
            // Set up the RSS channel.
            //
            
            Channel channel = new Channel();
            channel.title = "Daily digest of errors in " + log.ApplicationName + " on " + Environment.MachineName;
            channel.description = "Daily digest of application errors";
            channel.language = "en";

            Uri baseUrl = new Uri(Request.Url.GetLeftPart(UriPartial.Authority) + Request.ServerVariables["URL"]);
            channel.link = baseUrl.ToString();

            rss.channel = channel;

            //
            // Build the channel items.
            //

            const int pageSize = 30;
            const int maxPageLimit = 30;
            ArrayList itemList = new ArrayList(pageSize);
            ArrayList errorEntryList = new ArrayList(pageSize);

            //
            // Start with the first page of errors.
            //

            int pageIndex = 0;

            //
            // Initialize the running state.
            //

            DateTime runningDay = DateTime.MaxValue;
            int runningErrorCount = 0;
            Item item = null;
            StringBuilder sb = new StringBuilder();
            HtmlTextWriter writer = new HtmlTextWriter(new StringWriter(sb));

            do
            {
                //
                // Get a logical page of recent errors and loop through them.
                //

                errorEntryList.Clear();
                log.GetErrors(pageIndex++, pageSize, errorEntryList);

                foreach (ErrorLogEntry entry in errorEntryList)
                {
                    Error error = entry.Error;
                    DateTime time = error.Time.ToUniversalTime();
                    DateTime day = new DateTime(time.Year, time.Month, time.Day);

                    //
                    // If we're dealing with a new day then break out to a 
                    // new channel item, finishing off the previous one.
                    //

                    if (day < runningDay)
                    {
                        if (runningErrorCount > 0)
                        {
                            RenderEnd(writer);
                            item.description = sb.ToString();
                            itemList.Add(item);
                        }

                        runningDay = day;
                        runningErrorCount = 0;

                        if (itemList.Count == pageSize)
                            break;

                        item = new Item();
                        item.pubDate = time.ToString("r");
                        item.title = string.Format("Digest for {0} ({1})", 
                            runningDay.ToString("yyyy-MM-dd"), runningDay.ToLongDateString());

                        sb.Length = 0;
                        RenderStart(writer);
                    }

                    RenderError(writer, entry, baseUrl);
                    runningErrorCount++;
                }
            }
            while (pageIndex < maxPageLimit && itemList.Count < pageSize && errorEntryList.Count > 0);

            if (runningErrorCount > 0)
            {
                RenderEnd(writer);
                item.description = sb.ToString();
                itemList.Add(item);
            }

            channel.item = (Item[]) itemList.ToArray(typeof(Item));

            //
            // Stream out the RSS XML.
            //

            XmlSerializer serializer = new XmlSerializer(typeof(RichSiteSummary));
            serializer.Serialize(Response.Output, rss);
        }

        private static void RenderStart(HtmlTextWriter writer) 
        {
            Debug.Assert(writer != null);

            writer.RenderBeginTag(HtmlTextWriterTag.Ul);
        }

        private void RenderError(HtmlTextWriter writer, ErrorLogEntry entry, Uri baseUrl) 
        {
            Debug.Assert(writer != null);
            Debug.Assert(baseUrl != null);
            Debug.Assert(entry != null);

            Error error = entry.Error;
            writer.RenderBeginTag(HtmlTextWriterTag.Li);

            string errorType = ErrorDisplay.HumaneExceptionErrorType(error);

            if (errorType.Length > 0)
            {
                bool abbreviated = errorType.Length < error.Type.Length;
                        
                if (abbreviated)
                {
                    writer.AddAttribute(HtmlTextWriterAttribute.Title, error.Type);
                    writer.RenderBeginTag(HtmlTextWriterTag.Span);
                }

                Server.HtmlEncode(errorType, writer);
                        
                if (abbreviated)
                    writer.RenderEndTag(/* span */);

                writer.Write(": ");
            }

            writer.AddAttribute(HtmlTextWriterAttribute.Href, baseUrl + "/detail?id=" + entry.Id);
            writer.RenderBeginTag(HtmlTextWriterTag.A);
            Server.HtmlEncode(error.Message, writer);
            writer.RenderEndTag(/* a */);
                    
            writer.RenderEndTag( /* li */);
        }

        private static void RenderEnd(HtmlTextWriter writer)
        {
            Debug.Assert(writer != null);

            writer.RenderEndTag(/* li */);
            writer.Flush();
        }
    }
}