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
    using System.IO;
    using System.Xml;

    #endregion

    /// <summary>
    /// Represents a writer that provides a fast, non-cached, forward-only 
    /// way of generating streams or files containing JSON Text according
    /// to the grammar rules laid out in 
    /// <a href="http://www.ietf.org/rfc/rfc4627.txt">RFC 4627</a>.
    /// </summary>
    /// <remarks>
    /// This class supports ELMAH and is not intended to be used directly 
    /// from your code. It may be modified or removed in the future without 
    /// notice. It has public accessibility for testing purposes. If you
    /// need a general-purpose JSON Text encoder, consult
    /// <a href="http://www.json.org/">JSON.org</a> for implementations
    /// or use classes available from the Microsoft .NET Framework.
    /// </remarks>

    public sealed class JsonTextWriter
    {
        private readonly TextWriter _writer;
        private readonly int[] _counters;
        private readonly char[] _terminators;
        private int _depth;
        private string _memberName;

        public JsonTextWriter(TextWriter writer)
        {
            Debug.Assert(writer != null);
            _writer = writer;
            const int levels = 10 + /* root */ 1;
            _counters = new int[levels];
            _terminators = new char[levels];
        }

        public int Depth
        {
            get { return _depth; }
        }

        private int ItemCount
        {
            get { return _counters[Depth]; }
            set { _counters[Depth] = value; }
        }

        private char Terminator
        {
            get { return _terminators[Depth]; }
            set { _terminators[Depth] = value; }
        }

        public JsonTextWriter Object()
        {
            return StartStructured("{", "}");
        }

        public JsonTextWriter EndObject()
        {
            return Pop();
        }

        public JsonTextWriter Array()
        {
            return StartStructured("[", "]");
        }

        public JsonTextWriter EndArray()
        {
            return Pop();
        }

        public JsonTextWriter Pop()
        {
            return EndStructured();
        }

        public JsonTextWriter Member(string name)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (name.Length == 0) throw new ArgumentException(null, "name");
            if (_memberName != null) throw new InvalidOperationException("Missing member value.");
            _memberName = name;
            return this;
        }

        private JsonTextWriter Write(string text)
        {
            return WriteImpl(text, /* raw */ false);
        }

        private JsonTextWriter WriteEnquoted(string text)
        {
            return WriteImpl(text, /* raw */ true);
        }

        private JsonTextWriter WriteImpl(string text, bool raw)
        {
            Debug.Assert(raw || (text != null && text.Length > 0));

            if (Depth == 0 && (text.Length > 1 || (text[0] != '{' && text[0] != '[')))
                throw new InvalidOperationException();

            TextWriter writer = _writer;

            if (ItemCount > 0)
                writer.Write(',');   

            string name = _memberName;
            _memberName = null;

            if (name != null)
            {
                writer.Write(' ');
                Enquote(name, writer);
                writer.Write(':');
            }

            if (Depth > 0) 
                writer.Write(' ');
            
            if (raw) 
                Enquote(text, writer); 
            else 
                writer.Write(text);
            
            ItemCount = ItemCount + 1;

            return this;
        }

        public JsonTextWriter Number(int value)
        {
            return Write(value.ToString(CultureInfo.InvariantCulture));
        }

        public JsonTextWriter String(string str)
        {
            return str == null ? Null() : WriteEnquoted(str);
        }

        public JsonTextWriter Null()
        {
            return Write("null");
        }

        public JsonTextWriter Boolean(bool value)
        {
            return Write(value ? "true" : "false");
        }

        private static readonly DateTime _epoch =  /* ... */
#if NET_1_0 || NET_1_1
            /* ... */ new DateTime(1970, 1, 1, 0, 0, 0);
#else
            /* ... */ new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
#endif

        public JsonTextWriter Number(DateTime time)
        {
            double seconds = time.ToUniversalTime().Subtract(_epoch).TotalSeconds;
            return Write(seconds.ToString(CultureInfo.InvariantCulture));
        }

        public JsonTextWriter String(DateTime time)
        {
            string xmlTime;
#if NET_1_0 || NET_1_1
            xmlTime = XmlConvert.ToString(time);
#else
            xmlTime = XmlConvert.ToString(time, XmlDateTimeSerializationMode.Utc);
#endif
            return String(xmlTime);
        }

        private JsonTextWriter StartStructured(string start, string end)
        {
            if (Depth + 1 == _counters.Length)
                throw new Exception();

            Write(start);
            _depth++;
            Terminator = end[0];
            return this;
        }

        private JsonTextWriter EndStructured()
        {
            if (Depth - 1 < 0)
                throw new Exception();

            _writer.Write(' ');
            _writer.Write(Terminator);
            ItemCount = 0;
            _depth--;
            return this;
        }

        private static void Enquote(string s, TextWriter writer)
        {
            Debug.Assert(writer != null);

            int length = Mask.NullString(s).Length;

            writer.Write('"');

            char last;
            char ch = '\0';

            for (int index = 0; index < length; index++)
            {
                last = ch;
                ch = s[index];

                switch (ch)
                {
                    case '\\':
                    case '"':
                    {
                        writer.Write('\\');
                        writer.Write(ch);
                        break;
                    }

                    case '/':
                    {
                        if (last == '<')
                            writer.Write('\\');
                        writer.Write(ch);
                        break;
                    }

                    case '\b': writer.Write("\\b"); break;
                    case '\t': writer.Write("\\t"); break;
                    case '\n': writer.Write("\\n"); break;
                    case '\f': writer.Write("\\f"); break;
                    case '\r': writer.Write("\\r"); break;

                    default:
                    {
                        if (ch < ' ')
                        {
                            writer.Write("\\u");
                            writer.Write(((int)ch).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            writer.Write(ch);
                        }

                        break;
                    }
                }
            }

            writer.Write('"');
        }
    }
}