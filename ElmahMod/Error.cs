#region License, Terms and Author(s)
//
// ELMAH - Error Logging Modules and Handlers for ASP.NET
// Copyright (c) 2004-9 Atif Aziz. All rights reserved.
//
//  Author(s):
//
//      Atif Aziz, http://www.raboof.com
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//    http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
#endregion

[assembly: Elmah.Scc("$Id$")]

namespace Elmah
{
    #region Imports

    using System;
    using System.Security.Principal;
    using System.Web;
    using System.Xml;
    using Thread = System.Threading.Thread;
    using NameValueCollection = System.Collections.Specialized.NameValueCollection;
    using System.Collections;
    using System.Text;
    using System.Collections.Generic;

    #endregion



    /// <summary>
    /// Represents a logical application error (as opposed to the actual 
    /// exception it may be representing).
    /// </summary>
    [Serializable]
    public sealed class Error : ICloneable
    {
        const string ELMAH_MOD_DEFAULT_TRACE_KEY = "ELMAH_MOD_DEFAULT_TRACE_KEY";

        private readonly Exception _exception;
        private string _applicationName;
        private string _hostName;
        private string _typeName;
        private string _source;
        private string _message;
        private string _detail;
        private string _user;
        private DateTime _time;
        private int _statusCode;
        private string _webHostHtmlMessage;
        private NameValueCollection _serverVariables;
        private NameValueCollection _queryString;
        private NameValueCollection _form;
        private NameValueCollection _cookies;

        //mod
        private Error _innerError;
        private string _traceKey = ELMAH_MOD_DEFAULT_TRACE_KEY;
        private String _traceMsg;
        private IDictionary _data;
        //mod

        /// <summary>
        /// Initializes a new instance of the <see cref="Error"/> class.
        /// </summary>

