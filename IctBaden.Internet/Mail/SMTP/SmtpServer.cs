using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net.Mail;
using System.Net.Mime;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using IctBaden.Framework.Network;
// ReSharper disable StringLiteralTypo

namespace IctBaden.Internet.Mail.SMTP
{
    // HINT: Do NOT set header information receiving mails. This will result in invalid sending this.

    // ReSharper disable once ClassWithVirtualMembersNeverInherited.Global
    public class SmtpServer : SocketCommandLineServer
    {
        private enum SmtpState
        {
            Control,
            Authentication1,
            Authentication2,
            Multipart,
            Data,
            Body
        }
        private class IncommingMail
        {
            public readonly MailMessage Message = new MailMessage();
            public SmtpState State = SmtpState.Control;
            public string Body = string.Empty;
            public string ContentType = string.Empty;
            public string ContentEncoding = string.Empty;
            public string Boundary = string.Empty;
            public string PartContentType = string.Empty;
            public string PartContentEncoding = string.Empty;
            public string PartName = string.Empty;
            public string PartDisposition = string.Empty;
            public string RecentLine = string.Empty;

            public bool IsMultipart => ContentType.Contains("multipart");
        }

        private readonly Dictionary<Socket, IncommingMail> _mails;

        public event Action<MailMessage> NewMail;

        protected virtual void OnNewMail(MailMessage mail)
        {
            NewMail?.Invoke(mail);
        }

        public SmtpServer(int tcpPort)
            : base(tcpPort)
        {
            Eoc = new List<string> { Environment.NewLine };
            UseEncoding = Encoding.Default;
            HandleEmptyCommands = true;

            ClientConnected += OnClientConnected;
            HandleCommand += OnHandleCommand;

            _mails = new Dictionary<Socket, IncommingMail>();
        }

        private void OnClientConnected(Socket client)
        {
            Send(client, "220 localhost -- Simple SMTP server");
            if (_mails.ContainsKey(client))
            {
                _mails.Remove(client);
            }

            _mails.Add(client, new IncommingMail());
        }

        private static void Send(Socket client, string text)
        {
            text += Environment.NewLine;
            Debug.Write("SMTP-Server Tx: " + text);
            client.Send(Encoding.UTF8.GetBytes(text));
        }

        private void OnHandleCommand(Socket client, string commandLine)
        {
            commandLine = commandLine.TrimEnd();
            Debug.WriteLine("SMTP-Server Rx: " + commandLine);

            if (!_mails.ContainsKey(client))
            {
                Debug.WriteLine("SMTP-Server Rx: Unknown client - ignore data");
                return;
            }

            var context = _mails[client];
            switch (context.State)
            {
                case SmtpState.Control:
                    OnHandleControlCommand(client, context, commandLine);
                    break;
                case SmtpState.Authentication1:
                    OnHandleAuthentication1Command(client, context);
                    break;
                case SmtpState.Authentication2:
                    OnHandleAuthentication2Command(client, context);
                    break;
                case SmtpState.Multipart:
                    OnHandleMultipartCommand(context, commandLine);
                    break;
                case SmtpState.Data:
                    if (commandLine.StartsWith(" ") || commandLine.StartsWith("\t"))
                    {
                        commandLine = context.RecentLine + commandLine.TrimStart();
                    }
                    OnHandleDataCommand(context, commandLine);
                    break;
                case SmtpState.Body:
                    OnHandleBodyCommand(client, context, commandLine);
                    break;
            }
            context.RecentLine = commandLine;
        }

