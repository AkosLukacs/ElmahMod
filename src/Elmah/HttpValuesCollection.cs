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

namespace Elmah
{
	#region Imports

	using System;

    using NameValueCollection = System.Collections.Specialized.NameValueCollection;
    using XmlReader = System.Xml.XmlReader;
    using XmlWriter = System.Xml.XmlWriter;
    using SerializationInfo = System.Runtime.Serialization.SerializationInfo;
    using StreamingContext = System.Runtime.Serialization.StreamingContext;

	#endregion

    /// <summary>
    /// A name-values collection implementation suitable for web-based collections 
    /// (like server variables, query strings, forms and cookies) that can also
    /// be written and read as XML.
    /// </summary>
    
    [ Serializable ]
    internal sealed class HttpValuesCollection : NameValueCollection, IXmlExportable
    {
        public HttpValuesCollection() {}        

        public HttpValuesCollection(NameValueCollection other) : 
            base(other) {}
                
        public HttpValuesCollection(int capacity) : 
            base(capacity) {}
        
        public HttpValuesCollection(int capacity, NameValueCollection other) : 
            base(capacity, other) {}
                
        private HttpValuesCollection(SerializationInfo info, StreamingContext context) : 
            base(info, context) {}

        void IXmlExportable.FromXml(XmlReader reader)
        {
            if (reader == null)
                throw new ArgumentNullException("reader");

            if (this.IsReadOnly)
                throw new InvalidOperationException("Object is read-only.");

            reader.Read();

            //
            // Add entries into the collection as <item> elements
            // with child <value> elements are found.
            //

            while (reader.IsStartElement("item"))
            {
                string name = reader.GetAttribute("name");
                bool isNull = reader.IsEmptyElement; 

                reader.Read(); // <item>

                if (!isNull)
                {

                    while (reader.IsStartElement("value")) // <value ...>
                    {
                        string value = reader.GetAttribute("string");
                        Add(name, value);
                        reader.Read();
                    }

                    reader.ReadEndElement(); // </item>
                }
                else
                {
                    Add(name, null);
                }
            }

            reader.ReadEndElement();
        }

        void IXmlExportable.ToXml(XmlWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException("writer");

            if (this.Count == 0)
            {
                return;
            }

            //
            // Write out a named multi-value collection as follows 
            // (example here is the ServerVariables collection):
            //
            //      <item name="HTTP_URL">
            //          <value string="/myapp/somewhere/page.aspx" />
            //      </item>
            //      <item name="QUERY_STRING">
            //          <value string="a=1&amp;b=2" />
            //      </item>
            //      ...
            //

            foreach (string key in this.Keys)
            {
                writer.WriteStartElement("item");
                writer.WriteAttributeString("name", key);
                
                string[] values = GetValues(key);

                if (values != null)
                {
                    foreach (string value in values)
                    {
                        writer.WriteStartElement("value");
                        writer.WriteAttributeString("string", value);
                        writer.WriteEndElement();
                    }
                }
                
                writer.WriteEndElement();
            }
        }
    }
}
