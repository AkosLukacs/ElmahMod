#region License, Terms and Author(s)

//
// ELMAH - Error Logging Modules and Handlers for ASP.NET
// Copyright (c) 2007 Atif Aziz. All rights reserved.
//
//  Author(s):
//
//      James Driscoll, mailto:jamesdriscoll@btinternet.com
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

//
// this module is not currently available for .Net 1.0
// if someone can get the context.RewritePath line working in 1.0, 
// then it can be used in 1.0 as well!!
//

#if !NET_1_0
namespace Elmah
{
    #region Imports

    using System;
    using System.Web;

    #endregion

    /// <summary>
    /// HTTP module that resolves issues in ELMAH when wilcard mapping
    /// is implemented in IIS 5.x.
    /// </summary>
    /// <remarks>
    /// See <a href="http://groups.google.com/group/elmah/browse_thread/thread/c22b85ace3812da1">Elmah 
    /// with existing wildcard mapping</a> for more information behind the 
    /// reason for this module.
    /// </remarks>

    public class FixIIS5xWildcardMappingModule : IHttpModule
    {
        //
        // Mainly cribbed from an idea at http://forums.asp.net/t/1113541.aspx.
        //

        public FixIIS5xWildcardMappingModule()
        {
        }

        public void Dispose()
        { 
        }

#if !NET_1_1
        public string GetHandlerPath()
        {
            System.Web.Configuration.HttpHandlersSection handlersSection = System.Configuration.ConfigurationManager.GetSection("system.web/httpHandlers") as System.Web.Configuration.HttpHandlersSection;
            string elmahHandlerTypeName = typeof(ErrorLogPageFactory).AssemblyQualifiedName;
            foreach (System.Web.Configuration.HttpHandlerAction handlerAction in handlersSection.Handlers)
                if (elmahHandlerTypeName.IndexOf(handlerAction.Type) == 0)
                    return handlerAction.Path;

            return null;
        }
#else
        private const string DefaultHandlerPath = "elmah.axd";
        public string GetHandlerPath()
        {
            System.Xml.XmlDocument xml = new System.Xml.XmlDocument();
            try
            {
                //
                // Try and load the web.config file
                //

                string webConfigFile = HttpContext.Current.Server.MapPath(HttpContext.Current.Request.ApplicationPath + "/web.config");
                xml.Load(webConfigFile);
            }
            catch (Exception)
            {
                //
                // There were issues loading web.config, so let's assume the default 
                // 

                return DefaultHandlerPath;
            }

            //
            // We are looking for the Elmah handler...
            // So we need to look in...
            // <configuration>
            //   <system.web>
            //     <httpHandlers>
            //       <add type="*ErrorLogPageFactory*" path="****" />
            // We use contains for the ErrorLogPageFactory so that we pick up all variations here
            // And we pull out the path node, as that contains what we want!
            //

            System.Xml.XmlNode node = xml.SelectSingleNode("/configuration/system.web/httpHandlers/add[contains(@type, 'ErrorLogPageFactory')]/@path");
            if (node != null)
                return node.InnerText;

            return null;
        }
#endif
        public void Init(HttpApplication context)
        {
            string handlerPath = GetHandlerPath();

            //
            // Only set things up if we've found the handler path
            //

            if (handlerPath != null && handlerPath.Length > 0)
            {
                _handlerPathWithForwardSlash = handlerPath;
                if (_handlerPathWithForwardSlash[_handlerPathWithForwardSlash.Length - 1] != '/')
                    _handlerPathWithForwardSlash += "/";

#if NET_1_1
                //
                // Convert to lower case as we will be comparing against that later
                //

                _handlerPathWithForwardSlash = _handlerPathWithForwardSlash.ToLower();
#endif
                _handlerPathLength = _handlerPathWithForwardSlash.Length -1;

                //
                // IIS 5.x with Wildcard mapping can't find the required
                // "elmah.axd" handler, so we need to intercept it
                // (which must happen when the request begins)
                // and then rewrite the path so that the handler is found.
                //

                context.BeginRequest += new EventHandler(OnBeginRequest);
            }
        }

        private string _handlerPathWithForwardSlash;
        private int _handlerPathLength;

        private void OnBeginRequest(object sender, EventArgs e)
        {
            HttpApplication app = sender as HttpApplication;
            HttpContext context = app.Context;
            string path = context.Request.Path;

            //
            // Check to see if we are dealing with a request for the "elmah.axd" handler
            // and if so, we need to rewrite the path!
            //

#if !NET_1_1
            int handlerPosition = path.IndexOf(_handlerPathWithForwardSlash, StringComparison.OrdinalIgnoreCase);
#else
            int handlerPosition = path.ToLower().IndexOf(_handlerPathWithForwardSlash);
#endif
            if (handlerPosition >= 0)
                context.RewritePath(
                    path.Substring(0, handlerPosition + _handlerPathLength),
                    path.Substring(handlerPosition + _handlerPathLength),
                    context.Request.QueryString.ToString());
        }
    }
}
#endif
