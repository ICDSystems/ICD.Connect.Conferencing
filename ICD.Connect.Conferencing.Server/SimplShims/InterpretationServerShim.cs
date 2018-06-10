using System;
using System.Collections.Generic;
using System.Linq;
using ICD.Common.Properties;
using ICD.Common.Utils.Extensions;
using ICD.Connect.Conferencing.Server.Devices.Server;
using ICD.Connect.Settings.SPlusShims;

namespace ICD.Connect.Conferencing.Server.SimplShims
{
	public sealed class InterpretationServerShim : AbstractSPlusOriginatorShim<IInterpretationServerDevice>
	{
		public event EventHandler<InterpretationStateEventArgs> OnInterpretationStateChanged;
		public event EventHandler<InterpretationRoomInfoArgs> OnRoomAdded;

		[PublicAPI("S+")]
		public void BeginInterpretation(ushort roomId, ushort boothId)
		{
			Originator.BeginInterpretation(roomId, boothId);
		}

		[PublicAPI("S+")]
		public void EndInterpretation(ushort roomId, ushort boothId)
		{
			Originator.EndInterpretation(roomId, boothId);
		}

		[PublicAPI("S+")]
		public ushort[] GetAvailableBoothIds()
		{
			return Originator.GetAvailableBoothIds().ToArray();
		}

		[PublicAPI("S+")]
		public ushort[] GetAvailableRoomIds()
		{
			int[] available = Originator.GetAvailableRoomIds().ToArray();
			IEnumerable<ushort> availableAsUShorts = available.Where(value => value >= ushort.MinValue && value <= ushort.MaxValue)
															  .Select(value => (ushort)value);

			return availableAsUShorts.ToArray();
		}

		[PublicAPI("S+")]
		public string GetRoomName(int roomId)
		{
			return Originator.GetRoomName(roomId);
		}

		[PublicAPI("S+")]
		public string GetRoomPrefix(int roomId)
		{
			return Originator.GetRoomPrefix(roomId);
		}

		[PublicAPI("S+")]
		public ushort GetBoothId(int roomId)
		{
			return Originator.GetBoothId(roomId);
		}

		[PublicAPI("S+")]
		public ushort GetRoomExists(int roomId)
		{
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
		}

		private void OriginatorOnInterpretationStateChanged(object sender, InterpretationStateEventArgs args)
		{
			OnInterpretationStateChanged.Raise(this, new InterpretationStateEventArgs(args.RoomId, args.BoothId, args.Active));
		}

		private void OriginatorOnRoomAdded(object sender, InterpretationRoomInfoArgs args)
		{
			OnRoomAdded.Raise(this, new InterpretationRoomInfoArgs(args.RoomId, args.RoomName, args.RoomPrefix));
		}	
	}
}