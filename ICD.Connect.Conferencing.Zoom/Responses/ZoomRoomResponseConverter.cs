using System;
using System.Linq;
using Crestron.SimplSharp.Reflection;
using ICD.Common.Utils;
using ICD.Common.Utils.Extensions;
using ICD.Common.Utils.Json;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	public sealed class ZoomRoomResponseConverter : AbstractGenericJsonConverter<AbstractZoomRoomResponse>
	{
		public override bool CanWrite { get { return false; } }

		public override void WriteJson(JsonWriter writer, AbstractZoomRoomResponse value, JsonSerializer serializer)
		{
			throw new NotImplementedException();
		}

		public override AbstractZoomRoomResponse ReadJson(JsonReader reader, AbstractZoomRoomResponse existingValue, JsonSerializer serializer)
		{
			JObject jObject = JObject.Load(reader);
			string responseKey = jObject["topKey"] != null ? jObject["topKey"].ToString() : null;
			if (responseKey == null)
				throw new FormatException("No topKey property found, can't determine type of response");

			var responseType = GetType().GetAssembly().GetTypes().SingleOrDefault(t => TypeAttributeMatchesKey(t, responseKey));
			
			return (AbstractZoomRoomResponse)serializer.Deserialize(new JTokenReader(jObject), responseType);
		}

#if SIMPLSHARP
		private bool TypeAttributeMatchesKey(CType type, string key)
#else
		private bool TypeAttributeMatchesKey(Type type, string key)
#endif
		{
			var attribute = type.GetCustomAttribute<ZoomRoomApiResponseAttribute>();
			return attribute != null && attribute.ResponseKey.Equals(key);
		}
	}
}