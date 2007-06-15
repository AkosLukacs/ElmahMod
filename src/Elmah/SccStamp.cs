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
    using System.Reflection;
    using System.Text.RegularExpressions;

    #endregion

    [ Serializable ]
    public sealed class SccStamp
    {
        private readonly string _id;
        private readonly string _author;
        private readonly string _fileName;
        private readonly int _revision;
        private readonly DateTime _lastChanged;
        private static readonly Regex _regex;

        static SccStamp()
        {
            _regex = new Regex(
                @"^\$id:\s*(?<f>[^\s]+)\s+(?<r>[0-9]+)\s+((?<y>[0-9]{4})-(?<mo>[0-9]{2})-(?<d>[0-9]{2}))\s+((?<h>[0-9]{2})\:(?<mi>[0-9]{2})\:(?<s>[0-9]{2})Z)\s+(?<a>\w+)",
                RegexOptions.CultureInvariant | RegexOptions.IgnoreCase | RegexOptions.IgnorePatternWhitespace | RegexOptions.Singleline | RegexOptions.ExplicitCapture | RegexOptions.Compiled);
        }

        public SccStamp(string id)
        {
            if (id == null)
                throw new ArgumentNullException("id");

            if (id.Length == 0)
                throw new ArgumentException(null, "id");
            
            Match match = _regex.Match(id);
            
            if (!match.Success)
                throw new ArgumentException(null, "id");

            _id = id;

            GroupCollection groups = match.Groups;

            _fileName = groups["f"].Value;
            _revision = int.Parse(groups["r"].Value);
            _author = groups["a"].Value;

            int year = int.Parse(groups["y"].Value);
            int month = int.Parse(groups["mo"].Value);
            int day = int.Parse(groups["d"].Value);
            int hour = int.Parse(groups["h"].Value);
            int minute = int.Parse(groups["mi"].Value);
            int second = int.Parse(groups["s"].Value);
            
            _lastChanged = new DateTime(year, month, day, hour, minute, second);
        }

        public string Id
        {
            get { return _id; }
        }

        public string Author
        {
            get { return _author; }
        }

        public string FileName
        {
            get { return _fileName; }
        }

        public int Revision
        {
            get { return _revision; }
        }

        public DateTime LastChanged
        {
            get { return _lastChanged; }
        }

        public override string ToString()
        {
            return Id;
        }

        public static SccStamp[] FindAll(Assembly assembly)
        {
            if (assembly == null)
                throw new ArgumentNullException("assembly");

            SccAttribute[] attributes = (SccAttribute[]) Attribute.GetCustomAttributes(assembly, typeof(SccAttribute), false);
            
            if (attributes.Length == 0)
                return new SccStamp[0];
            
            ArrayList list = new ArrayList(attributes.Length);

            foreach (SccAttribute attribute in attributes)
            {
                string id = attribute.Id.Trim();

                if (id.Length > 0 && string.Compare("$Id" + /* IMPORTANT! */ "$", id, true, CultureInfo.InvariantCulture) != 0)
                    list.Add(new SccStamp(id));
            }

            return (SccStamp[]) list.ToArray(typeof(SccStamp));
        }

        public static SccStamp FindLatest(Assembly assembly)
        {
            return FindLatest(FindAll(assembly));
        }

        public static SccStamp FindLatest(SccStamp[] stamps)
        {
            if (stamps == null)
                throw new ArgumentNullException("stamps");
            
            if (stamps.Length == 0)
                return null;
            
            stamps = (SccStamp[]) stamps.Clone();
            SortByRevision(stamps, /* descending */ true);
            return stamps[0];
        }

        public static void SortByRevision(SccStamp[] stamps)
        {
            SortByRevision(stamps, false);
        }

        public static void SortByRevision(SccStamp[] stamps, bool descending)
        {
            IComparer comparer = new RevisionComparer();
            
            if (descending)
                comparer = new ReverseComparer(comparer);
            
            Array.Sort(stamps, comparer);
        }

        private sealed class RevisionComparer : IComparer
        {
            public int Compare(object x, object y)
            {
                if (x == null && y == null)
                    return 0;
                
                if (x == null)
                    return -1;
                
                if (y == null)
                    return 1;

                if (x.GetType() != y.GetType())
                    throw new ArgumentException("Objects cannot be compared because their types do not match.");
                
                return Compare((SccStamp) x, (SccStamp) y);
            }

            private int Compare(SccStamp lhs, SccStamp rhs)
            {
                Debug.Assert(lhs != null);
                Debug.Assert(rhs != null);
                
                return lhs.Revision.CompareTo(rhs.Revision);
            }
        }
    }
}
