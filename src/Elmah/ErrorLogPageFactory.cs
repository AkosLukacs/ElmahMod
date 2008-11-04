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

    using System.Collections;
    using System.Web;

    using CultureInfo = System.Globalization.CultureInfo;
    using Encoding = System.Text.Encoding;

    #endregion

    /// <summary>
    /// HTTP handler factory that dispenses handlers for rendering views and 
    /// resources needed to display the error log.
    /// </summary>

    public class ErrorLogPageFactory : IHttpHandlerFactory
    {
        private static readonly object _authorizationHandlersKey = new object();
        private static readonly IRequestAuthorizationHandler[] _zeroAuthorizationHandlers = new IRequestAuthorizationHandler[0];

        /// <summary>
        /// Returns an object that implements the <see cref="IHttpHandler"/> 
        /// interface and which is responsible for serving the request.
        /// </summary>
        /// <returns>
        /// A new <see cref="IHttpHandler"/> object that processes the request.
        /// </returns>

        public virtual IHttpHandler GetHandler(HttpContext context, string requestType, string url, string pathTranslated)
        {
            //
            // The request resource is determined by the looking up the
            // value of the PATH_INFO server variable.
            //

            string resource = context.Request.PathInfo.Length == 0 ? string.Empty :
                context.Request.PathInfo.Substring(1).ToLower(CultureInfo.InvariantCulture);

            IHttpHandler handler = FindHandler(resource);

            if (handler == null)
                throw new HttpException(404, "Resource not found.");

            //
            // Check if authorized then grant or deny request.
            //

            int authorized = IsAuthorized(context);
            if (authorized == 0
                || (authorized < 0 // Compatibility case...
                    && !HttpRequestSecurity.IsLocal(context.Request) 
                    && !SecurityConfiguration.Default.AllowRemoteAccess))
            {
                (new ManifestResourceHandler("RemoteAccessError.htm", "text/html")).ProcessRequest(context);
                HttpResponse response = context.Response;
                response.Status = "403 Forbidden";
                response.End();

                //
                // HttpResponse.End docs say that it throws 
                // ThreadAbortException and so should never end up here but
                // that's not been the observation in the debugger. So as a
                // precautionary measure, bail out anyway.
                //

                return null;
            }

            return handler;
        }

        private static IHttpHandler FindHandler(string name) 
        {
            Debug.Assert(name != null);

            switch (name)
            {
                case "detail":
                    return new ErrorDetailPage();

                case "html":
                    return new ErrorHtmlPage();

                case "xml":
                    return new ErrorXmlHandler();

                case "json":
                    return new ErrorJsonHandler();

                case "rss":
                    return new ErrorRssHandler();

                case "digestrss":
                    return new ErrorDigestRssHandler();

                case "download":
                    return new ErrorLogDownloadHandler();

                case "stylesheet":
                    return new ManifestResourceHandler("ErrorLog.css",
                        "text/css", Encoding.GetEncoding("Windows-1252"));

                case "test":
                    throw new TestException();

                case "about":
                    return new AboutPage();

                default:
                    return name.Length == 0 ? new ErrorLogPage() : null;
            }
        }

        /// <summary>
        /// Enables the factory to reuse an existing handler instance.
        /// </summary>

        public virtual void ReleaseHandler(IHttpHandler handler)
        {
        }

        /// <summary>
        /// Determines if the request is authorized by objects implementing
        /// <see cref="IRequestAuthorizationHandler" />.
        /// </summary>
        /// <returns>
        /// Returns zero if unauthorized, a value greater than zero if 
        /// authorized otherwise a value less than zero if no handlers
        /// were available to answer.
        /// </returns>

        private static int IsAuthorized(HttpContext context)
        {
            Debug.Assert(context != null);

            int authorized = /* uninitialized */ -1;
            IEnumerator authorizationHandlers = GetAuthorizationHandlers(context).GetEnumerator();
            while (authorized != 0 && authorizationHandlers.MoveNext())
            {
                IRequestAuthorizationHandler authorizationHandler = (IRequestAuthorizationHandler)authorizationHandlers.Current;
                authorized = authorizationHandler.Authorize(context) ? 1 : 0;
            }
            return authorized;
        }

        private static IList GetAuthorizationHandlers(HttpContext context)
        {
            Debug.Assert(context != null);

            object key = _authorizationHandlersKey;
            IList handlers = (IList)context.Items[key];

            if (handlers == null)
            {
                const int capacity = 4;
                ArrayList list = null;

                HttpApplication application = context.ApplicationInstance;
                if (application is IRequestAuthorizationHandler)
                {
                    list = new ArrayList(capacity);
                    list.Add(application);
                }

                foreach (IHttpModule module in HttpModuleRegistry.GetModules(application))
                {
                    if (module is IRequestAuthorizationHandler)
                    {
                        if (list == null)
                            list = new ArrayList(capacity);
                        list.Add(module);
                    }
                }

                context.Items[key] = handlers = ArrayList.ReadOnly(
                    list != null
                    ? list.ToArray(typeof(IRequestAuthorizationHandler))
                    : _zeroAuthorizationHandlers);
            }

            return handlers;
        }
    }

    public interface IRequestAuthorizationHandler
    {
        bool Authorize(HttpContext context);
    }
}
