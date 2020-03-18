﻿using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Umbraco.Composing;
using Umbraco.Core.IO;
using Umbraco.Core.Logging;

namespace Umbraco.Core.Configuration
{
    public class ConnectionStrings : IConnectionStrings
    {

        public ConfigConnectionString this[string key]
        {
            get
            {
                var settings = ConfigurationManager.ConnectionStrings[key];
                if (settings == null) return null;
                return new ConfigConnectionString(settings.ConnectionString, settings.ProviderName, settings.Name);
            }
        }

        public void RemoveConnectionString(string key, IIOHelper ioHelper)
        {
            var fileName = ioHelper.MapPath(string.Format("{0}/web.config", ioHelper.Root));
            var xml = XDocument.Load(fileName, LoadOptions.PreserveWhitespace);

            var appSettings = xml.Root.DescendantsAndSelf("appSettings").Single();
            var setting = appSettings.Descendants("add").FirstOrDefault(s => s.Attribute("key").Value == key);

            if (setting != null)
            {
                setting.Remove();
                xml.Save(fileName, SaveOptions.DisableFormatting);
                ConfigurationManager.RefreshSection("appSettings");
            }
            var settings = ConfigurationManager.ConnectionStrings[key];
        }

         /// <summary>
        /// Saves the connection string as a proper .net connection string in web.config.
        /// </summary>
        /// <remarks>Saves the ConnectionString in the very nasty 'medium trust'-supportive way.</remarks>
        /// <param name="connectionString">The connection string.</param>
        /// <param name="providerName">The provider name.</param>
         public void SaveConnectionString(string connectionString, string providerName, IIOHelper ioHelper)
        {

            if (connectionString == null) throw new ArgumentNullException(nameof(connectionString));
            if (string.IsNullOrWhiteSpace(connectionString)) throw new ArgumentException("Value can't be empty or consist only of white-space characters.", nameof(connectionString));
            if (providerName == null) throw new ArgumentNullException(nameof(providerName));
            if (string.IsNullOrWhiteSpace(providerName)) throw new ArgumentException("Value can't be empty or consist only of white-space characters.", nameof(providerName));


            var fileSource = "web.config";
            var fileName = ioHelper.MapPath(ioHelper.Root +"/" + fileSource);

            var xml = XDocument.Load(fileName, LoadOptions.PreserveWhitespace);
            if (xml.Root == null) throw new Exception($"Invalid {fileSource} file (no root).");

            var connectionStrings = xml.Root.DescendantsAndSelf("connectionStrings").FirstOrDefault();
            if (connectionStrings == null) throw new Exception($"Invalid {fileSource} file (no connection strings).");

            // handle configSource
            var configSourceAttribute = connectionStrings.Attribute("configSource");
            if (configSourceAttribute != null)
            {
                fileSource = configSourceAttribute.Value;
                fileName = ioHelper.MapPath(ioHelper.Root + "/" + fileSource);

                if (!File.Exists(fileName))
                    throw new Exception($"Invalid configSource \"{fileSource}\" (no such file).");

                xml = XDocument.Load(fileName, LoadOptions.PreserveWhitespace);
                if (xml.Root == null) throw new Exception($"Invalid {fileSource} file (no root).");

                connectionStrings = xml.Root.DescendantsAndSelf("connectionStrings").FirstOrDefault();
                if (connectionStrings == null) throw new Exception($"Invalid {fileSource} file (no connection strings).");
            }

            // create or update connection string
            var setting = connectionStrings.Descendants("add").FirstOrDefault(s => s.Attribute("name")?.Value == Constants.System.UmbracoConnectionName);
            if (setting == null)
            {
                connectionStrings.Add(new XElement("add",
                    new XAttribute("name", Constants.System.UmbracoConnectionName),
                    new XAttribute("connectionString", connectionString),
                    new XAttribute("providerName", providerName)));
            }
            else
            {
                AddOrUpdateAttribute(setting, "connectionString", connectionString);
                AddOrUpdateAttribute(setting, "providerName", providerName);
            }

            // save
            Current.Logger.Info<ConnectionStrings>("Saving connection string to {ConfigFile}.", fileSource);
            xml.Save(fileName, SaveOptions.DisableFormatting);
            Current.Logger.Info<ConnectionStrings>("Saved connection string to {ConfigFile}.", fileSource);
        }

         private static void AddOrUpdateAttribute(XElement element, string name, string value)
         {
             var attribute = element.Attribute(name);
             if (attribute == null)
                 element.Add(new XAttribute(name, value));
             else
                 attribute.Value = value;
         }

    }
}