        private void OnHandleControlCommand(Socket client, IncommingMail context, string commandLine)
        {
            if (commandLine.StartsWith("QUIT", StringComparison.InvariantCultureIgnoreCase))
            {
                Send(client, "221 Closing channel");
                client.Close();
                return;
            }

            if (commandLine.StartsWith("EHLO", StringComparison.InvariantCultureIgnoreCase))
            {
                Send(client, "250-AUTH LOGIN");
                Send(client, "250 OK");
                return;
            }
            if (commandLine.StartsWith("HELO", StringComparison.InvariantCultureIgnoreCase))
            {
                Send(client, "250 OK");
                return;
            }

            if (commandLine.StartsWith("AUTH", StringComparison.InvariantCultureIgnoreCase))
            {
                context.State = SmtpState.Authentication1;
                Send(client, "334 " + Convert.ToBase64String(Encoding.ASCII.GetBytes("useless")));
                return;
            }

            if (commandLine.StartsWith("RCPT TO", StringComparison.InvariantCultureIgnoreCase))
            {
                var addr = new Regex(@".*\<([a-zA-Z0-9\.\@_-]+)\>.*").Match(commandLine.Substring(7));
                if (addr.Success)
                {
                    context.Message.To.Add(addr.Groups[1].Value);
                }
                Send(client, "250 OK");
                return;
            }

            if (commandLine.StartsWith("MAIL FROM", StringComparison.InvariantCultureIgnoreCase))
            {
                var addr = new Regex(@".*\<([a-zA-Z0-9\.\@]+)\>.*").Match(commandLine.Substring(7));
                if (addr.Success)
                {
                    context.Message.From = new MailAddress(addr.Groups[1].Value);
                }
                Send(client, "250 OK");
                return;
            }

            if (commandLine.StartsWith("DATA", StringComparison.InvariantCultureIgnoreCase))
            {
                context.State = SmtpState.Data;
                Send(client, "354 Start mail input");
            }
        }

        private void OnHandleAuthentication1Command(Socket client, IncommingMail context)
        {
            context.State = SmtpState.Authentication2;
            Send(client, "334 " + Convert.ToBase64String(Encoding.ASCII.GetBytes("useless")));
        }

        private void OnHandleAuthentication2Command(Socket client, IncommingMail context)
        {
            context.State = SmtpState.Control;
            Send(client, "235 Authentication successful.");
        }

        private void OnHandleMultipartCommand(IncommingMail context, string commandLine)
        {
            if (context.IsMultipart && (commandLine == "--" + context.Boundary))
            {
                context.State = SmtpState.Data;
            }
        }

        private void OnHandleDataCommand(IncommingMail context, string commandLine)
        {
            // parse header data here
            if (commandLine == string.Empty)
            {
                context.State = SmtpState.Body;
                return;
            }

            if (commandLine.StartsWith("Subject:", StringComparison.InvariantCultureIgnoreCase))
            {
                context.Message.Subject = commandLine.Substring(8).Trim();
                if (context.Message.Subject.StartsWith("=") && context.Message.Subject.EndsWith("="))
                {
                    var decoder = new QuotedPrintable();
                    var iso = Encoding.GetEncoding("ISO-8859-1");
                    context.Message.Subject = iso.GetString(decoder.Decode(context.Message.Subject));
                }
                context.RecentLine = commandLine;
                return;
            }

            if (commandLine.StartsWith("Content-Type:", StringComparison.InvariantCultureIgnoreCase))
            {
                var contentType = commandLine.Substring(13).Trim();
                if (context.IsMultipart)
                {
                    context.PartContentType = contentType;
                    if (contentType.Contains("name="))
                    {
                        var name = contentType.IndexOf("name=", StringComparison.InvariantCultureIgnoreCase);
                        context.PartName = contentType.Substring(name + 5).Trim();
                    }
                    if (string.IsNullOrEmpty(context.PartName) && contentType.Contains("file="))
                    {
                        var file = contentType.IndexOf("file=", StringComparison.InvariantCultureIgnoreCase);
                        context.PartName = contentType.Substring(file + 5).Trim();
                    }
                    if (string.IsNullOrEmpty(context.PartName) && contentType.Contains("filename="))
                    {
                        var filename = context.PartDisposition.IndexOf("filename=", StringComparison.InvariantCultureIgnoreCase);
                        context.PartName = context.PartDisposition.Substring(filename + 9).Trim();
                    }
                }
                else
                {
                    context.ContentType = contentType;
                    if (contentType.Contains("multipart") && contentType.Contains("boundary="))
                    {
                        var boundary = contentType.IndexOf("boundary=", StringComparison.InvariantCultureIgnoreCase);
                        context.Boundary = contentType.Substring(boundary + 9).Trim();
                    }
                }

                if (context.IsMultipart && commandLine.Contains("boundary="))
                {
                    var boundary = commandLine.IndexOf("boundary=", StringComparison.InvariantCultureIgnoreCase);
                    context.Boundary = commandLine.Substring(boundary + 9).Trim();
                }

                context.RecentLine = commandLine;
                return;
            }

            if (commandLine.StartsWith("Content-Disposition:", StringComparison.InvariantCultureIgnoreCase))
            {
                var disposition = commandLine.Substring(20).Trim();
                context.PartDisposition = disposition;
                context.RecentLine = commandLine;
                return;
            }
            if (commandLine.StartsWith("Content-Transfer-Encoding:", StringComparison.InvariantCultureIgnoreCase))
            {
                var encoding = commandLine.Substring(26).Trim();
                if (context.IsMultipart)
                {
                    context.PartContentEncoding = encoding;
                }
                else
                {
                    context.ContentEncoding = encoding;
                }
                context.RecentLine = commandLine;
            }
        }

