using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Services.Logging;
using ICD.Connect.Conferencing.Server.Devices.Server;
using ICD.Connect.Settings.CrestronSPlus.SPlusShims;

namespace ICD.Connect.Conferencing.Server.SimplShims
{
	public sealed class InterpretationServerShim : AbstractSPlusOriginatorShim<IInterpretationServerDevice>
	{
		[PublicAPI("S+")]
		public event EventHandler<InterpretationStateEventArgs> OnInterpretationStateChanged;

		[PublicAPI("S+")]
		public event EventHandler<InterpretationRoomInfoArgs> OnRoomAdded;

		[PublicAPI("S+")]
		public void BeginInterpretation(ushort roomId, ushort boothId)
		{
			if (Originator == null)
			{
				Logger.Log(eSeverity.Error, "Originator Is Null, cannot begin interpretation.");
				return;
			}
			Originator.BeginInterpretation(roomId, boothId);
		}

		[PublicAPI("S+")]
		public void EndInterpretation(ushort roomId, ushort boothId)
		{
			if (Originator == null)
			{
				Logger.Log(eSeverity.Error, "Originator Is Null, cannot end interpretation.");
				return;
			}
			Originator.EndInterpretation(roomId, boothId);
		}

		[PublicAPI("S+")]
		public ushort[] GetAvailableBoothIds()
		{
			if (Originator == null)
			{
				Logger.Log(eSeverity.Error, "Originator Is Null, cannot get booth ids.");
				return new ushort[0];
			}
			return Originator.GetAvailableBoothIds().ToArray();
		}

		[PublicAPI("S+")]
		public ushort[] GetAvailableRoomIds()
		{
			if (Originator == null)
			{
				Logger.Log(eSeverity.Error, "Originator Is Null, cannot get room ids.");
				return new ushort[0];
			}
			int[] available = Originator.GetAvailableRoomIds().ToArray();
			IEnumerable<ushort> availableAsUShorts = available.Where(value => value >= ushort.MinValue && value <= ushort.MaxValue)
															  .Select(value => (ushort)value);

			return availableAsUShorts.ToArray();
		}

		[PublicAPI("S+")]
		public string GetRoomName(int roomId)
		{
			if (Originator == null)
			{
				Logger.Log(eSeverity.Error, "Originator Is Null, cannot get room name.");
				return string.Empty;
			}
			return Originator.GetRoomName(roomId);
		}

		[PublicAPI("S+")]
		public ushort GetBoothId(int roomId)
		{
			if (Originator == null)
			{
				Logger.Log(eSeverity.Error, "Originator Is Null, cannot get booth id.");
				return 0;
			}
			return Originator.GetBoothId(roomId);
		}

		[PublicAPI("S+")]
		public ushort GetRoomExists(int roomId)
		{
			if (Originator == null)
			{
				Logger.Log(eSeverity.Error, "Originator Is Null, cannot get room existance status.");
				return 0;
			}
			return Originator.GetRoomExists(roomId);
		}

		/// <summary>
		/// Subscribes to the originator events.
		/// </summary>
		/// <param name="originator"></param>
		protected override void Subscribe(IInterpretationServerDevice originator)
		{
			if (originator == null)
				return;

			base.Subscribe(originator);

			originator.OnInterpretationStateChanged += OriginatorOnInterpretationStateChanged;
			originator.OnRoomAdded += OriginatorOnRoomAdded;
		}

		/// <summary>
		/// Unsubscribes from the originator events.
		/// </summary>
		/// <param name="originator"></param>
		protected override void Unsubscribe(IInterpretationServerDevice originator)
		{
			if (originator == null)
				return;

			base.Unsubscribe(originator);
			originator.OnInterpretationStateChanged -= OriginatorOnInterpretationStateChanged;
			originator.OnRoomAdded -= OriginatorOnRoomAdded;

			base.Unsubscribe(originator);
		}

		private void OriginatorOnInterpretationStateChanged(object sender, InterpretationStateEventArgs args)
		{
			OnInterpretationStateChanged.Raise(this, new InterpretationStateEventArgs(args.RoomId, args.BoothId, args.Active));
		}

		private void OriginatorOnRoomAdded(object sender, InterpretationRoomInfoArgs args)
		{
			OnRoomAdded.Raise(this, new InterpretationRoomInfoArgs(args.RoomId, args.RoomName));
		}	
	}
}