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
    using System.Data.SqlClient;
    using IList = System.Collections.IList;
    using HttpRuntime = System.Web.HttpRuntime;

    #endregion

    /// <summary>
    /// Represents an error log capable of storing and retrieving errors
    /// generated in an ASP.NET Web application.
    /// </summary>

    public abstract class ErrorLog
    {
        [ ThreadStatic ] private static ErrorLog _defaultLog;
        
#if !NET_1_1 && !NET_1_0
        private string _appName;
#endif

        /// <summary>
        /// Logs an error in log for the application.
        /// </summary>
        
        public abstract string Log(Error error);

        /// <summary>
        /// When overridden in a subclass, begins an asynchronous version 
        /// of <see cref="Log"/>.
        /// </summary>

        public virtual IAsyncResult BeginLog(Error error, AsyncCallback asyncCallback, object asyncState)
        {
            return BeginSyncImpl(asyncCallback, asyncState, new LogHandler(Log), error);
        }

        /// <summary>
        /// When overridden in a subclass, ends an asynchronous version 
        /// of <see cref="Log"/>.
        /// </summary>

        public virtual string EndLog(IAsyncResult asyncResult)
        {
            return (string) EndSyncImpl(asyncResult);
        }

        private delegate string LogHandler(Error error);

        /// <summary>
        /// Retrieves a single application error from log given its 
        /// identifier, or null if it does not exist.
        /// </summary>

        public abstract ErrorLogEntry GetError(string id);

        /// <summary>
        /// When overridden in a subclass, begins an asynchronous version 
        /// of <see cref="GetError"/>.
        /// </summary>

        public virtual IAsyncResult BeginGetError(string id, AsyncCallback asyncCallback, object asyncState)
        {
            return BeginSyncImpl(asyncCallback, asyncState, new GetErrorHandler(GetError), id);
        }

        /// <summary>
        /// When overridden in a subclass, ends an asynchronous version 
        /// of <see cref="GetError"/>.
        /// </summary>

        public virtual ErrorLogEntry EndGetError(IAsyncResult asyncResult)
        {
            return (ErrorLogEntry) EndSyncImpl(asyncResult);
        }

        private delegate ErrorLogEntry GetErrorHandler(string id);

        /// <summary>
        /// Retrieves a page of application errors from the log in 
        /// descending order of logged time.
        /// </summary>
        
        public abstract int GetErrors(int pageIndex, int pageSize, IList errorEntryList);

        /// <summary>
        /// When overridden in a subclass, begins an asynchronous version 
        /// of <see cref="GetErrors"/>.
        /// </summary>

        public virtual IAsyncResult BeginGetErrors(int pageIndex, int pageSize, IList errorEntryList, AsyncCallback asyncCallback, object asyncState)
        {
            return BeginSyncImpl(asyncCallback, asyncState, new GetErrorsHandler(GetErrors), pageIndex, pageSize, errorEntryList);
        }

        /// <summary>
        /// When overridden in a subclass, ends an asynchronous version 
        /// of <see cref="GetErrors"/>.
        /// </summary>
        
        public virtual int EndGetErrors(IAsyncResult asyncResult)
        {
            return (int) EndSyncImpl(asyncResult);
        }

        private delegate int GetErrorsHandler(int pageIndex, int pageSize, IList errorEntryList);

        /// <summary>
        /// Get the name of this log.
        /// </summary>

        public virtual string Name
        {
            get { return this.GetType().Name; }   
        }

        /// <summary>
        /// Gets the name of the application to which the log is scoped.
        /// </summary>
        
        public virtual string ApplicationName
        {
            get
            {
#if NET_1_1 || NET_1_0
                return HttpRuntime.AppDomainAppId;
#else
                if (_appName == null)
                {
                    string path = HttpRuntime.AppDomainAppVirtualPath ?? string.Empty;
                    string[] parts = path.Split('/');
                    _appName = Mask.EmptyString(parts[parts.Length - 1], "/"); 
                }

                return _appName;
#endif
            }
        }

        /// <summary>
        /// Gets the default error log implementation specified in the 
        /// configuration file, or the in-memory log implemention if
        /// none is configured.
        /// </summary>
        
        public static ErrorLog Default
        {
            get 
            { 
                if (_defaultLog == null)
                {
                    //
                    // Determine the default store type from the configuration and 
                    // create an instance of it.
                    //

                    ErrorLog log = (ErrorLog) SimpleServiceProviderFactory.CreateFromConfigSection(Configuration.GroupSlash + "errorLog");

                    //
                    // If no object got created (probably because the right 
                    // configuration settings are missing) then default to 
                    // the in-memory log implementation.
                    //

                    _defaultLog = log != null ? log : new MemoryErrorLog();
                }

                return _defaultLog;
            }
        }

        //
        // The following two methods are helpers that provide boilerplate 
        // implementations for implementing asnychronous BeginXXXX and 
        // EndXXXX methods over a default synchronous implementation.
        //

        private static IAsyncResult BeginSyncImpl(AsyncCallback asyncCallback, object asyncState, Delegate syncImpl, params object[] args)
        {
            Debug.Assert(syncImpl != null);

            SynchronousAsyncResult asyncResult;
            string syncMethodName = syncImpl.Method.Name;

            try
            {
                asyncResult = SynchronousAsyncResult.OnSuccess(syncMethodName, asyncState, 
                    syncImpl.DynamicInvoke(args));
            }
            catch (Exception e)
            {
                asyncResult = SynchronousAsyncResult.OnFailure(syncMethodName, asyncState, e);
            }

            if (asyncCallback != null)
                asyncCallback(asyncResult);

            return asyncResult;
        }

        private static object EndSyncImpl(IAsyncResult asyncResult)
        {
            if (asyncResult == null)
                throw new ArgumentNullException("asyncResult");

            SynchronousAsyncResult syncResult = asyncResult as SynchronousAsyncResult;

            if (syncResult == null)
                throw new ArgumentException("IAsyncResult object did not come from the corresponding async method on this type.", "asyncResult");

            //
            // IMPORTANT! The End method on SynchronousAsyncResult will 
            // throw an exception if that's what Log did when 
            // BeginLog called it. The unforunate side effect of this is
            // the stack trace information for the exception is lost and 
            // reset to this point. There seems to be a basic failure in the 
            // framework to accommodate for this case more generally. One 
            // could handle this through a custom exception that wraps the 
            // original exception, but this assumes that an invocation will 
            // only throw an exception of that custom type.
            //

            return syncResult.End();
        }
    }
}
