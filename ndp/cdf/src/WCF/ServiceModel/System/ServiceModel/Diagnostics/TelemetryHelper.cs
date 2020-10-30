// <copyright>
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace System.ServiceModel
{
    using System;
    using System.Collections.Generic;
    using System.Runtime;
    using System.Security;
    using System.Security.Cryptography;
    using System.Security.Permissions;
    using System.ServiceModel.Activation;
    using System.ServiceModel.Description;
    using System.Threading;
    using Microsoft.Win32;

    /// <summary>
    /// Helper to get NetFx version details
    /// </summary>
    internal class TelemetryHelper
    {
        public string GetHostType()
        {
            return AspNetEnvironment.Enabled ? "IISHosted" : "SelfHosted";
        }

        public string GetEndpoints(ServiceDescription description)
        {
            string endpoints = string.Empty;
            if (description != null)
            {
                List<string> list = new List<string>();
                foreach (ServiceEndpoint endpoint in description.Endpoints)
                {
                    if (endpoint != null && endpoint.Binding != null)
                    {
                        list.Add(string.Concat(endpoint.Binding.Scheme, ":", endpoint.Binding.Name));
                    }
                }

                endpoints = string.Join(";", list);
            }

            return endpoints;
        }

        public string GetServiceId(ServiceDescription description)
        {
            // since ServiceDescription.ConfigurationName can contain customer service class name,
            // which can be treated as PII data, hash it before log into telemetry ETW. 
            // need non-randomized hashcode since we want to get the same hashcode with the same ConfigrationName
            return StringUtil.GetNonRandomizedHashCode(description.ConfigurationName).ToString();
        }
    }
}
