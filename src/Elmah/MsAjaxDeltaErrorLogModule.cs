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

// All code in this file requires .NET Framework 2.0 or later.

#if !NET_1_1 && !NET_1_0

[assembly: Elmah.Scc("$Id$")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Web;
    using System.Web.UI;

    #endregion

    /// <summary>
    /// Module to log unhandled exceptions during a delta-update
    /// request issued by the client when a page uses the UpdatePanel
    /// introduced with ASP.NET 2.0 AJAX Extensions.
    /// </summary>

    public class MsAjaxDeltaErrorLogModule : IHttpModule
    {
        public virtual void Init(HttpApplication context)
        {
            context.PostMapRequestHandler += OnPostMapRequestHandler;
        }

        public virtual void Dispose() { /* NOP */ }

        protected void OnPostMapRequestHandler(object sender, EventArgs args)
        {
            HttpContext context = ((HttpApplication) sender).Context;

            if (!IsAsyncPostBackRequest(context.Request))
                return;

            Page page = context.Handler as Page;

            if (page == null)
                return;

            page.Error += OnPageError;
        }

        protected virtual void OnPageError(object sender, EventArgs args)
        {
            Page page = (Page) sender;
            Exception exception = page.Server.GetLastError();

            if (exception == null)
                return;

            HttpContext context = HttpContext.Current;
            LogException(exception, context);
        }

        /// <summary>
        /// Logs an exception and its context to the error log.
        /// </summary>

        protected virtual void LogException(Exception e, HttpContext context)
        {
            ErrorSignal.FromContext(context).Raise(e, context);
        }

        protected virtual bool IsAsyncPostBackRequest(HttpRequest request)
        {
            if (request == null) 
                throw new ArgumentNullException("request");
            
            string[] values = request.Headers.GetValues("X-MicrosoftAjax");

            if (values == null || values.Length == 0)
                return false;

            foreach (string value in values)
            {
                if (string.Compare(value, "Delta=true", StringComparison.OrdinalIgnoreCase) == 0)
                    return true;
            }

            return false;
        }
    }
}

#endif //!NET_1_1 && !NET_1_0
