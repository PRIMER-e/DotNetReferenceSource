using System;
using System.Diagnostics.Tracing;
using MS.Internal;
using MS.Internal.Telemetry;
using MS.Internal.Telemetry.PresentationFramework;

namespace MS.Internal.Telemetry.PresentationFramework
{
    /// <summary>
    /// Trace logger for Flow Document control
    /// </summary>
    internal static class FlowDocumentTraceLogger
    {
        /// <summary>
        /// Logs flow document usage details 
        /// </summary>
        internal static void LogUsageDetails()
        {
            EventSource logger = TraceLoggingProvider.GetProvider();
            logger?.Write(FlowDocumentUsed, TelemetryEventSource.MeasuresOptions(), new
            {
                AppUsesFlowDocument = true
            });
        }

        /// <summary>
        /// Event name for logging flow document usage details
        /// </summary>
        private static readonly string FlowDocumentUsed = "FlowDocumentUsed";
    }
}
