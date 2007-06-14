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
    using System.Configuration;
    using System.Xml;

    #endregion

    /// <summary>
    /// Read-only collection of <see cref="Assertions.IAssertion"/> instances.
    /// </summary>

    [ Serializable ]
    public abstract class CompositeAssertion : ReadOnlyCollectionBase, IAssertion
    {
        protected CompositeAssertion() {}

        protected CompositeAssertion(XmlElement config)
        {
            if (config == null)
                throw new ArgumentNullException("config");
   
            foreach (XmlNode child in config.ChildNodes)
            {
                XmlNodeType nodeType = child.NodeType;

                //
                // Allow elements only as children, but skip comments and 
                // whitespaces.
                //

                if (nodeType == XmlNodeType.Comment || nodeType == XmlNodeType.Whitespace)
                    continue;

                if (nodeType != XmlNodeType.Element)
                {
                    throw new ConfigurationException(
                        string.Format("Unexpected type of node ({0}) in configuration.", nodeType.ToString()), 
                        child);
                }
                
                //
                // Create and configure the assertion given the configuration 
                // element and then add it to this collection.
                //

                InnerList.Add(AssertionFactory.Create((XmlElement) child));
            }
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