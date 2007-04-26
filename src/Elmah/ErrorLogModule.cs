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
    using System.Web;
    
    using Trace = System.Diagnostics.Trace;

    #endregion

    /// <summary>
    /// HTTP module implementation that logs unhandled exceptions in an
    /// ASP.NET Web application to an error log.
    /// </summary>
    
    public class ErrorLogModule : IHttpModule
    {

        /// <summary>
        /// Initializes the module and prepares it to handle requests.
        /// </summary>

        public virtual void Init(HttpApplication application)
        {
            if (application == null)
                throw new ArgumentNullException("application");

            application.Error += new EventHandler(OnError);
        }

        /// <summary>
        /// Disposes of the resources (other than memory) used by the module.
        /// </summary>
        
        public virtual void Dispose()
        {
        }

        /// <summary>
        /// Gets the <see cref="ErrorLog"/> instance to which the module
        /// will log exceptions.
        /// </summary>
        
        protected virtual ErrorLog ErrorLog
        {
            get { return ErrorLog.Default; }
        }

        /// <summary>
        /// The handler called when an unhandled exception bubbles up to 
        /// the module.
        /// </summary>

        protected virtual void OnError(object sender, EventArgs args)
        {
            HttpApplication application = (HttpApplication) sender;
            LogException(application.Server.GetLastError(), application.Context);
        }

        /// <summary>
        /// Logs an exception and its context to the error log.
        /// </summary>

        protected virtual void LogException(Exception e, HttpContext context)
        {
            if (e == null)
                throw new ArgumentNullException("e");

            try
            {
                this.ErrorLog.Log(new Error(e, context));
            }
            catch (Exception localException)
            {
                //
                // IMPORTANT! We swallow any exception raised during the 
                // logging and send them out to the trace . The idea 
                // here is that logging of exceptions by itself should not 
                // be  critical to the overall operation of the application.
                // The bad thing is that we catch ANY kind of exception, 
                // even system ones and potentially let them slip by.
                //

                Trace.WriteLine(localException);
            }
        }
    }
}
