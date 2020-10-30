// <copyright>
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>

namespace System
{
    using System.Diagnostics.Tracing;

    internal sealed partial class TelemetryEventSource
    {
        /// <summary>
        /// Messaging telemetry provider name.
        /// </summary>
        private const string MessagingProviderName = "Microsoft.DOTNET.System.Messaging";
        
        /// <summary>
        /// ID for MessageQueue event
        /// </summary>
        private const int MessageQueueEventId = 1;
        
        /// <summary>  
        /// Constructs a new instance of the TelemetryEventSource class with the  
        /// specified name. Sets the EtwSelfDescribingEventFormat option and joins the  
        /// MicrosoftTelemetry group.  
        /// </summary>  
        internal TelemetryEventSource() :
            this(MessagingProviderName)
        { 
        }

        /// <summary>
        /// Event fired if MessageQueue is used.
        /// </summary>
        [Event(MessageQueueEventId, Keywords = MeasuresKeyword)]  
        internal void MessageQueue() 
        { 
            WriteEvent(MessageQueueEventId); 
        }  
    }
}
