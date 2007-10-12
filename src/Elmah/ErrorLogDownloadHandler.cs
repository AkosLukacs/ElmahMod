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
    using System.Globalization;
    using System.IO;
    using System.Threading;
    using System.Web;

    #endregion

    internal sealed class ErrorLogDownloadHandler : IHttpAsyncHandler
    {
        private const int _pageSize = 100;
        private static readonly TimeSpan _beatPollInterval = TimeSpan.FromSeconds(3);

        private AsyncResult _result;
        private ErrorLog _log;
        private int _pageIndex;
        private DateTime _lastBeatTime;
        private ArrayList _errorEntryList;
        private HttpContext _context;
        private AsyncCallback _callback;

        public void ProcessRequest(HttpContext context)
        {
            EndProcessRequest(BeginProcessRequest(context, null, null));
        }

        public bool IsReusable
        {
            get { return false; }
        }

        public IAsyncResult BeginProcessRequest(HttpContext context, AsyncCallback cb, object extraData)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            if (_result != null)
                throw new InvalidOperationException("An asynchronous operation is already pending.");

            HttpResponse response = context.Response;
            response.BufferOutput = false;

            response.AppendHeader("Content-Type", "text/csv; header=present");
            response.AppendHeader("Content-Disposition", "attachment; filename=errorlog.csv");

            response.Output.Write("Application,Host,Time,Unix Time,Type,Source,User,Status Code,Message,URL\r\n");

            _result = new AsyncResult(extraData);
            _log = ErrorLog.GetDefault(context);
            _pageIndex = 0;
            _lastBeatTime = DateTime.Now;
            _context = context;
            _callback = cb;
            _errorEntryList = new ArrayList(_pageSize);

            _log.BeginGetErrors(0, _pageSize, _errorEntryList, 
                new AsyncCallback(GetErrorsCallback), null);

            return _result;
        }

        public void EndProcessRequest(IAsyncResult result)
        {
            if (result == null)
                throw new ArgumentNullException("result");
            
            if (result != _result)
                throw new ArgumentException(null, "result");

            _result = null;
            _log = null;
            _context = null;
            _callback = null;
            _errorEntryList = null;

            ((AsyncResult) result).End();
        }

        private void GetErrorsCallback(IAsyncResult result)
        {
            Debug.Assert(result != null);

            try
            {
                TryGetErrorsCallback(result);
            }
            catch (Exception e)
            {
                //
                // If anything goes wrong during the processing of the 
                // callback then the exception needs to be captured
                // and the raising delayed until EndProcessRequest.
                // Meanwhile, the BeginProcessRequest called is notified
                // immediately of completion.
                //

                _result.Complete(_callback, e);
            }
        }

        private void TryGetErrorsCallback(IAsyncResult result) 
        {
            Debug.Assert(result != null);

            _log.EndGetErrors(result);

            HttpResponse response = _context.Response;

            if (_errorEntryList.Count == 0)
            {
                response.Flush();
                _result.Complete(false, _callback);
                return;
            }

            //
            // Setup to emit CSV records.
            //

            StringWriter writer = new StringWriter();
            writer.NewLine = "\r\n";
            CsvWriter csv = new CsvWriter(writer);

            CultureInfo culture = CultureInfo.InvariantCulture;
            DateTime epoch = new DateTime(1970, 1, 1);

            //
            // For each error, emit a CSV record.
            //

            foreach (ErrorLogEntry entry in _errorEntryList)
            {
                Error error = entry.Error;
                DateTime time = error.Time.ToUniversalTime();
                Uri url = new Uri(_context.Request.Url, "detail?id=" + entry.Id);

                csv.Field(error.ApplicationName)
                    .Field(error.HostName)
                    .Field(time.ToString("yyyy-MM-dd hh:mm:ss", culture))
                    .Field(time.Subtract(epoch).TotalSeconds.ToString("0.0000", culture))
                    .Field(error.Type)
                    .Field(error.Source)
                    .Field(error.User)
                    .Field(error.StatusCode.ToString(culture))
                    .Field(error.Message)
                    .Field(url.ToString())
                    .Record();
            }

            response.Output.Write(writer.ToString());
            response.Flush();

            //
            // Poll whether the client is still connected so we are not
            // unnecessarily continue sending data to an abandoned 
            // connection. This check is only performed at certain
            // intervals.
            //

            if (DateTime.Now - _lastBeatTime > _beatPollInterval)
            {
                if (!response.IsClientConnected)
                {
                    _result.Complete(true, _callback);
                    return;
                }

                _lastBeatTime = DateTime.Now;
            }

            //
            // More or done?
            //

            _errorEntryList.Clear();

            _log.BeginGetErrors(++_pageIndex, _pageSize, _errorEntryList,
                new AsyncCallback(GetErrorsCallback), null);
        }

        private sealed class AsyncResult : IAsyncResult
        {
            private readonly object _lock = new object();
            private ManualResetEvent _event;
            private readonly object _userState;
            private bool _completed;
            private Exception _exception;
            private bool _ended;
            private bool _aborted;

            internal event EventHandler Completed;

            public AsyncResult(object userState)
            {
                _userState = userState;
            }

            public bool IsCompleted
            {
                get { return _completed; }
            }

            public WaitHandle AsyncWaitHandle
            {
                get
                {
                    if (_event == null)
                    {
                        lock (_lock)
                        {
                            if (_event == null)
                                _event = new ManualResetEvent(_completed);
                        }
                    }

                    return _event;
                }
            }

            public object AsyncState
            {
                get { return _userState; }
            }

            public bool CompletedSynchronously
            {
                get { return false; }
            }

            internal void Complete(bool aborted, AsyncCallback callback)
            {
                if (_completed)
                    throw new InvalidOperationException();

                _aborted = aborted;

                try
                {
                    lock (_lock)
                    {
                        _completed = true;

                        if (_event != null)
                            _event.Set();
                    }

                    if (callback != null)
                        callback(this);
                }
                finally
                {
                    OnCompleted();
                }
            }

            internal void Complete(AsyncCallback callback, Exception e)
            {
                _exception = e;
                Complete(false, callback);
            }

            internal bool End()
            {
                if (_ended)
                    throw new InvalidOperationException();

                _ended = true;

                if (!IsCompleted)
                    AsyncWaitHandle.WaitOne();

                if (_event != null)
                    _event.Close();

                if (_exception != null)
                    throw _exception;

                return _aborted;
            }

            private void OnCompleted()
            {
                EventHandler handler = Completed;

                if (handler != null)
                    handler(this, EventArgs.Empty);
            }
        }

        private sealed class CsvWriter
        {
            private readonly TextWriter _writer;
            private int _column;

            private static readonly char[] _reserved = new char[] { '\"', ',', '\r', '\n' };

            public CsvWriter(TextWriter writer)
            {
                Debug.Assert(writer != null);

                _writer = writer;
            }

            public CsvWriter Record()
            {
                _writer.WriteLine();
                _column = 0;
                return this;
            }

            public CsvWriter Field(string value)
            {
                if (_column > 0)
                    _writer.Write(',');

                // 
                // Fields containing line breaks (CRLF), double quotes, and commas 
                // need to be enclosed in double-quotes. 
                //

                int index = value.IndexOfAny(_reserved);

                if (index < 0)
                {
                    _writer.Write(value);
                }
                else
                {
                    //
                    // As double-quotes are used to enclose fields, then a 
                    // double-quote appearing inside a field must be escaped by 
                    // preceding it with another double quote. 
                    //

                    const string quote = "\"";
                    _writer.Write(quote);
                    _writer.Write(value.Replace(quote, quote + quote));
                    _writer.Write(quote);
                }

                _column++;
                return this;
            }
        }
    }
}