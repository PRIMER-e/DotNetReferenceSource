using System;
using System.Diagnostics.Tracing;
using MS.Internal;
using MS.Internal.Telemetry;
using MS.Internal.Telemetry.PresentationFramework;

namespace MS.Internal.Telemetry.PresentationFramework
{
    /// <summary>
    /// Trace logger for TreeView control
    /// </summary>
    internal static class TreeViewTraceLogger
    {
        /// <summary>
        /// Logs treeview usage details 
        /// </summary>
        internal static void LogUsageDetails()
        {
            EventSource logger = TraceLoggingProvider.GetProvider();
            logger?.Write(TreeViewUsed, TelemetryEventSource.MeasuresOptions(), new
            {
                AppUsesTreeView = true
            });
        }

        /// <summary>
        /// Event name for logging treeview usage details
        /// </summary>
        private static readonly string TreeViewUsed = "TreeViewUsed";
    }
}
