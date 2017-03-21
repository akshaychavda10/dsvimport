using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Configuration;


namespace DSVImportFile
{
    class ConfigHelper
    {
        public static string GetConfigSetting(string configKey)
        {
            if (ConfigurationManager.AppSettings[Environment.MachineName + "." + configKey] == null)
            {
                if (ConfigurationManager.AppSettings[configKey] == null)
                {
                    return "";
                }
                else
                {
                    return ConfigurationManager.AppSettings[configKey];
                }
            }
            else
            {
                return ConfigurationManager.AppSettings[Environment.MachineName + "." + configKey];
            }

        }
    }
}
