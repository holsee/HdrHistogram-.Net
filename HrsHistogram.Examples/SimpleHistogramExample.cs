namespace HrsHistogram.Examples
{
    using System;
    using System.Diagnostics;
    using System.Net.Sockets;

    using HdrHistogram;

    /// <summary>
    /// A simple example of using HdrHistogram: run for 20 seconds collecting the
    /// time it takes to perform a simple Datagram Socket create/close operation,
    /// and report a histogram of the times at the end.
    /// </summary>
    public static class SimpleHistogramExample
    {
        // A Histogram covering the range from 1 nsec to 1 hour with 3 decimal point resolution:
        #region Constants

        /// <summary>
        /// The run time msec.
        /// </summary>
        private const long RunTimeMsec = 20000;

        /// <summary>
        /// The warmup time msec.
        /// </summary>
        private const long WarmupTimeMsec = 5000;

        #endregion

        #region Static Fields

        /// <summary>
        /// The histogram.
        /// </summary>
        private static readonly Histogram Histogram = new Histogram(3600000000000L, 3);

        #endregion

        #region Public Methods and Operators
        
        /// <summary>
        /// The main.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        public static void Run(string[] args)
        {
            var sw = new Stopwatch();
            sw.Start();

            long startTime = sw.ElapsedMilliseconds;
            long now;

            do
            {
                recordTimeToCreateAndCloseDatagramSocket();
                now = sw.ElapsedMilliseconds;
            }
            while (now - startTime < WarmupTimeMsec);

            Histogram.Reset();
            
            sw.Reset();
            startTime = sw.ElapsedMilliseconds;
            do
            {
                recordTimeToCreateAndCloseDatagramSocket();
                now = sw.ElapsedMilliseconds;
            }
            while (now - startTime < RunTimeMsec);

            Console.WriteLine("Recorded latencies [in usec] for Create+Close of a DatagramSocket:");

            Histogram.GetHistogramData().OutputPercentileDistribution(Console.Out, 1000.0);
        }

        #endregion

        #region Methods

        /// <summary>
        /// The record time to create and close datagram socket.
        /// </summary>
        private static void recordTimeToCreateAndCloseDatagramSocket()
        {
            var sw = new Stopwatch();
            sw.Start();

            using (var socket = new Socket(SocketType.Dgram, ProtocolType.IP))
            {
            }

            sw.Stop();

            Histogram.RecordValue(sw.ElapsedTicks);
        }

        #endregion
    }
}