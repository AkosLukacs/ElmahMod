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

    #endregion
    
    public interface IExceptionFiltering
    {
        event ExceptionFilterEventHandler Filtering;
    }

    public delegate void ExceptionFilterEventHandler(object sender, ExceptionFilterEventArgs args);

    [ Serializable ]
    public sealed class ExceptionFilterEventArgs : EventArgs
    {
        private readonly Exception _exception;
        [ NonSerialized ] private readonly object _context;
        private bool _dismissed;

        public ExceptionFilterEventArgs(Exception e, object context)
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

        public object Context
        {
            get { return _context; }
        }

        public bool Dismissed
        {
            get { return _dismissed; }
        }
        
        public void Dismiss()
        {
            _dismissed = true;
        }
    }
}