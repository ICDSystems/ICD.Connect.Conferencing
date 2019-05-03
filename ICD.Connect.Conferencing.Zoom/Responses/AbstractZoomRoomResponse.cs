﻿using System;
using ICD.Common.Properties;
using Newtonsoft.Json;

namespace ICD.Connect.Conferencing.Zoom.Responses
{
	public abstract class AbstractZoomRoomResponse
	{
		[CanBeNull]
		public static AbstractZoomRoomResponse DeserializeResponse(string data, out AttributeKey key)
		{
			if (!AttributeKey.TryParse(data, out key))
				return null;

			// Find concrete type that matches the json values
			Type responseType = key.GetResponseType();
			return responseType == null
				? null
				: JsonConvert.DeserializeObject(data, responseType) as AbstractZoomRoomResponse;
		}
	}
}