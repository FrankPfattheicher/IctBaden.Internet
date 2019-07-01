using System;

namespace IctBaden.Internet.Mail.POP3
{
	/// <summary>
	/// Describes status information of a mail message.
	/// </summary>
	[Serializable]
	public class MessageInfo
    {
		internal MessageInfo(uint messageNumber, ulong messageSize)
        {
			Number = messageNumber;
			Size = messageSize;
		}

		/// <summary>
		/// The message number of this mail message.
		/// </summary>
		public uint Number { get; private set; }

		/// <summary>
		/// The size of this mail message, in bytes.
		/// </summary>
		public ulong Size { get; private set; }
	}
}
