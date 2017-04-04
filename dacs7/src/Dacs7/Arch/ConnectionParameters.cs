using Dacs7.Domain;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Dacs7
{
    internal class ConnectionParameters
    {
        #region Defaults
        public const string DefaultIp = "127.0.0.1";
        public const int DefaultPort = 102;
        public const int DefaultRack = 0;
        public const int DefaultSlot = 2;
        #endregion



        private readonly Dictionary<string,Tuple<bool,Func<ConnectionParameters,string, bool>>> _validationDictionary = new Dictionary<string, Tuple<bool,Func<ConnectionParameters,string, bool>>>
        {   // parameter name,                          is necessary, validator
            {"Data Source", new Tuple<bool,Func<ConnectionParameters,string, bool>>(true, ValidateDataSource) },
            {"Connection Type", new Tuple<bool,Func<ConnectionParameters,string, bool>>(false, ValidateConnectionType) }
        };

        private readonly Dictionary<string,object> _config = new Dictionary<string, object>();

        public ConnectionParameters(string connectionString)
        {
            foreach (var connEntry in ConvertConnectionStringToDict(connectionString))
            {
                if (_validationDictionary.TryGetValue(connEntry.Key, out Tuple<bool, Func<ConnectionParameters, string, bool>> valEntry))
                {
                    if (!valEntry.Item2(this, connEntry.Value) && valEntry.Item1)
                        throw new ArgumentException(string.Format("Invalid Connection string on parameter {0}", connEntry.Key));
                }
                else
                    SetParameter(connEntry.Key, connEntry.Value);
            }
        }


        public void SetParameter<T>(string name, T value)
        {
            _config[name] = value;
        }

        public T GetParameter<T>(string name, T defaultValue)
        {
            if (_config.ContainsKey(name))
            {
                var obj = _config[name];
                var targetType = typeof (T);
                if (obj.GetType() != targetType)
                {
                    obj = Convert.ChangeType(obj, targetType);
                    _config[name] = obj;
                }
                return (T)obj;
            }
            return defaultValue;
        }


        #region Validation
        private static bool ValidateDataSource(ConnectionParameters parameters,string value)
        {
            var parts = value.Split(',');
            if (parts.Length > 0)
            {
                var ipAndPort = parts[0].Split(':');
                parameters.SetParameter("Ip",ipAndPort.Length > 0 ? ipAndPort[0] : DefaultIp);
                parameters.SetParameter("Port", ipAndPort.Length > 1 ? Int32.Parse(ipAndPort[1]) : DefaultPort);
            }
            parameters.SetParameter("Rack", parts.Length > 1 ? Int32.Parse(parts[1]) : DefaultRack);
            parameters.SetParameter("Slot", parts.Length > 2 ? Int32.Parse(parts[2]) : DefaultSlot);
            return true;
        }

        private static bool ValidateConnectionType(ConnectionParameters parameters, string value)
        {
            if (Enum.TryParse(value, out PlcConnectionType conType))
            {
                parameters.SetParameter("Connection Type", conType);
                return true;
            }
            return false;
        }

        #endregion

        private static Dictionary<string, string> ConvertConnectionStringToDict(string connectionString)
        {
            return connectionString.Split(';')
                .Select(part => part.Split('='))
                .Where(values => values.Length > 1)
                .ToDictionary(values => values[0].Trim(), values => values[1].Trim());
        }
    }
}
