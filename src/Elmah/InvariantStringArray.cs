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

    #endregion

    /// <summary> 
    /// Helper methods for array containing culturally-invariant strings.
    /// The main reason for this helper is to help with po
    /// </summary>
 
    internal sealed class InvariantStringArray
    {
        public static void Sort(string[] keys)
        {
            Array.Sort(keys);
        }

        public static void Sort(string[] keys, Array items)
        {
            Debug.Assert(keys != null);

            Array.Sort(keys, items, InvariantComparer);
        }

        private static IComparer InvariantComparer
        {
            get
            {
#if NET_1_0
                return StringComparer.DefaultInvariant;
#else
                return Comparer.DefaultInvariant;
#endif
            }
        }

#if NET_1_0
        
        [ Serializable ]
        private sealed class StringComparer : IComparer
        {
            private CompareInfo _compareInfo;
            
            public static readonly StringComparer DefaultInvariant = new StringComparer(CultureInfo.InvariantCulture);

            private StringComparer(CultureInfo culture)
            {
                Debug.Assert(culture != null);
                
                _compareInfo = culture.CompareInfo;
            }

            public int Compare(object x, object y)
            {
                if (x == y) 
                    return 0;
                else if (x == null) 
                    return -1;
                else if (y == null) 
                    return 1;
                else
                    return _compareInfo.Compare((string) x, (string) y);
            }
        }

#endif
        
        private InvariantStringArray() {}
    }
}