        public Error() { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Error"/> class
        /// from a given <see cref="Exception"/> instance.
        /// </summary>

        public Error(Exception e) :
            this(e, null) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="Error"/> class
        /// from a given <see cref="Exception"/> instance and 
        /// <see cref="HttpContext"/> instance representing the HTTP 
        /// context during the exception.
        /// </summary>

        public Error(Exception e, HttpContext context)
        {
            if (e == null)
                throw new ArgumentNullException("e");

            _exception = e;
            Exception baseException = e.GetBaseException();

            //
            // Load the basic information.
            //

            _hostName = Environment.TryGetMachineName(context);
            _typeName = baseException.GetType().FullName;
            _message = baseException.Message;
            _source = baseException.Source;
            //_detail = e.ToString();
            _user = Thread.CurrentPrincipal.Identity.Name ?? string.Empty;

            _data = e.Data;

            //based on david.wh...'s code from here: http://code.google.com/p/elmah/issues/detail?id=162&colspec=ID%20Type%20Status%20Priority%20Stars%20Milestone%20Owner%20Summary
            // begin change to exception details to include stack trace and exception data
            StringBuilder sbDetail = new StringBuilder();
            sbDetail.Append("Stack Trace:");
            sbDetail.Append(System.Environment.NewLine);
            sbDetail.Append(System.Environment.NewLine);
            sbDetail.Append(e.ToString());
            sbDetail.Append(System.Environment.NewLine);
            sbDetail.Append(System.Environment.NewLine);
            sbDetail.Append("Exception Data (" + e.Data.Keys.Count.ToString() + " items):");

            sbDetail.Append(WriteIDictionary(e.Data, System.Environment.NewLine + "    "));

            _detail = sbDetail.ToString();
            // end change

            //Check for trace message
            if (context != null && context.Items[_traceKey] != null)
            {
                try
                {
                    //this may fail...
                    _traceMsg = context.Items[_traceKey].ToString();
                }
                catch (Exception ex)
                {
                    _traceMsg = String.Format(@"Error getting trace message: {0}", ex);
                }
            }

            //innerException / inner Error. 
            //Initializing the "Context" of the inner Error to null, since don't want to log the same HttpContext more than once.
            if (e.InnerException != null) { this._innerError = new Error(e.InnerException, null); }

            _time = DateTime.Now;

            //
            // If this is an HTTP exception, then get the status code
            // and detailed HTML message provided by the host.
            //

            HttpException httpException = e as HttpException;

            if (httpException != null)
            {
                _statusCode = httpException.GetHttpCode();
                _webHostHtmlMessage = httpException.GetHtmlErrorMessage() ?? string.Empty;
            }

            //
            // If the HTTP context is available, then capture the
            // collections that represent the state request as well as
            // the user.
            //

            if (context != null)
            {
                IPrincipal webUser = context.User;
                if (webUser != null
                    && (webUser.Identity.Name ?? string.Empty).Length > 0)
                {
                    _user = webUser.Identity.Name;
                }

                HttpRequest request = context.Request;

                _serverVariables = CopyCollection(request.ServerVariables);
                _queryString = CopyCollection(request.QueryString);
                _form = CopyCollection(request.Form);
                _cookies = CopyCollection(request.Cookies);
                /*
                //TODO:ezt ellenõrizni
                //2011.08.09 13:37 - LAK - mod: A request.Form Request Validation Exception-t dobhat, ha XML-lel lett meghívva például. Még akkor is, ha az eredeti requestet kezelõ nem halt meg.
                try
                { _serverVariables = CopyCollection(request.ServerVariables); }
                catch (HttpRequestValidationException ex)
                {
                    _serverVariables = new NameValueCollection();
                    _serverVariables.Add("ex", ex.Message);
                }

                try
                { _queryString = CopyCollection(request.QueryString); }
                catch (HttpRequestValidationException ex)
                {
                    _queryString = new NameValueCollection();
                    _queryString.Add("ex", ex.Message);
                }

                try
                { _form = CopyCollection(request.Form); }
                catch (HttpRequestValidationException ex)
                {
                    _form = new NameValueCollection();
                    _form.Add("ex", ex.Message);
                }

                try
                { _cookies = CopyCollection(request.Cookies); }
                catch (HttpRequestValidationException ex)
                {
                    _cookies = new NameValueCollection();
                    _cookies.Add("ex", ex.Message);
                }

                */
            }
        }

        private static string FormatObject(object obj, string indent)
        {
            string result = "";

            //if (obj is NameValueCollection)
            //{ result = WriteNVC((NameValueCollection)obj, indent + "  "); }
            //else
            {
                if (obj is IDictionary)
                { result = WriteIDictionary((IDictionary)obj, indent + "  "); }
                else
                    if (obj is IEnumerable && !(obj is string))
                    { result = WriteIEnumerable((IEnumerable)obj, indent + "  "); }
                    else
                    { result = ((obj == null) ? "[null]" : obj.ToString()); }
            }
            return result;
        }


        private static string WriteIDictionary(IDictionary dict, string indent)
        {
            if (dict == null) { return "[null]"; }
            if (dict.Keys.Count == 0) { return "[empty]"; }

            //StringBuilder sbDetail = new StringBuilder("[IDictionary]");
            StringBuilder sbDetail = new StringBuilder("[" + dict.GetType().ToString() + "]");

            foreach (object key in dict.Keys)
            {
                string detail = FormatObject(dict[key], indent);
                sbDetail.AppendFormat(indent + "'{0}' = {1}", key, detail);
            }

            return sbDetail.ToString();
        }

        /// <summary>
        /// Write IEnumerable to details. 
        /// </summary>
        /// <param name="coll"></param>
        /// <param name="indent"></param>
        /// <returns></returns>
        private static string WriteIEnumerable(IEnumerable coll, string indent)
        {
            if (coll == null) { return "[null]"; }

            IEnumerator enumerator = coll.GetEnumerator();
            //StringBuilder sbDetail = new StringBuilder("[IEnumerable]");
            StringBuilder sbDetail = new StringBuilder("[" + coll.GetType().ToString() + "]");

            while (enumerator.MoveNext())
            {
                string detail = FormatObject(enumerator.Current, indent);
                sbDetail.AppendFormat(indent + " * " + detail);
            }

            return sbDetail.ToString();
        }



        //private static string WriteNVC(NameValueCollection nvc, string indent)
        //{
        //    if (nvc == null) { return "[null]"; }
        //    if (nvc.Keys.Count == 0) { return "[empty]"; }

        //    //StringBuilder sbDetail = new StringBuilder("[NameValueCollection]");
        //    StringBuilder sbDetail = new StringBuilder(nvc.GetType().ToString());

        //    foreach (string key in nvc.Keys)
        //    {
        //        string detail = FormatObject(nvc[key], indent);
        //        sbDetail.AppendFormat(indent + "'{0}' = {1}", key, detail);
        //    }

        //    return sbDetail.ToString();
        //}
        /// <summary>
        /// Gets the <see cref="Exception"/> instance used to initialize this
        /// instance.
        /// </summary>
        /// <remarks>
        /// This is a run-time property only that is not written or read 
        /// during XML serialization via <see cref="ErrorXml.Decode"/> and 
        /// <see cref="ErrorXml.Encode(Error,XmlWriter)"/>.
        /// </remarks>

        public Exception Exception
        {
            get { return _exception; }
        }

        public Error InnerError
        {
            get { return _innerError; }
            set { _innerError = value; }
        }
        /// <summary>
        /// Gets or sets the name of application in which this error occurred.
        /// </summary>

        public string ApplicationName
        {
            get { return _applicationName ?? string.Empty; }
            set { _applicationName = value; }
        }

        public IDictionary Data
        {
            get { return _data; }
            set { _data = value; }
        }

        public string TraceMsg
        {
            get { return _traceMsg ?? string.Empty; }
            set { _traceMsg = value; }
        }

        /// <summary>
        /// The key for the trace message int HttpContex.Items
        /// </summary>
        public string TraceKey
        {
            get { return _traceKey ?? ELMAH_MOD_DEFAULT_TRACE_KEY; }
            set { _traceKey = value; }
        }

        /// <summary>
        /// Gets or sets name of host machine where this error occurred.
        /// </summary>
        public string HostName
        {
            get { return _hostName ?? string.Empty; }
            set { _hostName = value; }
        }

        /// <summary>
        /// Gets or sets the type, class or category of the error.
        /// </summary>

        public string Type
        {
            get { return _typeName ?? string.Empty; }
            set { _typeName = value; }
        }

        /// <summary>
        /// Gets or sets the source that is the cause of the error.
        /// </summary>

        public string Source
        {
            get { return _source ?? string.Empty; }
            set { _source = value; }
        }

        /// <summary>
        /// Gets or sets a brief text describing the error.
        /// </summary>

        public string Message
        {
            get { return _message ?? string.Empty; }
            set { _message = value; }
        }

        /// <summary>
        /// Gets or sets a detailed text describing the error, such as a
        /// stack trace.
        /// </summary>

        public string Detail
        {
            get { return _detail ?? string.Empty; }
            set { _detail = value; }
        }

        /// <summary>
        /// Gets or sets the user logged into the application at the time 
        /// of the error.
        /// </summary>

        public string User
        {
            get { return _user ?? string.Empty; }
            set { _user = value; }
        }

        /// <summary>
        /// Gets or sets the date and time (in local time) at which the 
        /// error occurred.
        /// </summary>

        public DateTime Time
        {
            get { return _time; }
            set { _time = value; }
        }

        /// <summary>
        /// Gets or sets the HTTP status code of the output returned to the 
        /// client for the error.
        /// </summary>
        /// <remarks>
        /// For cases where this value cannot always be reliably determined, 
        /// the value may be reported as zero.
        /// </remarks>

        public int StatusCode
        {
            get { return _statusCode; }
            set { _statusCode = value; }
        }

        /// <summary>
        /// Gets or sets the HTML message generated by the web host (ASP.NET) 
        /// for the given error.
        /// </summary>

        public string WebHostHtmlMessage
        {
            get { return _webHostHtmlMessage ?? string.Empty; }
            set { _webHostHtmlMessage = value; }
        }

        /// <summary>
        /// Gets a collection representing the Web server variables
        /// captured as part of diagnostic data for the error.
        /// </summary>

        public NameValueCollection ServerVariables
        {
            get { return FaultIn(ref _serverVariables); }
        }

        /// <summary>
        /// Gets a collection representing the Web query string variables
        /// captured as part of diagnostic data for the error.
        /// </summary>

        public NameValueCollection QueryString
        {
            get { return FaultIn(ref _queryString); }
        }

        /// <summary>
        /// Gets a collection representing the form variables captured as 
        /// part of diagnostic data for the error.
        /// </summary>

        public NameValueCollection Form
        {
            get { return FaultIn(ref _form); }
        }

        /// <summary>
        /// Gets a collection representing the client cookies
        /// captured as part of diagnostic data for the error.
        /// </summary>

        public NameValueCollection Cookies
        {
            get { return FaultIn(ref _cookies); }
        }

        /// <summary>
        /// Returns the value of the <see cref="Message"/> property.
        /// </summary>

        public override string ToString()
        {
            return this.Message;
        }

        /// <summary>
        /// Creates a new object that is a copy of the current instance.
        /// </summary>

        object ICloneable.Clone()
        {
            //
            // Make a base shallow copy of all the members.
            //

            Error copy = (Error)MemberwiseClone();

            //
            // Now make a deep copy of items that are mutable.
            //

            copy._serverVariables = CopyCollection(_serverVariables);
            copy._queryString = CopyCollection(_queryString);
            copy._form = CopyCollection(_form);
            copy._cookies = CopyCollection(_cookies);
            copy._data = CopyCollection(_data);

            return copy;
        }

        private static IDictionary CopyCollection(IDictionary collection)
        {
            if (collection == null || collection.Count == 0)
                return null;

            return new Hashtable(collection);
        }

        private static NameValueCollection CopyCollection(NameValueCollection collection)
        {
            if (collection == null || collection.Count == 0)
                return null;

            return new NameValueCollection(collection);
        }

        private static NameValueCollection CopyCollection(HttpCookieCollection cookies)
        {
            if (cookies == null || cookies.Count == 0)
                return null;

            NameValueCollection copy = new NameValueCollection(cookies.Count);

            for (int i = 0; i < cookies.Count; i++)
            {
                HttpCookie cookie = cookies[i];

                //
                // NOTE: We drop the Path and Domain properties of the 
                // cookie for sake of simplicity.
                //

                copy.Add(cookie.Name, cookie.Value);
            }

            return copy;
        }

        private static NameValueCollection FaultIn(ref NameValueCollection collection)
        {
            if (collection == null)
                collection = new NameValueCollection();

            return collection;
        }
    }
}
