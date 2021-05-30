using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Reflection;
using System.Xml;

namespace OpenRiaServices.Client.HttpDomainClient
{
    /// <summary>
    /// This file is based on the OpenRiaServices.Client/Hosting.WebHttpQueryStringConverter 
    /// </summary>
    static class QueryStringConverter
    {
        public static string ConvertValueToString(object parameter, Type parameterType)
        {
            if (parameterType == null)
            {
                throw new ArgumentNullException("parameterType");
            }
            if (parameterType.GetTypeInfo().IsValueType && parameter == null)
            {
                throw new ArgumentNullException("parameter");
            }

            if (Mono.System.ServiceModel.Dispatcher.QueryStringConverter.CanConvert(parameterType))
            {
                return Mono.System.ServiceModel.Dispatcher.QueryStringConverter.ConvertValueToString(parameter, parameterType);
            }
            using (MemoryStream ms = new MemoryStream())
            {
                //                new DataContractJsonSerializer(parameterType).WriteObject(ms, parameter);               
                //byte[] result = ms.ToArray();
                //string value = Encoding.UTF8.GetString(result, 0, result.Length);

                // TODO: JsonSerializerSettings , and ensure it is correctly configured
                string value = Newtonsoft.Json.JsonConvert.SerializeObject(parameter, parameterType, new Newtonsoft.Json.JsonSerializerSettings()
                { });

                return Uri.EscapeDataString(value);
            }
        }
    }
}
