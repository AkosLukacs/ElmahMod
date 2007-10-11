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

    #endregion

    internal sealed class AboutPage : ErrorPageBase
    {
        public AboutPage()
        {
            PageTitle = "About ELMAH";
        }

        protected override void RenderContents(HtmlTextWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException("writer");
            
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "PageTitle");
            writer.RenderBeginTag(HtmlTextWriterTag.H1);
            writer.Write(PageTitle);
            writer.RenderEndTag(); // </h1>
            writer.WriteLine();

            SccStamp[] stamps = SccStamp.FindAll(typeof(ErrorLog).Assembly);
            SccStamp.SortByRevision(stamps, /* descending */ true);

            writer.RenderBeginTag(HtmlTextWriterTag.P);
            writer.Write("This <strong>{0}</strong> ", Build.TypeLowercase);
            
            if (stamps.Length > 0)
                writer.Write("(SCC #{0}) ", stamps[0].Revision.ToString("N0"));

            writer.Write("build was compiled from the following sources for CLR {0}:", Build.ImageRuntimeVersion);

            writer.RenderEndTag(); // </p>

            writer.RenderBeginTag(HtmlTextWriterTag.Ul);

            foreach (SccStamp stamp in stamps)
            {
                writer.RenderBeginTag(HtmlTextWriterTag.Li);
                writer.RenderBeginTag(HtmlTextWriterTag.Code);
                Server.HtmlEncode(stamp.Id, writer);
                writer.RenderEndTag(); // </code>
                writer.RenderEndTag(); // </li>
            }

            writer.RenderEndTag(); // </ul>
        }
    }
}
