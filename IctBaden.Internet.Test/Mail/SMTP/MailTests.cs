using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Text;
using IctBaden.Internet.Mail.SMTP;
using Xunit;
// ReSharper disable StringLiteralTypo

[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace IctBaden.Internet.Test.Mail.SMTP
{
    public class SmtpTests : IDisposable
    {
        private SmtpServer _testServer;
        private SmtpClient _testClient;
        private MailMessage _receivedMessage;

        public void Dispose()
        {
            if (_testServer != null)
            {
                _testServer.Terminate();
                _testServer = null;
            }
            if (_testClient != null)
            {
                try
                {
                    _testClient.Dispose();
                }
                catch (Exception ex)
                {
                    Debug.WriteLine(ex.Message);
                }
                _testClient = null;
            }
            _receivedMessage = null;
        }

        private void StartTestEnvironment()
        {
            _testServer = new SmtpServer(25);
            _testServer.Start();
            _testServer.NewMail += mailMessage => _receivedMessage = mailMessage;

            _testClient = new SmtpClient("localhost", 25) { Timeout = 100000 };
        }

        [Fact]
        public void CreateAndStartSmtpServer()
        {
            var server = new SmtpServer(25);
            Assert.NotNull(server);

            var started = server.Start();
            Assert.True(started);

            server.Terminate();
        }

        [Fact]
        public void ReceiveSimpleMail()
        {
            StartTestEnvironment();

            var message = new MailMessage("from@ict.test.de", "to@ict.test.de")
            {
                Subject = "TestSubject",
                Body = "TestBody"
            };

            try
            {
                _testClient.Send(message);
            }
            catch (SmtpException ex)
            {
                Assert.True(false, ex.Message);
            }

            Assert.Equal(message.From, _receivedMessage.From);
            Assert.Equal(message.To, _receivedMessage.To);
            Assert.Equal(message.Subject, _receivedMessage.Subject);
            Assert.Equal(message.Body, _receivedMessage.Body);
        }

        [Fact]
        public void ReceiveMailWithUmlauts()
        {
            StartTestEnvironment();

            var message = new MailMessage("from@ict.test.de", "to@ict.test.de")
            {
                Subject = "TestSübject",
                Body = "TestBödy"
            };

            try
            {
                _testClient.Send(message);
            }
            catch (SmtpException ex)
            {
                Assert.True(false, ex.Message);
            }

            Assert.Equal(message.From, _receivedMessage.From);
            Assert.Equal(message.To, _receivedMessage.To);
            Assert.Equal(message.Subject, _receivedMessage.Subject);
            Assert.Equal(message.Body, _receivedMessage.Body);
        }

        [Fact]
        public void ReceiveMailWithTextAttachment()
        {
            StartTestEnvironment();

            var message = new MailMessage("from@ict.test.de", "to@ict.test.de")
            {
                Subject = "TestWithAttachment",
                Body = "See attached text"
            };

            const string attachmentText = "Content of text file.";
            var attachmentData = new MemoryStream(Encoding.UTF8.GetBytes(attachmentText));
            var messageAttachment = new Attachment(attachmentData, "test.txt");
            message.Attachments.Add(messageAttachment);

            try
            {
                _testClient.Send(message);
            }
            catch (SmtpException ex)
            {
                Assert.True(false, ex.Message);
            }

            Assert.Equal(message.From, _receivedMessage.From);
            Assert.Equal(message.To, _receivedMessage.To);
            Assert.Equal(message.Subject, _receivedMessage.Subject);
            Assert.Equal(message.Body, _receivedMessage.Body);
            Assert.Single(_receivedMessage.Attachments);

            var receivedAttachment = _receivedMessage.Attachments[0];
            Assert.Equal(messageAttachment.Name, receivedAttachment.Name);

            var receivedData = new byte[receivedAttachment.ContentStream.Length];
            receivedAttachment.ContentStream.Read(receivedData, 0, receivedData.Length);
            var receivedText = Encoding.UTF8.GetString(receivedData);
            Assert.Equal(attachmentText, receivedText);
        }

        [Fact]
        public void ReceiveMailWithImageAttachment()
        {
            StartTestEnvironment();

            var message = new MailMessage("from@ict.test.de", "to@ict.test.de")
            {
                Subject = "TestWithAttachment",
                Body = "See attached image"
            };

            var messageAttachment = new Attachment("ICT-BADEN.png", "image/png");
            message.Attachments.Add(messageAttachment);

            try
            {
                _testClient.Send(message);
            }
            catch (SmtpException ex)
            {
                Assert.True(false, ex.Message);
            }

            Assert.Equal(message.From, _receivedMessage.From);
            Assert.Equal(message.To, _receivedMessage.To);
            Assert.Equal(message.Subject, _receivedMessage.Subject);
            Assert.Equal(message.Body, _receivedMessage.Body);
            Assert.Single(_receivedMessage.Attachments);

            var receivedAttachment = _receivedMessage.Attachments[0];
            Assert.Equal(messageAttachment.Name, receivedAttachment.Name);

            var receivedData = new byte[receivedAttachment.ContentStream.Length];
            receivedAttachment.ContentStream.Read(receivedData, 0, receivedData.Length);

            var fileData = File.ReadAllBytes("ICT-BADEN.png");
            Assert.Equal(receivedData, fileData);
        }

        [Fact]
        public void ReceiveMailWithMultipleAttachments()
        {
            StartTestEnvironment();

            var message = new MailMessage("from@ict.test.de", "to@ict.test.de")
            {
                Subject = "TestWithAttachment",
                Body = "See attached image"
            };

            const string attachmentText1 = "Content of text attachment 1.";
            var attachmentData = new MemoryStream(Encoding.UTF8.GetBytes(attachmentText1));
            var messageAttachment1 = new Attachment(attachmentData, "test1.txt");
            message.Attachments.Add(messageAttachment1);

            var messageAttachment2 = new Attachment("ICT-BADEN.png", "image/png");
            message.Attachments.Add(messageAttachment2);

            const string attachmentText3 = "Content of text attachment 2.";
            attachmentData = new MemoryStream(Encoding.UTF8.GetBytes(attachmentText3));
            var messageAttachment3 = new Attachment(attachmentData, "test2.txt");
            message.Attachments.Add(messageAttachment3);

            try
            {
                _testClient.Send(message);
            }
            catch (SmtpException ex)
            {
                Assert.True(false, ex.Message);
            }

            Assert.Equal(message.From, _receivedMessage.From);
            Assert.Equal(message.To, _receivedMessage.To);
            Assert.Equal(message.Subject, _receivedMessage.Subject);
            Assert.Equal(message.Body, _receivedMessage.Body);
            Assert.Equal(3, _receivedMessage.Attachments.Count);

            // check attachment 1 : text
            var receivedAttachment = _receivedMessage.Attachments[0];
            Assert.Equal(messageAttachment1.Name, receivedAttachment.Name);

            var receivedData = new byte[receivedAttachment.ContentStream.Length];
            receivedAttachment.ContentStream.Read(receivedData, 0, receivedData.Length);
            var receivedText = Encoding.UTF8.GetString(receivedData);
            Assert.Equal(attachmentText1, receivedText);

            // check attachment 2 : image
            receivedAttachment = _receivedMessage.Attachments[1];
            Assert.Equal(messageAttachment2.Name, receivedAttachment.Name);

            receivedData = new byte[receivedAttachment.ContentStream.Length];
            receivedAttachment.ContentStream.Read(receivedData, 0, receivedData.Length);

            var fileData = File.ReadAllBytes("ICT-BADEN.png");
            Assert.Equal(receivedData, fileData);

            // check attachment 3 : text
            receivedAttachment = _receivedMessage.Attachments[2];
            Assert.Equal(messageAttachment3.Name, receivedAttachment.Name);

            receivedData = new byte[receivedAttachment.ContentStream.Length];
            receivedAttachment.ContentStream.Read(receivedData, 0, receivedData.Length);
            receivedText = Encoding.UTF8.GetString(receivedData);
            Assert.Equal(attachmentText3, receivedText);
        }

        [Fact]
        public void ReceiveAuthenticatedMail()
        {
            StartTestEnvironment();

            var message = new MailMessage("from@ict.test.de", "to@ict.test.de")
            {
                Subject = "TestSubject",
                Body = "Using Credentials"
            };

            try
            {
                _testClient.Credentials = new NetworkCredential("Tester", "geheim :-)");
                _testClient.Send(message);
            }
            catch (SmtpException ex)
            {
                Assert.True(false, ex.Message);
            }

            Assert.Equal(message.From, _receivedMessage.From);
            Assert.Equal(message.To, _receivedMessage.To);
            Assert.Equal(message.Subject, _receivedMessage.Subject);
            Assert.Equal(message.Body, _receivedMessage.Body);
        }

    }
}