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
    using System.Collections;
    using System.Web;

    #endregion

    public sealed class ErrorSignal
    {
        public event ErrorSignalEventHandler Raised;

        private static Hashtable _signalByApp;
        private static readonly object _lock = new object();

        public void Raise(Exception e)
        {
            Raise(e, null);
        }

        public void Raise(Exception e, HttpContext context)
        {
            if (context == null)
                context = HttpContext.Current;

            ErrorSignalEventHandler handler = Raised;

            if (handler != null)
                handler(this, new ErrorSignalEventArgs(e, context));
        }

        public static ErrorSignal FromCurrentContext()
        {
            return FromContext(HttpContext.Current);
        }

        public static ErrorSignal FromContext(HttpContext context)
        {
            if (context == null) 
                throw new ArgumentNullException("context");

            return Get(context.ApplicationInstance);
        }

        public static ErrorSignal Get(HttpApplication application)
        {
            if (application == null)
                throw new ArgumentNullException("application");

            lock (_lock)
            {
                //
                // Allocate map of object per application on demand.
                //

                if (_signalByApp == null)
                    _signalByApp = new Hashtable();

                //
                // Get the list of modules fot the application. If this is
                // the first registration for the supplied application object
                // then setup a new and empty list.
                //

                ErrorSignal signal = (ErrorSignal) _signalByApp[application];

                if (signal == null)
                {
                    signal = new ErrorSignal();
                    _signalByApp.Add(application, signal);
                    application.Disposed += new EventHandler(OnApplicationDisposed);
                }

                return signal;
            }
        }

        private static void OnApplicationDisposed(object sender, EventArgs e)
        {
            HttpApplication application = (HttpApplication) sender;

            lock (_lock)
            {
                if (_signalByApp == null)
                    return;

                _signalByApp.Remove(application);
                
                if (_signalByApp.Count == 0)
                    _signalByApp = null;
            }
        }
    }

    public delegate void ErrorSignalEventHandler(object sender, ErrorSignalEventArgs args);

    [ Serializable ]
    public sealed class ErrorSignalEventArgs : EventArgs
    {
        private readonly Exception _exception;
        [ NonSerialized ]
        private readonly HttpContext _context;

        public ErrorSignalEventArgs(Exception e, HttpContext context)
        {
            if (e == null)
                throw new ArgumentNullException("e");

            _exception = e;
            _context = context;
        }

        public Exception Exception
        {
            get { return _exception; }
        }

        public HttpContext Context
        {
            get { return _context; }
        }

        public override string ToString()
        {
            return Mask.EmptyString(Exception.Message, Exception.GetType().FullName);
        }
    }
}