using System.Collections.Specialized;

namespace IctBaden.Internet.Mail.POP3
{
	/// <summary>
	/// Represents a part of a MIME multi-part message. Each part consists
	/// of its own content header and a content body.
	/// </summary>
	// ReSharper disable once InconsistentNaming
	internal class MIMEPart
    {
		/// <summary>
		/// A collection containing the content header information as
		/// key-value pairs.
		/// </summary>
		// ReSharper disable once InconsistentNaming
		public NameValueCollection header { get; set; }
		/// <summary>
		/// A string containing the content body of the part.
		/// </summary>
		// ReSharper disable once InconsistentNaming
		public string body { get; set; }
	}
}
