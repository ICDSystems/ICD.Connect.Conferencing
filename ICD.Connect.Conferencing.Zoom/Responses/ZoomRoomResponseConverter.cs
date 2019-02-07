//using System;
//using System.Collections.Generic;
//using ICD.Common.Utils.Timers;
//#if SIMPLSHARP
//using Crestron.SimplSharp.Reflection;
//#else
//using System.Reflection;
//#endif
//using ICD.Common.Utils.Extensions;
//using ICD.Common.Utils.Json;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;

//namespace ICD.Connect.Conferencing.Zoom.Responses
//{
//    public sealed class ZoomRoomResponseConverter : AbstractGenericJsonConverter<AbstractZoomRoomResponse>
//    {
//        /// <summary>
//        /// Key to the property in the json which stores where the actual response data is stored
//        /// </summary>
//        private const string RESPONSE_KEY = "topKey";

//        /// <summary>
//        /// Key to the property in the json that stores the type of response (zCommand, zConfiguration, zEvent, zStatus)
//        /// </summary>
//        private const string API_RESPONSE_TYPE = "type";

//        /// <summary>
//        /// Key to the property in the json that stores whether the response was synchronous to a command, or an async event
//        /// </summary>
//        private const string SYNCHRONOUS = "Sync";

//        private static readonly Dictionary<AttributeKey, Type> s_TypeDict;

//        public override bool CanWrite { get { return false; } }

//        /// <summary>
//        /// Static constructor.
//        /// </summary>
//        static ZoomRoomResponseConverter()
//        {
//            s_TypeDict = new Dictionary<AttributeKey, Type>();

//            foreach (
//#if SIMPLSHARP
//                CType
//#else
//                Type
//#endif
//                    type in typeof(ZoomRoomResponseConverter).GetAssembly().GetTypes())
//            {
//                foreach (ZoomRoomApiResponseAttribute attribute in type.GetCustomAttributes<ZoomRoomApiResponseAttribute>())
//                {
//                    AttributeKey key = new AttributeKey(attribute);
//                    s_TypeDict.Add(key, type);
//                }
//            }
//        }

//        protected override AbstractZoomRoomResponse Instantiate()
//        {
//            throw new NotImplementedException();
//        }

//        public override void WriteJson(JsonWriter writer, AbstractZoomRoomResponse value, JsonSerializer serializer)
//        {
//            throw new NotSupportedException();
//        }

//        /// <summary>
//        /// 
//        /// </summary>
//        /// <param name="reader"></param>
//        /// <param name="existingValue"></param>
//        /// <param name="serializer"></param>
//        /// <returns></returns>
//        public override AbstractZoomRoomResponse ReadJson(JsonReader reader, AbstractZoomRoomResponse existingValue, JsonSerializer serializer)
//        {
//            try
//            {
//                JObject jObject = IcdStopwatch.Profile(() => JObject.Load(reader), "JObject.Load");
//                string responseKey = jObject[RESPONSE_KEY].ToString();
//                eZoomRoomApiType apiResponseType = jObject[API_RESPONSE_TYPE].ToObject<eZoomRoomApiType>();
//                bool synchronous = jObject[SYNCHRONOUS].ToObject<bool>();

//                //AttributeKey key = new AttributeKey(responseKey, apiResponseType, synchronous);

//                // find concrete type that matches the json values
//                Type responseType;
//                //if (!s_TypeDict.TryGetValue(key, out responseType))
//                {
//                    return null;
//                }
//                // shitty zoom api sometimes sends a single object instead of array
//                if (responseType == typeof(ListParticipantsResponse) && jObject[responseKey].Type != JTokenType.Array)
//                {
//                    responseType = typeof(SingleParticipantResponse);
//                }

//                if (responseType != null)
//                {
//                    return (AbstractZoomRoomResponse)IcdStopwatch.Profile(() =>
//                    {
//                        var result = Crestron.SimplSharp.Reflection.Activator.CreateInstance(responseType);
//                        var response = (AbstractZoomRoomResponse) result;
//                        response.LoadFromJObject(jObject);
//                        return response;
//                        //serializer.Deserialize(new JTokenReader(jObject), responseType)
//                    }, "Deserialize Type");
//                }

//                return null;
//            }
//#if SIMPLSHARP
//            //Added to catch JsonExceptions in SimplSharp. JsonException causing build to fail.
//            catch (Exception)
//            {
//                return null;
//            }
//#else
//            catch (JsonException)
//            {
//                return null;
//            }
//#endif
//        }

		
//    }
//}
