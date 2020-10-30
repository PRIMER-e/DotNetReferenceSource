using System;
using System.Diagnostics.Tracing;
using MS.Internal;
using MS.Internal.Telemetry;
using MS.Internal.Telemetry.PresentationFramework;

namespace MS.Internal.Telemetry.PresentationFramework
{
    /// <summary>
    /// Trace logger for DataGrid control
    /// </summary>
    internal static class DataGridTraceLogger
    {
        /// <summary>
        /// Logs datagrid usage details 
        /// </summary>
        internal static void LogUsageDetails()
        {
            EventSource logger = TraceLoggingProvider.GetProvider();
            logger?.Write(DataGridUsed, TelemetryEventSource.MeasuresOptions(), new
            {
                AppUsesDataGrid = true
            });
        }

        /// <summary>
        /// Event name for logging datagrid usage details
        /// </summary>
        private static readonly string DataGridUsed = "DataGridUsed";
    }
}