        private void OnHandleBodyCommand(Socket client, IncommingMail context, string commandLine)
        {
            // handle mail body
            if (context.IsMultipart && commandLine.StartsWith("--" + context.Boundary))
            {
                if (context.PartDisposition.Contains("attachment"))
                {
                    ContentType contentType = null;
                    try
                    {
                        if (!string.IsNullOrEmpty(context.PartContentType))
                        {
                            contentType = new ContentType(context.PartContentType);
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.TraceWarning("Invalid ContentType '" + context.PartContentType + "' ignored: " + ex.Message);
                    }
                    var data = new MemoryStream(GetBytes(context.Body, context.PartContentEncoding));

                    var attachment = (contentType != null) ? new Attachment(data, contentType) : new Attachment(data, context.PartName);
                    if ((attachment.Name == null) && !string.IsNullOrEmpty(context.PartName))
                    {
                        attachment.Name = context.PartName;
                    }
                    context.Message.Attachments.Add(attachment);
                }
                else
                {
                    context.Message.BodyEncoding = null;
                    context.Message.Body = Encoding.UTF8.GetString(GetBytes(context.Body, context.PartContentEncoding));
                    context.Message.IsBodyHtml = context.PartContentType.ToLower().Contains("html");
                    if (context.Message.IsBodyHtml)
                    {
                        context.Message.BodyEncoding = DetectHtmlEncoding(context.Message.Body);
                    }
                }
                context.PartContentType = string.Empty;
                context.PartContentEncoding = string.Empty;
                context.PartName = string.Empty;
                context.PartDisposition = string.Empty;
                context.Body = string.Empty;
                context.State = SmtpState.Data;
                return;
            }
            if (commandLine == ".")
            {
                //message has successfully been received
                if (!context.IsMultipart)
                {
                    context.Message.BodyEncoding = null;
                    context.Message.Body = Encoding.UTF8.GetString(GetBytes(context.Body, context.ContentEncoding));
                    context.Message.IsBodyHtml = context.ContentType.ToLower().Contains("html");
                    if (context.Message.IsBodyHtml)
                    {
                        context.Message.BodyEncoding = DetectHtmlEncoding(context.Message.Body);
                    }
                }
                OnNewMail(context.Message);
                Send(client, "250 OK");
                return;
            }

            context.Body += commandLine;
        }

        private Encoding DetectHtmlEncoding(string html)
        {
            var charset = html.IndexOf("charset=", StringComparison.CurrentCultureIgnoreCase);
            if (charset != -1)
            {
                var encoding = html.Substring(charset + 8).Trim();
                var end = encoding.IndexOfAny(new[] { '"', ';', ' ' });
                if (end != -1)
                {
                    encoding = encoding.Substring(0, end);
                    try
                    {
                        var enc = Encoding.GetEncoding(encoding);
                        return enc;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine(ex.Message);
                    }
                }
            }

            return Encoding.ASCII;
        }

        private byte[] GetBytes(string text, string encoding)
        {
            if (encoding == "base64")
            {
                return Convert.FromBase64String(text);
            }
            if (encoding == "quoted-printable")
            {
                var decoder = new QuotedPrintable();
                var iso = Encoding.GetEncoding("ISO-8859-1");
                return Encoding.UTF8.GetBytes(iso.GetString(decoder.Decode(text)));
            }

            return Encoding.UTF8.GetBytes(text);
        }
    }
}
