using System;
using System.Diagnostics.Tracing;
using MS.Internal;
using MS.Internal.Telemetry;
using MS.Internal.Telemetry.PresentationFramework;

namespace MS.Internal.Telemetry.PresentationFramework
{
    /// <summary>
    /// Trace logger for ScrollViewer control
    /// </summary>
    internal static class ScrollViewerTraceLogger
    {
        /// <summary>
        /// Logs scroll viewer usage details 
        /// </summary>
        internal static void LogUsageDetails()
        {
            EventSource logger = TraceLoggingProvider.GetProvider();
            logger?.Write(ScrollViewerUsed, TelemetryEventSource.MeasuresOptions(), new
            {
                AppUsesScrollViewer = true
            });
        }

        /// <summary>
        /// Event name for logging scroll viewer usage details
        /// </summary>
        private static readonly string ScrollViewerUsed = "ScrollViewerUsed";
    }
}
