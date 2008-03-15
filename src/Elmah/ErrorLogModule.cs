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
    
    using Trace = System.Diagnostics.Trace;

    #endregion

    /// <summary>
    /// HTTP module implementation that logs unhandled exceptions in an
    /// ASP.NET Web application to an error log.
    /// </summary>
    
    public class ErrorLogModule : HttpModuleBase, IExceptionFiltering
    {
        public event ExceptionFilterEventHandler Filtering;
        public event ErrorLoggedEventHandler Logged;

        /// <summary>
        /// Initializes the module and prepares it to handle requests.
        /// </summary>
        
        protected override void OnInit(HttpApplication application)
        {
            if (application == null)
                throw new ArgumentNullException("application");
            
            application.Error += new EventHandler(OnError);
            ErrorSignal.Get(application).Raised += new ErrorSignalEventHandler(OnErrorSignaled);
        }

        /// <summary>
        /// Gets the <see cref="ErrorLog"/> instance to which the module
        /// will log exceptions.
        /// </summary>
        
        protected virtual ErrorLog GetErrorLog(HttpContext context)
        {
            return ErrorLog.GetDefault(context);
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
        /// The handler called when an exception is explicitly signaled.
        /// </summary>

        protected virtual void OnErrorSignaled(object sender, ErrorSignalEventArgs args)
        {
            LogException(args.Exception, args.Context);
        }

        /// <summary>
        /// Logs an exception and its context to the error log.
        /// </summary>

        protected virtual void LogException(Exception e, HttpContext context)
        {
            if (e == null)
                throw new ArgumentNullException("e");

            //
            // Fire an event to check if listeners want to filter out
            // logging of the uncaught exception.
            //

            ExceptionFilterEventArgs args = new ExceptionFilterEventArgs(e, context);
            OnFiltering(args);
            
            if (args.Dismissed)
                return;
            
            //
            // Log away...
            //

            ErrorLogEntry entry = null;

            try
            {
                Error error = new Error(e, context);
                ErrorLog log = GetErrorLog(context);
                string id = log.Log(error);
                entry = new ErrorLogEntry(log, id, error);
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

            if (entry != null)
                OnLogged(new ErrorLoggedEventArgs(entry));
        }

        /// <summary>
        /// Raises the <see cref="Logged"/> event.
        /// </summary>

        protected virtual void OnLogged(ErrorLoggedEventArgs args)
        {
            ErrorLoggedEventHandler handler = Logged;

            if (handler != null)
                handler(this, args);
        }

        /// <summary>
        /// Raises the <see cref="Filtering"/> event.
        /// </summary>

        protected virtual void OnFiltering(ExceptionFilterEventArgs args)
        {
            ExceptionFilterEventHandler handler = Filtering;
            
            if (handler != null)
                handler(this, args);
        }

        /// <summary>
        /// Determines whether the module will be registered for discovery
        /// in partial trust environments or not.
        /// </summary>

        protected override bool SupportDiscoverability
        {
            get { return true; }
        }
    }

    public delegate void ErrorLoggedEventHandler(object sender, ErrorLoggedEventArgs args);

    [ Serializable ]
    public sealed class ErrorLoggedEventArgs : EventArgs
    {
        private readonly ErrorLogEntry _entry;

        public ErrorLoggedEventArgs(ErrorLogEntry entry)
        {
            if (entry == null)
                throw new ArgumentNullException("entry");

            _entry = entry;
        }

        public ErrorLogEntry Entry
        {
            get { return _entry; }
        }
    }
}
