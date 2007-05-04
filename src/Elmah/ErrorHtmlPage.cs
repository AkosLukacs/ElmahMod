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
    using System.Web.UI;

	#endregion

    /// <summary>
    /// Renders an HTML page displaying the detailed host-generated (ASP.NET)
    /// HTML recorded for an error from the error log.
    /// </summary>
	
    internal sealed class ErrorHtmlPage : ErrorPageBase
	{
        protected override void Render(HtmlTextWriter writer)
        {
            if (writer == null)
                throw new ArgumentNullException("writer");

            //
            // Retrieve the ID of the error to display and read it from 
            // the log.
            //

            string errorId = Mask.NullString(this.Request.QueryString["id"]);

            if (errorId.Length == 0)
                return;

            ErrorLogEntry errorEntry = this.ErrorLog.GetError(errorId);

            if (errorEntry == null)
                return;

            //
            // If we have a host (ASP.NET) formatted HTML message 
            // for the error then just stream it out as our response.
            //

            if (errorEntry.Error.WebHostHtmlMessage.Length != 0)
            {
                writer.Write(errorEntry.Error.WebHostHtmlMessage);
            }
        }
	}
}
