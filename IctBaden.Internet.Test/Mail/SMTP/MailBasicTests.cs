using System;
using System.Net.Mail;
using System.Text;
using System.Threading;
using IctBaden.Framework.Network;
using IctBaden.Internet.Mail.SMTP;
using Xunit;
// ReSharper disable StringLiteralTypo

namespace IctBaden.Internet.Test.Mail.SMTP
{
    public class SmtpBasicTests : IDisposable
    {
        private SmtpServer _testServer;
        private readonly SocketCommandClient _testClient;
        private string _receivedData;
        private MailMessage _receivedMessage;

        
        public SmtpBasicTests()
        {
            _testServer = new SmtpServer(25);
            _testServer.Start();
            _testServer.NewMail += mailMessage => _receivedMessage = mailMessage;
            _receivedMessage = null;

            _testClient = new SocketCommandClient("localhost", 25, s => { _receivedData = s; });
            _testClient.Connect();
            _receivedData = null;
        }

        public void Dispose()
        {
            if (_testServer != null)
            {
                _testServer.Terminate();
                _testServer = null;
            }
            _receivedMessage = null;
        }

        private void WaitForData(int timeout = 1000)
        {
            while ((timeout > 0) && string.IsNullOrEmpty(_receivedData))
            {
                Thread.Sleep(100);
                timeout -= 100;
            }
        }

        private void SendLine(string line)
        {
            _receivedData = null;
            _testClient.SendCommand(line + "\r\n");
        }

        // ReSharper disable once UnusedParameter.Local
        private void SendCommand(string command, string expectedResponse)
        {
            if (expectedResponse == null) throw new ArgumentNullException(nameof(expectedResponse));
            
            SendLine(command);
            WaitForData();
            Assert.False(string.IsNullOrEmpty(_receivedData));
            Assert.StartsWith(expectedResponse, _receivedData);
        }

        [Fact]
        public void ReceiveWelcomeMessage()
        {
            WaitForData();
            Assert.False(string.IsNullOrEmpty(_receivedData));
            Assert.StartsWith("220", _receivedData);
        }

        [Fact]
        public void SimpleSendOk()
        {
            WaitForData();
            SendCommand("HELO test", "250 OK");
            SendCommand("MAIL FROM:<from@ict.test.de>", "250 OK");
            SendCommand("RCPT TO:<to@ict.test.de>", "250 OK");
            SendCommand("DATA", "354 ");
            SendLine("MIME-Version: 1.0");
            SendLine("From: from@ict.test.de");
            SendLine("To: to@ict.test.de");
            SendLine("Date: 2 May 2014 08:15:18 +0200");
            SendLine("Subject: TestSubject");
            SendLine("Content-Type: text/plain; charset=us-ascii");
            SendLine("Content-Transfer-Encoding: quoted-printable");
            SendLine("");
            SendLine("Test Body");
            SendLine("");
            SendCommand(".", "250 OK");

            Assert.NotNull(_receivedMessage);
            Assert.Equal("Test Body", _receivedMessage.Body);
            Assert.Equal(Encoding.ASCII, _receivedMessage.BodyEncoding);
            Assert.Empty(_receivedMessage.Headers.AllKeys);
        }

        [Fact]
        public void SendQuotedPrintableBodyOk()
        {
            WaitForData();
            SendCommand("HELO test", "250 OK");
            SendCommand("MAIL FROM:<from@ict.test.de>", "250 OK");
            SendCommand("RCPT TO:<to@ict.test.de>", "250 OK");
            SendCommand("DATA", "354 ");
            SendLine("MIME-Version: 1.0");
            SendLine("From: from@ict.test.de");
            SendLine("To: to@ict.test.de");
            SendLine("Date: 2 May 2014 08:15:18 +0200");
            SendLine("Subject: TestSubject");
            SendLine("Content-Type: text/plain");
            SendLine("Content-Transfer-Encoding: quoted-printable");
            SendLine("");
            SendLine("M=F6d=F6Corrente Tageslastgang Wansleben am Strand");
            SendLine("");
            SendCommand(".", "250 OK");

            Assert.NotNull(_receivedMessage);
            Assert.Equal("MödöCorrente Tageslastgang Wansleben am Strand", _receivedMessage.Body);
            Assert.Equal(Encoding.UTF8, _receivedMessage.BodyEncoding);
            Assert.Empty(_receivedMessage.Headers.AllKeys);
        }

