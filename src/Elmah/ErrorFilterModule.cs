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
    using System.Configuration;
    using System.Diagnostics;
    using System.Web;
    using Elmah.Assertions;

    #endregion

    /// <summary>
    /// HTTP module implementation that logs unhandled exceptions in an
    /// ASP.NET Web application to an error log.
    /// </summary>
    
    public class ErrorFilterModule : IHttpModule
    {
        private IAssertion _assertion = StaticAssertion.False;
        
        /// <summary>
        /// Initializes the module and prepares it to handle requests.
        /// </summary>

        public virtual void Init(HttpApplication application)
        {
            if (application == null)
                throw new ArgumentNullException("application");
            
            ErrorFilterConfiguration config = (ErrorFilterConfiguration) ConfigurationSettings.GetConfig("elmah/errorFilter");
            
            if (config == null)
                return;
            
            _assertion = config.Assertion;

            foreach (IHttpModule module in HttpModuleRegistry.GetModules(application))
            {
                IExceptionFiltering filtering = module as IExceptionFiltering;

                if (filtering != null)
                    filtering.Filtering += new ExceptionFilterEventHandler(OnErrorModuleFiltering);
            }
        }

        /// <summary>
        /// Disposes of the resources (other than memory) used by the module.
        /// </summary>
        
        public virtual void Dispose()
        {
        }

        public virtual IAssertion Assertion
        {
            get { return _assertion; }
        }

        protected virtual void OnErrorModuleFiltering(object sender, ExceptionFilterEventArgs args)
        {
            if (args == null)
                throw new ArgumentNullException("args");
            
            if (args.Exception == null)
                throw new ArgumentException(null, "args");
            
            // TODO: Consider making this robust in case an exception is thrown during testing of the assertion.
            
            if (Assertion.Test(new AssertionHelperContext(args.Exception, args.Context)))
                args.Dismiss();
        }

        internal sealed class AssertionHelperContext
        {
            private readonly Exception _exception;
            private readonly object _context;
            private Exception _baseException;
            private int _httpStatusCode;
            private bool _statusCodeInitialized;

            public AssertionHelperContext(Exception e, object context)
            {
                Debug.Assert(e != null);
                
                _exception = e;
                _context = context;
            }

            public Exception Exception
            {
                get { return _exception; }
            }

            public Exception BaseException
            {
                get
                {
                    if (_baseException == null)
                        _baseException = Exception.GetBaseException();
                    
                    return _baseException;
                }
            }

            public bool HasHttpStatusCode
            {
                get { return HttpStatusCode != 0; }
            }

            public int HttpStatusCode
            {
                get
                {
                    if (!_statusCodeInitialized)
                    {
                        _statusCodeInitialized = true;
                        
                        HttpException exception = Exception as HttpException;

                        if (exception != null)
                            _httpStatusCode = exception.GetHttpCode();
                    }
                    
                    return _httpStatusCode;
                }
            }

            public object Context
            {
                get { return _context; }
            }
        }
    }
}
