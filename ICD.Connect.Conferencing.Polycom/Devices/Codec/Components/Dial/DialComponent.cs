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

		private static readonly BiDictionary<eDialType, string> s_TypeNames =
			new BiDictionary<eDialType, string>
			{
				{eDialType.H323, "h323"},
				{eDialType.Ip, "ip"},
				{eDialType.Sip, "sip"},
				{eDialType.Gateway, "gateway"}
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

		/// <summary>
		/// Called to initialize the component.
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();

			Codec.SendCommand("callstate register");
			Codec.SendCommand("notify callstatus");
			Codec.SendCommand("notify linestatus");

			Codec.SendCommand("callinfo all");
			Codec.SendCommand("callstate get");
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
		/// Dials a video call number of type h323.
		/// </summary>
		/// <param name="number"></param>
		public void DialAuto(string number)
		{
			if (number == null)
				throw new ArgumentNullException("number");

			number = StringUtils.Enquote(number);

			Codec.SendCommand("dial auto auto {0}", number);
			Codec.Log(eSeverity.Informational, "Dialing auto number {0}", StringUtils.ToRepresentation(number));
		}

		/// <summary>
		/// Dials a video call number of type h323.
		/// Use dial manual when you do not want automatic call rollover or when
		/// the dialstring might not convey the intended transport.
		/// </summary>
		/// <param name="number"></param>
		public void DialManual(string number)
		{
			if (number == null)
				throw new ArgumentNullException("number");

			number = StringUtils.Enquote(number);

			Codec.SendCommand("dial manual auto {0}", number);
			Codec.Log(eSeverity.Informational, "Dialing manual number {0}", StringUtils.ToRepresentation(number));
		}

		/// <summary>
		/// Dials a video call number.
		/// Use dial manual when you do not want automatic call rollover or when
		/// the dialstring might not convey the intended transport.
		/// </summary>
		/// <param name="number"></param>
		/// <param name="type"></param>
		public void DialManual(string number, eDialType type)
		{
			if (number == null)
				throw new ArgumentNullException("number");

			number = StringUtils.Enquote(number);

			string typeName = s_TypeNames.GetValue(type);

			Codec.SendCommand("dial manual auto {0} {1}", number, typeName);
			Codec.Log(eSeverity.Informational, "Dialing manual number {0} type {1}", StringUtils.ToRepresentation(number), typeName);
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

			string protocolValues = StringUtils.ArrayFormat(EnumUtils.GetValues<eDialProtocol>());
			string typeValues = StringUtils.ArrayFormat(EnumUtils.GetValues<eDialType>());

			yield return new GenericConsoleCommand<string>("DialAuto", "DialAuto <NUMBER>", n => DialAuto(n));
			yield return new GenericConsoleCommand<string>("DialManual", "DialManual <NUMBER>", n => DialManual(n));

			string dialManualTypeHelp = string.Format("DialManualType <NUMBER> <{0}>", typeValues);
			yield return new GenericConsoleCommand<string, eDialType>("DialManualType", dialManualTypeHelp, (n, t) => DialManual(n, t));

			string dialPhoneHelp = string.Format("DialPhone <{0}> <NUMBER>", protocolValues);
			yield return new GenericConsoleCommand<eDialProtocol, string>("DialPhone", dialPhoneHelp, (p, n) => DialPhone(p, n));
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
