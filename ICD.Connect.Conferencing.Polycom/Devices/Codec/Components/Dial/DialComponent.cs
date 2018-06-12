using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.Dial
{
	public sealed class DialComponent : AbstractPolycomComponent
	{
		private static readonly BiDictionary<eDialProtocol, string> s_ProtocolNames =
			new BiDictionary<eDialProtocol, string>
			{
				{eDialProtocol.Sip, "sip"},
				{eDialProtocol.H323, "h323"},
				{eDialProtocol.Auto, "auto"},
				{eDialProtocol.SipSpeakerphone, "sip_speakerphone"}
			};

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public DialComponent(PolycomGroupSeriesDevice codec)
			: base(codec)
		{
			Subscribe(Codec);

			if (Codec.Initialized)
				Initialize();
		}

		#region Methods

		/// <summary>
		/// Dials the contact with the given name.
		/// </summary>
		/// <param name="contactName"></param>
		public void DialAddressbook(string contactName)
		{
			if (contactName == null)
				throw new ArgumentNullException("contactName");

			contactName = StringUtils.Enquote(contactName);

			Codec.SendCommand("dial addressbook {0}", contactName);
			Codec.Log(eSeverity.Informational, "Dialing addressbook contact {0}", StringUtils.ToRepresentation(contactName));
		}

		/// <summary>
		/// Dials the given phone number.
		/// </summary>
		/// <param name="protocol"></param>
		/// <param name="number"></param>
		public void DialPhone(eDialProtocol protocol, string number)
		{
			if (number == null)
				throw new ArgumentNullException("number");

			number = StringUtils.Enquote(number);

			string protocolName = s_ProtocolNames.GetValue(protocol);

			Codec.SendCommand("dial phone {0} {1}", protocolName, number);
			Codec.Log(eSeverity.Informational, "Dialing phone number {0} {1}", protocolName, StringUtils.ToRepresentation(number));
		}

		#endregion

		#region Console

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			yield return new GenericConsoleCommand<string>("DialAddressbook", "DialAddressbook <NAME>", n => DialAddressbook(n));

			string dialPhoneHelp = string.Format("DialPhone <{0}> <NUMBER>",
			                                     StringUtils.ArrayFormat(EnumUtils.GetValues<eDialProtocol>()));
			yield return new GenericConsoleCommand<string>("DialPhone", dialPhoneHelp, n => DialAddressbook(n));
		}

		/// <summary>
		/// Shim to avoid "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		#endregion
	}
}
