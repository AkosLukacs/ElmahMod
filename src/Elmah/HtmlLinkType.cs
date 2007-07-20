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
    /// <summary>
    /// User agents, search engines, etc. may interpret and use these link 
    /// types in a variety of ways. For example, user agents may provide 
    /// access to linked documents through a navigation bar.
    /// </summary>
    /// <remarks>
    /// See <a href="http://www.w3.org/TR/html401/types.html#type-links">6.12 Link types</a>
    /// for more information.
    /// </remarks>
    
    internal sealed class HtmlLinkType
    {
        // Designates  substitute  versions  for the document in which the
        // link occurs. When used together with the lang attribute, it
        // implies  a  translated  version  of  the  document.  When  used
        // together  with  the  media  attribute, it implies a version
        // designed for a different medium (or media).

        public const string Alternate = "alternate";

        // Refers   to  an  external  style  sheet.  See  the  section  on
        // external  style  sheets  for details. This is used together
        // with  the  link  type "Alternate" for user-selectable alternate
        // style sheets.

        public const string Stylesheet = "stylesheet";

        // Refers to the first document in a collection of documents. This
        // link  type tells search engines which document is considered by
        // the author to be the starting point of the collection.

        public const string Start = "start";

        // Refers  to the next document in a linear sequence of documents.
        // User  agents  may  choose  to  preload  the "next" document, to
        // reduce the perceived load time.

        public const string Next = "next";

        // Refers  to  the  previous  document  in  an  ordered  series of
        // documents.   Some   user   agents   also  support  the  synonym
        // "Previous".

        public const string Prev = "prev";

        // Refers  to a document serving as a table of contents. Some user
        // agents also support the synonym ToC (from "Table of Contents").

        public const string Contents = "contents";

        // Refers  to  a  document  providing  an  index  for  the current
        // document.

        public const string Index = "index";

        // Refers to a document providing a glossary of terms that pertain
        // to the current document.

        public const string Glossary = "glossary";

        // Refers to a copyright statement for the current document.

        public const string Copyright = "copyright";

        // Refers  to  a  document serving as a chapter in a collection of
        // documents.

        public const string Chapter = "chapter";

        // Refers  to  a  document serving as a section in a collection of
        // documents.

        public const string Section = "section";

        // Refers to a document serving as a subsection in a collection of
        // documents.

        public const string Subsection = "subsection";

        // Refers  to a document serving as an appendix in a collection of
        // documents.

        public const string Appendix = "appendix";

        // Refers  to a document offering help (more information, links to
        //  other sources information, etc.)

        public const string Help = "help";

        // Refers to a bookmark. A bookmark is a link to a key entry point
        // within  an  extended  document.  The title attribute may be
        // used,  for  example,  to  label the bookmark. Note that several
        // bookmarks may be defined in each document.

        public const string Bookmark = "bookmark";

        private HtmlLinkType() {}
    }
}
