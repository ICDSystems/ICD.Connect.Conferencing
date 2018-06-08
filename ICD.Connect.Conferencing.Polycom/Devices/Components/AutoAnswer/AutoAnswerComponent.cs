using System;
using System.Collections.Generic;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.API.Commands;
using ICD.Connect.API.Nodes;

namespace ICD.Connect.Conferencing.Polycom.Devices.Components.AutoAnswer
{
	public sealed class AutoAnswerComponent : AbstractPolycomComponent
	{
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
			Codec.SendCommand("autoanswer {0}", mode.ToString().ToLower());
			Codec.Log(eSeverity.Informational, "Setting Auto Answer {0}", mode);
		}

		/// <summary>
		/// Called to initialize the component.
		/// </summary>
		protected override void Initialize()
		{
			base.Initialize();

			Codec.SendCommand("autoanswer register");
			Codec.SendCommand("autoanswer get");
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
