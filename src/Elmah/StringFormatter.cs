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
    using System.Globalization;
    using System.Text;
    using System.Web;

    #endregion

    public delegate string StringFormatTokenBindingHandler(string token, object[] args, IFormatProvider provider);

    /// <summary>
    /// Helper class for formatting templated strings with supplied replacements.
    /// </summary>

    public sealed class StringFormatter
    {
        public static readonly StringFormatTokenBindingHandler DefaultTokenBinder = new StringFormatTokenBindingHandler(BindFormatToken);

        /// <summary>
        /// Replaces each format item in a specified string with the text 
        /// equivalent of a corresponding object's value. 
        /// </summary>

        public static string Format(string format, params object[] args)
        {
            return Format(format, null, null, args);
        }

        public static string Format(string format, IFormatProvider provider, params object[] args)
        {
            return Format(format, provider, null, args);
        }

        public static string Format(string format, StringFormatTokenBindingHandler binder, params object[] args)
        {
            return Format(format, null, binder, args);
        }

        public static string Format(string format,
            IFormatProvider provider, StringFormatTokenBindingHandler binder, params object[] args)
        {
            return FormatImpl(format, provider, binder != null ? binder : DefaultTokenBinder, args);
        }

        private static string FormatImpl(string format,
            IFormatProvider provider, StringFormatTokenBindingHandler binder, params object[] args)
        {
            if (format == null)
                throw new ArgumentNullException("format");

            Debug.Assert(binder != null);

            //
            // Following is a slightly modified version of the parser from
            // Henri Weichers that was presented at:
            // http://haacked.com/archive/2009/01/14/named-formats-redux.aspx
            // See also the following comment about the modifications: 
            // http://haacked.com/archive/2009/01/14/named-formats-redux.aspx#70485
            //

            StringBuilder result = new StringBuilder(format.Length * 2);
            StringBuilder token = new StringBuilder();

            CharEnumerator e = format.GetEnumerator();
            while (e.MoveNext())
            {
                char ch = e.Current;
                if (ch == '{')
                {
                    while (true)
                    {
                        if (!e.MoveNext())
                            throw new FormatException();

                        ch = e.Current;
                        if (ch == '}')
                        {
                            if (token.Length == 0)
                                throw new FormatException();

                            result.Append(binder(token.ToString(), args, provider));
                            token.Length = 0;
                            break;
                        }
                        if (ch == '{')
                        {
                            result.Append(ch);
                            break;
                        }
                        token.Append(ch);
                    }
                }
                else if (ch == '}')
                {
                    if (!e.MoveNext() || e.Current != '}')
                        throw new FormatException();
                    result.Append('}');
                }
                else
                {
                    result.Append(ch);
                }
            }

            return result.ToString();
        }

        public static string BindFormatToken(string token, object[] args, IFormatProvider provider)
        {
            if (token == null)
                throw new ArgumentNullException("token");
            if (token.Length == 0)
                throw new ArgumentException("Format token cannot be an empty string.", "token");
            if (args == null)
                throw new ArgumentNullException("args");
            if (args.Length == 0)
                throw new ArgumentException("Missing replacement arguments.", "args");

            object source = args[0];
            int dotIndex = token.IndexOf('.');
            int sourceIndex;
            if (dotIndex > 0 && 0 <= (sourceIndex = TryParseUnsignedInteger(token.Substring(0, dotIndex))))
            {
                source = args[sourceIndex];
                token = token.Substring(dotIndex + 1);
            }

            string format = string.Empty;

            int colonIndex = token.IndexOf(':');
            if (colonIndex > 0)
            {
                format = "{0:" + token.Substring(colonIndex + 1) + "}";
                token = token.Substring(0, colonIndex);
            }

            if ((sourceIndex = TryParseUnsignedInteger(token)) >= 0)
            {
                source = args[sourceIndex];
                token = null;
            }

            object result;

            try
            {
                result = DataBinder.Eval(source, token);
                if (result == null)
                    result = string.Empty;
            }
            catch (HttpException e) // Map silly exception type from DataBinder.Eval
            {
                throw new FormatException(e.Message, e);
            }

            return Mask.NullString(format).Length > 0
                 ? string.Format(provider, format, result)
                 : result.ToString();
        }

        public static int TryParseUnsignedInteger(string str)
        {
#if !NET_1_1 && !NET_1_0
            int result;
            return int.TryParse(str, NumberStyles.None, CultureInfo.InvariantCulture, out result) 
                 ? result : -1;
#else
            return System.Text.RegularExpressions.Regex.IsMatch(str, "^[0-9]+$")
                 ? int.Parse(str, NumberStyles.None, CultureInfo.InvariantCulture)
                 : -1;
#endif
        }

        private StringFormatter() { }
    }
}