        [Fact]
        public void SendMissingContentTypeOk()
        {
            WaitForData();
            SendCommand("HELO test", "250 OK");
            SendCommand("MAIL FROM:<from@ict.test.de>", "250 OK");
            SendCommand("RCPT TO:<to@ict.test.de>", "250 OK");
            SendCommand("DATA", "354 ");
            SendLine("MIME-Version: 1.0");
            SendLine("From: from@ict.test.de");
            SendLine("To: to@ict.test.de");
            SendLine("Date: 2 May 2014 08:15:18 +0200");
            SendLine("Subject: TestSubject");
            SendLine("Content-Type:");
            SendLine("Content-Transfer-Encoding: quoted-printable");
            SendLine("");
            SendLine("Test Body");
            SendLine("");
            SendCommand(".", "250 OK");

            Assert.NotNull(_receivedMessage);
            Assert.Equal("Test Body", _receivedMessage.Body);
            Assert.Empty(_receivedMessage.Headers.AllKeys);
        }

        [Fact]
        public void SendInvalidContentTypeOk()
        {
            WaitForData();
            SendCommand("HELO test", "250 OK");
            SendCommand("MAIL FROM:<from@ict.test.de>", "250 OK");
            SendCommand("RCPT TO:<to@ict.test.de>", "250 OK");
            SendCommand("DATA", "354 ");
            SendLine("MIME-Version: 1.0");
            SendLine("From: from@ict.test.de");
            SendLine("To: to@ict.test.de");
            SendLine("Date: 2 May 2014 08:15:18 +0200");
            SendLine("Subject: TestSubject");
            SendLine("Content-Type: must/fail");
            SendLine("Content-Transfer-Encoding: quoted-printable");
            SendLine("");
            SendLine("Test Body");
            SendLine("");
            SendCommand(".", "250 OK");

            Assert.NotNull(_receivedMessage);
            Assert.Equal("Test Body", _receivedMessage.Body);
            Assert.Empty(_receivedMessage.Headers.AllKeys);
        }

        [Fact]
        public void SendSubjectQuotedPrintableOk()
        {
            WaitForData();
            SendCommand("HELO test", "250 OK");
            SendCommand("MAIL FROM:<from@ict.test.de>", "250 OK");
            SendCommand("RCPT TO:<to@ict.test.de>", "250 OK");
            SendCommand("DATA", "354 ");
            SendLine("MIME-Version: 1.0");
            SendLine("From: from@ict.test.de");
            SendLine("To: to@ict.test.de");
            SendLine("Date: 2 May 2014 08:15:18 +0200");
            SendLine("Subject: =?iso-8859-1?Q?Stora_Farsn=E4s=2DFL_799_=23799=3A_02=2E05=2E2014_10=3A14=2D?=");
            SendLine("  =?iso-8859-1?Q?M.Bear_OilPressPp_?=");
            SendLine("\t=?iso-8859-1?Q?<_SL(1381)?=");
            SendLine("Content-Type: text/plain");
            SendLine("Content-Transfer-Encoding: quoted-printable");
            SendLine("");
            SendLine("Test Body");
            SendLine("");
            SendCommand(".", "250 OK");

            Assert.NotNull(_receivedMessage);
            Assert.Equal("Stora Farsnäs-FL 799 #799: 02.05.2014 10:14-M.Bear OilPressPp < SL(1381)", _receivedMessage.Subject);
            Assert.Equal("Test Body", _receivedMessage.Body);
            Assert.Empty(_receivedMessage.Headers.AllKeys);
        }

