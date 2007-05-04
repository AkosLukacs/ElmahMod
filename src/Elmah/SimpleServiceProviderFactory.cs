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

    using IDictionary = System.Collections.IDictionary;
    using ConfigurationSettings = System.Configuration.ConfigurationSettings;

	#endregion

    /// <summary>
    /// A simple factory for creating instances of types specified in a 
    /// section of the configuration file.
    /// </summary>
	
    internal sealed class SimpleServiceProviderFactory
	{
        public static object CreateFromConfigSection(string sectionName)
        {
            Debug.AssertStringNotEmpty(sectionName);

            //
            // Get the configuration section with the settings.
            //
            
            IDictionary config = (IDictionary) ConfigurationSettings.GetConfig(sectionName);

            if (config == null)
            {
                return null;
            }

            //
            // We modify the settings by removing items as we consume 
            // them so make a copy here.
            //

            config = (IDictionary) ((ICloneable) config).Clone();

            //
            // Get the type specification of the service provider.
            //

            string typeSpec = Mask.NullString((string) config["type"]);
            
            if (typeSpec.Length == 0)
            {
                return null;
            }

            config.Remove("type");

            //
            // Locate, create and return the service provider object.
            //

            Type type = Type.GetType(typeSpec, true);
            return Activator.CreateInstance(type, new object[] { config });
        }

        private SimpleServiceProviderFactory() {}
	}
}
