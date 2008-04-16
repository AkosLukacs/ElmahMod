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
    using System.Reflection;
    using System.Web.UI;

    #endregion

    /// <summary>
    /// Renders an HTML page that presents information about the version,
    /// build configuration, source files as well as a method to check
    /// for updates.
    /// </summary>

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

            //
            // Emit a script that emit version info and checks for updates.
            //

            writer.WriteLine(@"
                <script type='text/javascript' language='JavaScript'>
                    function onCheckForUpdate(sender) {
                        var script = document.createElement('script');
                        script.type = 'text/javascript';
                        script.language = 'JavaScript';
                        script.src = 'http://elmah.googlecode.com/svn/www/update.js?__=' + (new Date()).getTime();
                        document.getElementsByTagName('head')[0].appendChild(script);
                        return false;
                    }
                    var ELMAH = {
                        info : {
                            version     : '" + GetVersion() + @"',
                            fileVersion : '" + GetFileVersion() + @"',
                            type        : '" + Build.TypeLowercase + @"',
                            status      : '" + Build.Status + @"',
                            framework   : '" + Build.Framework + @"',
                            imageRuntime: '" + Build.ImageRuntimeVersion + @"'
                        }
                    };
                </script>");

            //
            // Title
            //
            
            writer.AddAttribute(HtmlTextWriterAttribute.Id, "PageTitle");
            writer.RenderBeginTag(HtmlTextWriterTag.H1);
            writer.Write(PageTitle);
            writer.RenderEndTag(); // </h1>
            writer.WriteLine();

            //
            // Speed Bar
            //

            SpeedBar.Render(writer,
                SpeedBar.Home.Format(BasePageName),
                SpeedBar.Help,
                SpeedBar.About.Format(BasePageName));

            //
            // Content...
            //

            writer.RenderBeginTag(HtmlTextWriterTag.P);
            writer.AddAttribute(HtmlTextWriterAttribute.Onclick, "return onCheckForUpdate(this)");
            writer.AddAttribute(HtmlTextWriterAttribute.Title, "Checks if your ELMAH version is up to date (requires Internet connection)");
            writer.RenderBeginTag(HtmlTextWriterTag.Button);
            writer.Write("Check for Update");
            writer.RenderEndTag(); // </button>
            writer.RenderEndTag(); // </p>

            SccStamp[] stamps = SccStamp.FindAll(typeof(ErrorLog).Assembly);
            SccStamp.SortByRevision(stamps, /* descending */ true);

            writer.RenderBeginTag(HtmlTextWriterTag.P);
            writer.Write("This <strong>{0}</strong> ", Build.TypeLowercase);
            
            if (stamps.Length > 0)
                writer.Write("(SCC #{0}) ", stamps[0].Revision.ToString("N0"));

            writer.Write("build was compiled from the following sources for CLR {0}:", Build.ImageRuntimeVersion);

            writer.RenderEndTag(); // </p>

            //
            // Stamps...
            //

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

        private Version GetVersion() 
        {
            return GetType().Assembly.GetName().Version;
        }

        private Version GetFileVersion()
        {
            AssemblyFileVersionAttribute version = (AssemblyFileVersionAttribute) Attribute.GetCustomAttribute(GetType().Assembly, typeof(AssemblyFileVersionAttribute));
            return version != null ? new Version(version.Version) : new Version();
        }
    }
}
