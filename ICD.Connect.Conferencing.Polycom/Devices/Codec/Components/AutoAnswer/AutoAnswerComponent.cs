using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Utils;
using ICD.Common.Utils.Collections;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Conferencing.Polycom.Devices.Codec.Components.AutoAnswer
{
	public sealed class AutoAnswerComponent : AbstractPolycomComponent
	{
		private static readonly BiDictionary<eAutoAnswer, string> s_AutoAnswerNames =
			new BiDictionary<eAutoAnswer, string>
			{
				{eAutoAnswer.No, "no"},
				{eAutoAnswer.Yes, "yes"},
				{eAutoAnswer.DoNotDisturb, "donotdisturb"}
			};

		/// <summary>
		/// Raised when the auto-answer mode changes.
		/// </summary>
		public event EventHandler<PolycomAutoAnswerEventArgs> OnAutoAnswerChanged;

		private eAutoAnswer m_AutoAnswer;

		/// <summary>
		/// Gets the auto answer mode.
		/// </summary>
		public eAutoAnswer AutoAnswer
		{
			get { return m_AutoAnswer; }
			private set
			{
				if (value == m_AutoAnswer)
					return;

				m_AutoAnswer = value;

				Codec.Log(eSeverity.Informational, "AutoAnswer set to {0}", m_AutoAnswer);

				OnAutoAnswerChanged.Raise(this, new PolycomAutoAnswerEventArgs(m_AutoAnswer));
			}
		}

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="codec"></param>
		public AutoAnswerComponent(PolycomGroupSeriesDevice codec)
			: base(codec)
		{
			Subscribe(Codec);

			Codec.RegisterFeedback("autoanswer", HandleAutoAnswerState);

			if (Codec.Initialized)
				Initialize();
		}

		/// <summary>
		/// Release resources.
		/// </summary>
		/// <param name="disposing"></param>
		protected override void Dispose(bool disposing)
		{
			OnAutoAnswerChanged = null;

			base.Dispose(disposing);
		}

		/// <summary>
		/// Sets the auto-answer mode.
		/// </summary>
		/// <param name="mode"></param>
		public void SetAutoAnswer(eAutoAnswer mode)
		{
			string name = s_AutoAnswerNames.GetValue(mode);

			Codec.SendCommand("autoanswer {0}", name);
			Codec.Log(eSeverity.Informational, "Setting Auto Answer {0}", name);
		}

		/// <summary>
		/// Called to initialize the component.
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();

			Codec.SendCommand("autoanswer get");
		}

		/// <summary>
		/// Called when we get an autoanswer feedback message.
		/// </summary>
		/// <param name="data"></param>
		private void HandleAutoAnswerState(string data)
		{
			string[] split = data.Split();
			string result = split.Skip(1).FirstOrDefault();
			if (result == null)
				return;

			eAutoAnswer mode;
			if (s_AutoAnswerNames.TryGetKey(result, out mode))
				AutoAnswer = mode;
		}

		#region Console

		/// <summary>
		/// Gets the child console commands.
		/// </summary>
		/// <returns></returns>
		public override IEnumerable<IConsoleCommand> GetConsoleCommands()
		{
			foreach (IConsoleCommand command in GetBaseConsoleCommands())
				yield return command;

			string autoAnswerHelp = string.Format("SetAutoAnswer <{0}>", StringUtils.ArrayFormat(EnumUtils.GetValues<eAutoAnswer>()));
			yield return new GenericConsoleCommand<eAutoAnswer>("SetAutoAnswer", autoAnswerHelp, a => SetAutoAnswer(a));
		}

		/// <summary>
		/// Shim to avoid "unverifiable code" warning.
		/// </summary>
		/// <returns></returns>
		private IEnumerable<IConsoleCommand> GetBaseConsoleCommands()
		{
			return base.GetConsoleCommands();
		}

		/// <summary>
		/// Calls the delegate for each console status item.
		/// </summary>
		/// <param name="addRow"></param>
		public override void BuildConsoleStatus(AddStatusRowDelegate addRow)
		{
			base.BuildConsoleStatus(addRow);

			addRow("AutoAnswer", AutoAnswer);
		}

		#endregion
	}
}