        [Fact]
        public void MultipartSendOk()
        {
            WaitForData();
            SendCommand("HELO test", "250 OK");
            SendCommand("MAIL FROM:<from@ict.test.de>", "250 OK");
            SendCommand("RCPT TO:<to@ict.test.de>", "250 OK");
            SendCommand("DATA", "354 ");
            SendLine("MIME-Version: 1.0");
            SendLine("From: from@ict.test.de");
            SendLine("To: to@ict.test.de");
            SendLine("Date: 2 May 2014 08:15:18 +0200");
            SendLine("Subject: TestWithAttachment");
            SendLine("Content-Type: multipart/mixed;");
            SendLine(" boundary=--boundary_0_18326a9d-a0fe-4ba2-aa67-eae449deef9d");
            SendLine("");
            SendLine("");
            SendLine("");
            SendLine("----boundary_0_18326a9d-a0fe-4ba2-aa67-eae449deef9d");
            SendLine("Content-Type: text/plain; charset=us-ascii");
            SendLine("Content-Transfer-Encoding: quoted-printable");
            SendLine("");
            SendLine("See attached text");
            SendLine("----boundary_0_18326a9d-a0fe-4ba2-aa67-eae449deef9d");
            SendLine("Content-Type: application/octet-stream;");
            SendLine("  name=test.txt");
            SendLine("Content-Transfer-Encoding: base64");
            SendLine("Content-Disposition: attachment");
            SendLine("");
            SendLine("Q29udGVudCBvZiB0ZXh0IGZpbGUu");
            SendLine("----boundary_0_18326a9d-a0fe-4ba2-aa67-eae449deef9d--");
            SendLine("");
            SendLine("");
            SendCommand(".", "250 OK");

            Assert.NotNull(_receivedMessage);
            Assert.Equal("See attached text", _receivedMessage.Body);
            Assert.Single(_receivedMessage.Attachments);
            Assert.Equal("test.txt", _receivedMessage.Attachments[0].Name);
            Assert.Empty(_receivedMessage.Headers.AllKeys);
        }

        [Fact]
        public void DoNotFailOnInvalidFormattedParts()
        {
            WaitForData();
            SendCommand("HELO test", "250 OK");
            SendCommand("MAIL FROM:<from@ict.test.de>", "250 OK");
            SendCommand("RCPT TO:<to@ict.test.de>", "250 OK");
            SendCommand("DATA", "354 ");
            SendLine("MIME-Version: 1.0");
            SendLine("From: from@ict.test.de");
            SendLine("To: to@ict.test.de");
            SendLine("Date: 2 May 2014 08:15:18 +0200");
            SendLine("Subject: TestWithAttachment");
            SendLine("Content-Type: multipart/mixed;");
            SendLine(" boundary=--boundary_0_18326a9d-a0fe-4ba2-aa67-eae449deef9d");
            SendLine("");
            SendLine("");
            SendLine("");
            SendLine("----boundary_0_18326a9d-a0fe-4ba2-aa67-eae449deef9d");
            SendLine("Content-Type: text/plain; charset=");
            SendLine("Content-Type: text/plain; charset=us-ascii");
            SendLine("Content-Transfer-Encoding: quoted-printable");
            SendLine("");
            SendLine("See attached text");
            SendLine("----boundary_0_18326a9d-a0fe-4ba2-aa67-eae449deef9d");
            SendLine("Content-Type: application/octet-stream; name=");
            SendLine("Content-Type: application/octet-stream; name=test.txt");
            SendLine("Content-Transfer-Encoding: base64");
            SendLine("Content-Disposition: attachment; filename=?");
            SendLine("");
            SendLine("Q29udGVudCBvZiB0ZXh0IGZpbGUu");
            SendLine("----boundary_0_18326a9d-a0fe-4ba2-aa67-eae449deef9d--");
            SendLine("");
            SendLine("");
            SendLine(".");
        }

