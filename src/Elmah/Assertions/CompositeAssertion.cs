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

namespace Elmah.Assertions
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    #endregion

    /// <summary>
    /// Read-only collection of <see cref="Assertions.IAssertion"/> instances.
    /// </summary>

    [ Serializable ]
    public abstract class CompositeAssertion : ReadOnlyCollection<IAssertion>, IAssertion
    {
        protected CompositeAssertion() : 
            this(Enumerable.Empty<IAssertion>()) {}

        protected CompositeAssertion(IEnumerable<IAssertion> assertions) : 
            base(Validate(assertions).ToArray()) {}

        private static IEnumerable<IAssertion> Validate(IEnumerable<IAssertion> assertions)
        {
            if (assertions == null) throw new ArgumentNullException("assertions");
            return ValidateImpl(assertions);
        }

        private static IEnumerable<IAssertion> ValidateImpl(IEnumerable<IAssertion> assertions)
        {
            foreach (var assertion in assertions)
            {
                if (assertion == null)
                    throw new ArgumentException(null, "assertions");
                yield return assertion;
            }
        }

        public abstract bool Test(object context);
    }
}