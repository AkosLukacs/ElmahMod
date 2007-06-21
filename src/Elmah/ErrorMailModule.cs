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
    using System.Globalization;
    using System.Web;
    using System.IO;

#if NET_1_0 || NET_1_1
    using System.Web.Mail;
#else
    using System.Net.Mail;
#endif

    using ConfigurationSettings = System.Configuration.ConfigurationSettings;
    using IDictionary = System.Collections.IDictionary;
    using ThreadPool = System.Threading.ThreadPool;
    using WaitCallback = System.Threading.WaitCallback;
    using Encoding = System.Text.Encoding;
    using NetworkCredential = System.Net.NetworkCredential;

    #endregion

    /// <summary>
    /// HTTP module that sends an e-mail whenever an unhandled exception
    /// occurs in an ASP.NET web application.
    /// </summary>

    public class ErrorMailModule : IHttpModule, IExceptionFiltering
    {
        private string _smtpServer;
        private int _smtpPort;
        private string _authUserName;
        private string _authPassword;
        private string _mailSender;
        private string _mailRecipient;
        private string _mailSubjectFormat;
        private bool _reportAsynchronously;

        public event ExceptionFilterEventHandler Filtering;

        /// <summary>
        /// Initializes the module and prepares it to handle requests.
        /// </summary>

        public virtual void Init(HttpApplication application)
        {
            if (application == null)
                throw new ArgumentNullException("application");
            
            HttpModuleRegistry.RegisterInPartialTrust(application, this);

            //
            // Get the configuration section of this module.
            // If it's not there then there is nothing to initialize or do.
            // In this case, the module is as good as mute.
            //

            IDictionary config = (IDictionary) GetConfig();

            if (config == null)
                return;

            //
            // Extract the settings.
            //

            string mailRecipient = GetSetting(config, "to");
            string mailSender = GetSetting(config, "from", mailRecipient);
            string mailSubjectFormat = GetSetting(config, "subject", string.Empty);
            bool reportAsynchronously = Convert.ToBoolean(GetSetting(config, "async", bool.TrueString));
            string smtpServer = GetSetting(config, "smtpServer", string.Empty);
            int smtpPort = Convert.ToUInt16(GetSetting(config, "smtpPort", "25"), CultureInfo.InvariantCulture);
            string authUserName = GetSetting(config, "userName", string.Empty);
            string authPassword = GetSetting(config, "password", string.Empty);

            //
            // Hook into the Error event of the application.
            //

            application.Error += new EventHandler(OnError);
            
            //
            // Finally, commit the state of the module if we got this far.
            // Anything beyond this point should not cause an exception.
            //

            _mailRecipient = mailRecipient;
            _mailSender = mailSender;
            _mailSubjectFormat = mailSubjectFormat;
            _reportAsynchronously = reportAsynchronously;
            _smtpServer = smtpServer;
            _smtpPort = smtpPort;
            _authUserName = authUserName;
            _authPassword = authPassword;
        }

        /// <summary>
        /// Disposes of the resources (other than memory) used by the module.
        /// </summary>

        public virtual void Dispose()
        {
        }

        /// <summary>
        /// Gets the e-mail address of the sender.
        /// </summary>
        
        protected virtual string MailSender
        {
            get { return _mailSender; }
        }
        
        /// <summary>
        /// Gets the e-mail address of the recipient, or a semicolon-delimited 
        /// list of e-mail addresses in case of multiple recipients.
        /// </summary>

        protected virtual string MailRecipient
        {
            get { return _mailRecipient; }
        }

        /// <summary>
        /// Gets the text used to format the e-mail subject.
        /// </summary>
        /// <remarks>
        /// The subject text specification may include {0} where the
        /// error message (<see cref="Error.Message"/>) should be inserted 
        /// and {1} <see cref="Error.Type"/> where the error type should 
        /// be insert.
        /// </remarks>

        protected virtual string MailSubjectFormat
        {
            get { return _mailSubjectFormat; }
        }
        
        /// <summary>
        /// Gets the SMTP server host name used when sending the mail.
        /// </summary>

        protected string SmtpServer
        {
            get { return _smtpServer; }
        }

        /// <summary>
        /// Gets the SMTP port used when sending the mail.
        /// </summary>

        protected int SmtpPort
        {
            get { return _smtpPort; }
        }

        /// <summary>
        /// Gets the user name to use if the SMTP server requires authentication.
        /// </summary>

        protected string AuthUserName
        {
            get { return _authUserName; }
        }

        /// <summary>
        /// Gets the clear-text password to use if the SMTP server requires 
        /// authentication.
        /// </summary>

        protected string AuthPassword
        {
            get { return _authPassword; }
        }

        /// <summary>
        /// The handler called when an unhandled exception bubbles up to 
        /// the module.
        /// </summary>

        protected virtual void OnError(object sender, EventArgs e)
        {
            HttpContext context = ((HttpApplication) sender).Context;
            
            //
            // Fire an event to check if listeners want to filter out
            // reporting of the uncaught exception.
            //

            Exception ex = context.Server.GetLastError();
            ExceptionFilterEventArgs filterArgs = new ExceptionFilterEventArgs(ex, context);
            if (filterArgs.Dismissed)
                return;
            
            //
            // Get the last error and then report it synchronously or 
            // asynchronously based on the configuration.
            //

            Error error = GetLastError(context);

            if (_reportAsynchronously)
                ReportErrorAsync(error);
            else
                ReportError(error);
        }
        
        /// <summary>
        /// Raises the <see cref="Filtering"/> event.
        /// </summary>

        protected virtual void OnFiltering(ExceptionFilterEventArgs args)
        {
            ExceptionFilterEventHandler handler = Filtering;
            
            if (handler != null)
                handler(this, args);
        }

        /// <summary>
        /// Schedules the error to be e-mailed asynchronously.
        /// </summary>
        /// <remarks>
        /// The default implementation uses the <see cref="ThreadPool"/>
        /// to queue the reporting.
        /// </remarks>

        protected virtual void ReportErrorAsync(Error error)
        {
            if (error == null)
                throw new ArgumentNullException("error");

            //
            // Schedule the reporting at a later time using a worker from 
            // the system thread pool. This makes the implementation
            // simpler, but it might have an impact on reducing the
            // number of workers available for processing ASP.NET
            // requests in the case where lots of errors being generated.
            //

            ThreadPool.QueueUserWorkItem(new WaitCallback(ReportError), error);
        }

        private void ReportError(object state)
        {
            ReportError((Error) state);
        }

        /// <summary>
        /// Schedules the error to be e-mailed synchronously.
        /// </summary>

        protected virtual void ReportError(Error error)
        {
            if (error == null)
                throw new ArgumentNullException("error");

            //
            // Start by checking if we have a sender and a recipient.
            // These values may be null if someone overrides the
            // implementation of Init but does not override the
            // MailSender and MailRecipient properties.
            //

            string sender = Mask.NullString(this.MailSender);
            string recipient = Mask.NullString(this.MailRecipient);

            // TODO: Under 2.0, the sender can be defaulted via <network> configuration so consider only checking recipient here.

            if (sender.Length == 0 || recipient.Length == 0)
            {
                return;
            }

            //
            // Create the mail, setting up the sender and recipient.
            //

            MailMessage mail = new MailMessage();

#if NET_1_0 || NET_1_1
            mail.From = sender;
            mail.To = recipient;
#else
            mail.From = new MailAddress(sender);
            mail.To.Add(recipient);
#endif

            //
            // Format the mail subject.
            // 

            string subjectFormat = Mask.NullString(this.MailSubjectFormat);
        
            if (subjectFormat.Length == 0)
            {
                subjectFormat = "Error ({1}): {0}";
            }

            mail.Subject = string.Format(subjectFormat, error.Message, error.Type);

            //
            // Format the mail body.
            //

            ErrorTextFormatter formatter = CreateErrorFormatter();

            StringWriter bodyWriter = new StringWriter();
            formatter.Format(bodyWriter, error);
            mail.Body = bodyWriter.ToString();

            switch (formatter.MimeType)
            {
#if NET_1_0 || NET_1_1
                case "text/html" : mail.BodyFormat = MailFormat.Html; break;
                case "text/plain" : mail.BodyFormat = MailFormat.Text; break;
#else
                case "text/html": mail.IsBodyHtml = true; break;
                case "text/plain": mail.IsBodyHtml = false; break;
#endif
                default :
                {
                    throw new ApplicationException(string.Format(
                        "The error mail module does not know how to handle the {1} media type that is created by the {0} formatter.",
                        formatter.GetType().FullName, formatter.MimeType));
                }
            }

#if NET_1_1
            //
            // If the mail needs to be delivered to a particular SMTP server
            // then set-up the corresponding CDO configuration fields of the 
            // mail message.
            //
            
            string smtpServer = Mask.NullString(this.SmtpServer);

            if (smtpServer.Length > 0)
            {
                IDictionary fields = mail.Fields;

                fields.Add(CdoConfigurationFields.SendUsing, /* cdoSendUsingPort */ 2);
                fields.Add(CdoConfigurationFields.SmtpServer, smtpServer);
                fields.Add(CdoConfigurationFields.SmtpServerPort, this.SmtpPort);

                //
                // If the SMTP server requires authentication (indicated by 
                // non-blank user name and password settings) then set-up 
                // the corresponding CDO configuration fields of the mail 
                // message.
                //
            
                string userName = Mask.NullString(this.AuthUserName);
                string password = Mask.NullString(this.AuthPassword);

                if (userName.Length > 0 && password.Length > 0)
                {
                    fields.Add(CdoConfigurationFields.SmtpAuthenticate, 1);
                    fields.Add(CdoConfigurationFields.SendUserName, userName);
                    fields.Add(CdoConfigurationFields.SendPassword, password);
                }
            }
#endif

            //
            // Provide one last hook to pre-process the mail and then send 
            // it off.
            //

            try
            {
                PreSendMail(mail, error);
                SendMail(mail);
            }
            finally
            {
                DisposeMail(mail);
            }
        }

        /// <summary>
        /// Provides a final point (mainly for inheritors) to customize the 
        /// e-mail message before it is sent.
        /// </summary>

        protected virtual void PreSendMail(MailMessage mail, Error error)
        {
            if (mail == null)
                throw new ArgumentNullException("mail");

            if (error == null)
                throw new ArgumentNullException("error");

            //
            // If a HTML message supplied by the web host then attach it to
            // the mail.
            //

            if (error.WebHostHtmlMessage.Length != 0)
            {
#if NET_1_0 || NET_1_1
                //
                // Create a temporary file to hold the attachment. Note that 
                // the temporary file is created in the location returned by
                // System.Web.HttpRuntime.CodegenDir. It is assumed that
                // this code will have sufficient rights to create the
                // temporary file in that area.
                //

                string fileName = "YSOD-" + Guid.NewGuid().ToString() + ".html";
                string path = Path.Combine(HttpRuntime.CodegenDir, fileName);

                try
                {
                    using (StreamWriter attachementWriter = File.CreateText(path))
                        attachementWriter.Write(error.WebHostHtmlMessage);

                    mail.Attachments.Add(new MailAttachment(path));
                }
                catch (IOException)
                {
                    //
                    // Ignore I/O errors as non-critical. It's not the
                    // end of the world if the attachment could not be
                    // created (though it would be nice). It is more
                    // important to get to deliver the error message!
                    //
                }
#else
                mail.Attachments.Add(Attachment.CreateAttachmentFromString(error.WebHostHtmlMessage, 
                    "YSOD.html", Encoding.UTF8, "text/html"));
#endif
            }
        }

        /// <summary>
        /// Disposes the e-mail message after sending it, like deleting
        /// any temporary files created for attachements.
        /// </summary>

        protected virtual void DisposeMail(MailMessage mail)
        {
#if NET_1_1
            foreach (MailAttachment attachment in mail.Attachments)
                File.Delete(attachment.Filename);
#endif
        }

        /// <summary>
        /// Creates the <see cref="ErrorTextFormatter"/> implementation to 
        /// be used to format the body of the e-mail.
        /// </summary>

        protected virtual ErrorTextFormatter CreateErrorFormatter()
        {
            return new ErrorMailHtmlFormatter();
        }

        /// <summary>
        /// Sends the e-mail using SmtpMail or SmtpClient.
        /// </summary>

        protected virtual void SendMail(MailMessage mail)
        {
            if (mail == null)
                throw new ArgumentNullException("mail");

#if NET_1_0 || NET_1_1
            SmtpMail.Send(mail);
#else
            //
            // Under .NET Framework 2.0, the authentication settings
            // go on the SmtpClient object rather than mail message
            // so these have to be set up here.
            //

            SmtpClient client = new SmtpClient();

            string host = SmtpServer ?? string.Empty;
            if (host.Length > 0)
                client.Host = host;

            int port = SmtpPort;
            if (port > 0)
                client.Port = port;

            string userName = AuthUserName ?? string.Empty;
            string password = AuthPassword ?? string.Empty;

            // TODO: Allow use of default credentials?

            if (userName.Length > 0 && password.Length > 0)
                client.Credentials = new NetworkCredential(userName, password);

            client.Send(mail);
#endif
        }

        /// <summary>
        /// Gets the configuration object used by <see cref="Init"/> to read
        /// the settings for module.
        /// </summary>

        protected virtual object GetConfig()
        {
            return ConfigurationSettings.GetConfig("elmah/errorMail");
        }

        /// <summary>
        /// Builds an <see cref="Error"/> object from the last context
        /// exception generated.
        /// </summary>

        protected virtual Error GetLastError(HttpContext context)
        {
            if (context == null)
                throw new ArgumentNullException("context");

            return new Error(context.Server.GetLastError(), context);
        }

        private static string GetSetting(IDictionary config, string name)
        {
            return GetSetting(config, name, null);
        }

        private static string GetSetting(IDictionary config, string name, string defaultValue)
        {
            Debug.Assert(config != null);
            Debug.AssertStringNotEmpty(name);

            string value = Mask.NullString((string) config[name]);

            if (value.Length == 0)
            {
                if (defaultValue == null)
                {
                    throw new ApplicationException(string.Format(
                        "The required configuration setting '{0}' is missing for the error mailing module.", name));
                }

                value = defaultValue;
            }

            return value;
        }
    }
}