        [Fact]
        public void HtmlUtf8BodyOk()
        {
            WaitForData();
            SendCommand("HELO test", "250 OK");
            SendCommand("MAIL FROM:<from@ict.test.de>", "250 OK");
            SendCommand("RCPT TO:<to@ict.test.de>", "250 OK");
            SendCommand("DATA", "354 ");
            SendLine("MIME-Version: 1.0");
            SendLine("From: from@ict.test.de");
            SendLine("To: to@ict.test.de");
            SendLine("Date: 2 May 2014 08:15:18 +0200");
            SendLine("Subject: It is HTML");
            SendLine("Content-Type: text/html;");
            SendLine("");
            SendLine("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\"> <html xmlns");
            SendLine("=\"http://www.w3.org/1999/xhtml\">  <head> <meta content=\"de\" http-equiv=\"Content-Language\" /> <meta content=\"text/html; charset=utf-8\" http-equiv=\"C");
            SendLine("ontent-Type\" /> <title>ICT Baden - Downloads</title> <link href=\"../../styles/ict-baden.css\" rel=\"stylesheet\" type=\"text/css\" />  <script type=\"tex");
            SendLine("t/javascript\"> <!-- if(top == self) {   self.location.href = \"../../index.html?navigate=Community/Mono/index.html\"; } //--> </script>  </head>  <bo");
            SendLine("dy>  <h1>Mono</h1> <p>&nbsp;</p>  </body>  </html>");
            SendLine("");
            SendCommand(".", "250 OK");

            Assert.NotNull(_receivedMessage);
            Assert.Equal("It is HTML", _receivedMessage.Subject);
            Assert.True(_receivedMessage.IsBodyHtml);
            Assert.Equal(Encoding.UTF8, _receivedMessage.BodyEncoding);
            Assert.Empty(_receivedMessage.Headers.AllKeys);
        }

        [Fact]
        public void HtmlLatin1BodyOk()
        {
            WaitForData();
            SendCommand("HELO test", "250 OK");
            SendCommand("MAIL FROM:<from@ict.test.de>", "250 OK");
            SendCommand("RCPT TO:<to@ict.test.de>", "250 OK");
            SendCommand("DATA", "354 ");
            SendLine("MIME-Version: 1.0");
            SendLine("From: from@ict.test.de");
            SendLine("To: to@ict.test.de");
            SendLine("Date: 2 May 2014 08:15:18 +0200");
            SendLine("Subject: It is HTML");
            SendLine("Content-Type: text/html;");
            SendLine("");
            SendLine("<!DOCTYPE html PUBLIC \"-//W3C//DTD XHTML 1.0 Transitional//EN\" \"http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd\"> <html xmlns");
            SendLine("=\"http://www.w3.org/1999/xhtml\">  <head> <meta content=\"de\" http-equiv=\"Content-Language\" /> <meta content=\"text/html; charset=ISO-8859-1\" http-equiv=\"C");
            SendLine("ontent-Type\" /> <title>ICT Baden - Downloads</title> <link href=\"../../styles/ict-baden.css\" rel=\"stylesheet\" type=\"text/css\" />  <script type=\"tex");
            SendLine("t/javascript\"> <!-- if(top == self) {   self.location.href = \"../../index.html?navigate=Community/Mono/index.html\"; } //--> </script>  </head>  <bo");
            SendLine("dy>  <h1>Mono</h1> <p>&nbsp;</p>  </body>  </html>");
            SendLine("");
            SendCommand(".", "250 OK");

            var expectedEncoding = Encoding.GetEncoding("ISO-8859-1");
            
            Assert.NotNull(_receivedMessage);
            Assert.Equal("It is HTML", _receivedMessage.Subject);
            Assert.True(_receivedMessage.IsBodyHtml);
            Assert.Equal(expectedEncoding, _receivedMessage.BodyEncoding);
            Assert.Empty(_receivedMessage.Headers.AllKeys);
        }

    }
}
