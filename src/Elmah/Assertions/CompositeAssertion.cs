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

namespace Elmah.Assertions
{
    #region Imports

    using System;
    using System.Collections;

    #endregion

    /// <summary>
    /// Read-only collection of <see cref="Assertions.IAssertion"/> instances.
    /// </summary>

    [ Serializable ]
    public abstract class CompositeAssertion : ReadOnlyCollectionBase, IAssertion
    {
        protected CompositeAssertion() {}

        protected CompositeAssertion(IAssertion[] assertions)
        {
            if (assertions == null) 
                throw new ArgumentNullException("assertions");

            foreach (IAssertion assertion in assertions)
            {
                if (assertion == null)
                    throw new ArgumentException(null, "assertions");
            }

            InnerList.AddRange(assertions);
        }

        protected CompositeAssertion(ICollection assertions)
        {
            if (assertions != null)
                InnerList.AddRange(assertions);
        }

        public virtual IAssertion this[int index]
        {
            get { return (IAssertion) InnerList[index]; }
        }

        public virtual bool Contains(IAssertion assertion)
        {
            return InnerList.Contains(assertion);
        }

        public virtual int IndexOf(IAssertion assertion)
        {
            return InnerList.IndexOf(assertion);
        }
        
        public abstract bool Test(object context);
    }
}