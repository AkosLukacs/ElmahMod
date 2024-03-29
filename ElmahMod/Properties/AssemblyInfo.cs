﻿#region License, Terms and Author(s)
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

//
//Modification by Akos Lukacs, https://github.com/AkosLukacs/ElmahMod
//
#endregion

#region Imports

using System.Reflection;

using CLSCompliantAttribute = System.CLSCompliantAttribute;
using ComVisible = System.Runtime.InteropServices.ComVisibleAttribute;

#endregion

[assembly: AssemblyTitle("ELMAHMod")]
[assembly: AssemblyDescription("Error Logging Modules and Handlers (ELMAH) for ASP.NET")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("ELMAHMod")]
[assembly: AssemblyCopyright("Copyright (c) 2004-9, Atif Aziz. All rights reserved.")]
[assembly: AssemblyCulture("")]

[assembly: AssemblyVersion("0.1.*")]
[assembly: AssemblyFileVersion("0.1.0.0")]
[assembly: AssemblyConfiguration(Elmah.Build.Configuration)]

[assembly: CLSCompliant(true)]
[assembly: ComVisible(false)]

[assembly: Elmah.Scc("$Id$")]
