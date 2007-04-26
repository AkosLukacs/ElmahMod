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

#region Imports

using System.Reflection;

using CLSCompliantAttribute = System.CLSCompliantAttribute;
using ComVisible = System.Runtime.InteropServices.ComVisibleAttribute;

#endregion

[assembly: AssemblyTitle("ELMAH")]
[assembly: AssemblyDescription("Error Logging Modules and Handlers (ELMAH) for ASP.NET")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("ELMAH")]
[assembly: AssemblyCopyright("Copyright (c) 2007, Atif Aziz. All rights reserved.")]
[assembly: AssemblyCulture("")]

[assembly: AssemblyVersion("1.0.5527.0")]
[assembly: AssemblyFileVersion("1.0.5527.0")]

[assembly: AssemblyDelaySign(false)]
[assembly: AssemblyKeyFile("..\\..\\Key.snk")]

#if DEBUG
[assembly: AssemblyConfiguration("Debug")]
#else
[assembly: AssemblyConfiguration("Release")]
#endif

[assembly: CLSCompliant(true)] 
[assembly: ComVisible(false)]
