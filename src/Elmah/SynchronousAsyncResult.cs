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
    using System.Threading;

    #endregion

    internal sealed class SynchronousAsyncResult : IAsyncResult
    {
        private ManualResetEvent _waitHandle;
        private readonly string _syncMethodName;
        private readonly object _asyncState;
        private readonly object _result;
        private readonly Exception _exception;
        private bool _ended;

        public static SynchronousAsyncResult OnSuccess(string syncMethodName, object asyncState, object result)
        {
            return new SynchronousAsyncResult(syncMethodName, asyncState, result, null);
        }

        public static SynchronousAsyncResult OnFailure(string syncMethodName, object asyncState, Exception e)
        {
            Debug.Assert(e != null);

            return new SynchronousAsyncResult(syncMethodName, asyncState, null, e);
        }

        private SynchronousAsyncResult(string syncMethodName, object asyncState, object result, Exception e)
        {
            Debug.AssertStringNotEmpty(syncMethodName);

            _syncMethodName = syncMethodName;
            _asyncState = asyncState;
            _result = result;
            _exception = e;
        }

        public bool IsCompleted 
        {
            get { return true; }
        }

        public WaitHandle AsyncWaitHandle 
        {
            get
            {
                //
                // Create the async handle on-demand, assuming the caller
                // insists on having it even though CompletedSynchronously and
                // IsCompleted should make this redundant.
                //

                if (_waitHandle == null)
                    _waitHandle = new ManualResetEvent(true);
    
                return _waitHandle;
            }
        }

        public object AsyncState 
        {
            get { return _asyncState; }
        }

        public bool CompletedSynchronously 
        {
            get { return true; }
        }

        public object End()
        {
            if (_ended)
                throw new InvalidOperationException(string.Format("End{0} can only be called once for each asynchronous operation.", _syncMethodName));

            _ended = true;

            if (_exception != null)
                throw _exception;

            return _result;
        }
    }
}
