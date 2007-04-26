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

namespace Elmah.ContentSyndication 
{
    #region Imports

    using System.Xml.Serialization;

    #endregion

    //
    // See RSS 0.91 specification at http://backend.userland.com/rss091 for
    // explanation of the XML vocabulary represented by the classes in this
    // file.
    //

    [ XmlRoot("rss", Namespace = "", IsNullable = false) ]
    public class RichSiteSummary 
    {
        public Channel channel;
        [ XmlAttribute ]
        public string version;
    }
    
    public class Channel 
    {
        public string title;
        [ XmlElement(DataType = "anyURI") ]
        public string link;
        public string description;
        [ XmlElement(DataType = "language") ]
        public string language;
        public string rating;
        public Image image;
        public TextInput textInput;
        public string copyright;
        [ XmlElement(DataType = "anyURI") ]
        public string docs;
        public string managingEditor;
        public string webMaster;
        public string pubDate;
        public string lastBuildDate;
        [ XmlArrayItem("hour", IsNullable = false) ]
        public int[] skipHours;
        [ XmlArrayItem("day", IsNullable = false) ]
        public Day[] skipDays;
        [ XmlElement("item") ]
        public Item[] item;
    }
    
    public class Image 
    {
        public string title;
        [ XmlElement(DataType = "anyURI") ]
        public string url;
        [ XmlElement(DataType = "anyURI") ]
        public string link;
        public int width;
        [ XmlIgnore() ]
        public bool widthSpecified;
        public int height;
        [ XmlIgnore() ]
        public bool heightSpecified;
        public string description;
    }
    
    public class Item 
    {
        public string title;
        public string description;
        public string pubDate;
        [ XmlElement(DataType = "anyURI") ]
        public string link;
    }
    
    public class TextInput 
    {
        public string title;
        public string description;
        public string name;
        [ XmlElement(DataType = "anyURI") ]
        public string link;
    }

    public enum Day 
    {
        Monday,
        Tuesday,
        Wednesday,
        Thursday,
        Friday,
        Saturday,
        Sunday,
    }
}
