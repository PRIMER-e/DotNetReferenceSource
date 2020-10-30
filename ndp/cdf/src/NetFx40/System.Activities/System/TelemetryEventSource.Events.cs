// <copyright>
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace System
{
    using System.Diagnostics.Tracing;

    internal sealed partial class TelemetryEventSource
    {
        /// <summary>
        /// Workflow V2 telemetry provider name.
        /// </summary>
        private const string WfV2ProviderName = "Microsoft.DOTNET.WF.V2";
        
        /// <summary>
        /// ID for V2Runtime event
        /// </summary>
        private const int V2RuntimeEventId = 1;
        
        /// <summary>  
        /// Constructs a new instance of the TelemetryEventSource class with the  
        /// specified name. Sets the EtwSelfDescribingEventFormat option and joins the  
        /// MicrosoftTelemetry group.  
        /// </summary>  
        internal TelemetryEventSource() :
            this(WfV2ProviderName)
        { 
        }

        /// <summary>
        /// Event fired if WFV2 runtime is used.
        /// </summary>
        [Event(V2RuntimeEventId, Keywords = MeasuresKeyword)]  
        internal void V2Runtime() 
        { 
            WriteEvent(V2RuntimeEventId); 
        }  
    }
}
