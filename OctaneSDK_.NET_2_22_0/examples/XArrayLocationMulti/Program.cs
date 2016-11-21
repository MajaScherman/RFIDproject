////////////////////////////////////////////////////////////////////////////////
//
//    Multiple xArray Location
//
////////////////////////////////////////////////////////////////////////////////

using System;
using Impinj.OctaneSdk;
using System.Collections.Generic;

namespace OctaneSdkExamples
{
    class Program
    {

        // This example does a weighted average with just 2 xArrays.  You can additional xArrays by adding xArray elements to the
        // 2 xArrays defined in xArrays[] below:

        //                                       Reader      HeightCM, FacXcm, FacYcm, Orient, Session
        XArray[] xArrays = {   new XArray("xarray-XX-XX-XX",   300,       0,       0,      0,     2),      
                               new XArray("xarray-XX-XX-XX",   300,       0,     400,      0,     3) };

        ReaderMode READER_MODE = ReaderMode.AutoSetDenseReaderDeepScan;   // Recommended moded for xArray
        const int COMPUTE_WINDOW_SEC = 30;

        // Use dictionaries to store Confidence, WeightedX and WeightedY and Cycle Lengths.
        Dictionary<string, double> CycleLengths = new Dictionary<string, double>();
        Dictionary<string, TagReadInfo> TagReadInfo = new Dictionary<string, TagReadInfo>();

        public Program()
        {
            ImpinjReader[] readers = new ImpinjReader[xArrays.Length];
            for (int i = 0; i < readers.Length; i++)
            {
                readers[i] = new ImpinjReader();
                LaunchXArray(readers[i], xArrays[i]);
            }
            // Wait for the user to press enter.
            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();
            for (int i = 0; i < xArrays.Length; i++)
            {
                CloseXArray(readers[i]);
            }
        }
        public void LaunchXArray(ImpinjReader reader, XArray xArray)
        {
            try
            {
                // Connect to the reader.
                // Change the ReaderHostname constant in SolutionConstants.cs 
                // to the IP address or hostname of your reader.
                reader.Connect(xArray.Hostname);

                // Assign the LocationReported event handler.
                // This specifies which method to call
                // when a location report is available.
                reader.LocationReported += OnLocationReported;
                // Don't forget to define diagnostic method
                reader.DiagnosticsReported += OnDiagnosticsReported;

                // Apply the newly modified settings.
                reader.ApplySettings(GetPrepareSettings(reader, xArray));

                // Start the reader
                reader.Start();
            }
            catch (OctaneSdkException e)
            {
                // Handle Octane SDK errors.
                Console.WriteLine("Octane SDK exception: {0}", e.Message);
            }
            catch (Exception e)
            {
                // Handle other .NET errors.
                Console.WriteLine("Exception : {0}", e.Message);
            }
        }
        public Settings GetPrepareSettings(ImpinjReader reader, XArray xArray)
        {
            // Get the default settings
            // We'll use these as a starting point
            // and then modify the settings we're 
            // interested in.
            Settings settings = reader.QueryDefaultSettings();

            // Put the xArray into location Role
            settings.XArray.Mode = XArrayMode.Location;

            // Enable all three report types
            settings.XArray.Location.EntryReportEnabled = true;
            settings.XArray.Location.UpdateReportEnabled = true;
            settings.XArray.Location.ExitReportEnabled = true;
            // Enable Diagnostic reports here, soon to be deprecated
            settings.XArray.Location.DiagnosticReportEnabled = true;

            // Set xArray placement parameters
            // The mounting height of the xArray, in centimeters
            settings.XArray.Placement.HeightCm = xArray.Height;
            settings.XArray.Placement.FacilityXLocationCm = xArray.FacilityXcm;
            settings.XArray.Placement.FacilityYLocationCm = xArray.FacilityYcm;
            settings.XArray.Placement.OrientationDegrees = xArray.Orientation;

            // Compute Window and Gen2 Settings
            settings.XArray.Location.ComputeWindowSeconds = COMPUTE_WINDOW_SEC;
            settings.ReaderMode = READER_MODE;
            settings.Session = xArray.Session;
            settings.XArray.Location.TagAgeIntervalSeconds = 60;

            // Specify how often we want to receive location reports
            settings.XArray.Location.UpdateIntervalSeconds = 10;

            // Exercise: Filter out extraneous tags
        
            settings.Filters.TagFilter1.TagMask = "9999";
            settings.Filters.TagFilter1.MemoryBank = MemoryBank.Epc;
            settings.Filters.TagFilter1.BitPointer = BitPointers.Epc;
            settings.Filters.TagFilter1.BitCount = 16;
            settings.Filters.Mode = TagFilterMode.OnlyFilter1;  // Use just one filter
         
            return settings;
        }
        // This event handler will be called when a location report is ready.
        void OnLocationReported(ImpinjReader reader, LocationReport report)
        {
            string EpcStr = report.Epc.ToHexString();

            // Compute confidence. Make sure that the first cycle report came in before computing the Weighted averages.
            if (!CycleLengths.ContainsKey(reader.Address) || CycleLengths[reader.Address] == 0)
                return;

            // If first time
            if (!TagReadInfo.ContainsKey(EpcStr))
                TagReadInfo.Add(EpcStr, new TagReadInfo());

            double Mult = Math.Floor((double)COMPUTE_WINDOW_SEC * 1000000 / CycleLengths[reader.Address]);  
            if (Mult == 0) Mult=1;
            double Confidence = report.ConfidenceFactors.ReadCount / Mult;
            Console.WriteLine(reader.Address + "  " + EpcStr + " x=" + report.LocationXCm + " y=" + report.LocationYCm + " conf=" + Confidence);
            // Weighted X
            double WgtX = Confidence * report.LocationXCm;
            TagReadInfo[EpcStr].WeightedX += WgtX;

            // Weighted Y
            double WgtY = Confidence * report.LocationYCm;
            TagReadInfo[EpcStr].WeightedY += WgtY;
            
            // TagReadCounts 
            TagReadInfo[EpcStr].Confidence += Confidence;

            // Pick a reader to key off the Averaging calculation
            // Let's use the last one.
            if (reader.Address.Equals(xArrays[xArrays.Length - 1].Hostname))
            {
                Console.Write("Weighted: "+EpcStr);
                if (TagReadInfo[EpcStr].Confidence != 0)
                {
                    Console.Write(" x=" + Math.Floor(TagReadInfo[EpcStr].WeightedX / TagReadInfo[EpcStr].Confidence));
                    Console.WriteLine(" y=" + Math.Floor(TagReadInfo[EpcStr].WeightedY / TagReadInfo[EpcStr].Confidence));
                }
                else
                {
                    Console.WriteLine("Invalid Read. Confidence is 0");
                }
                // Reinitialize variables
                TagReadInfo[EpcStr].WeightedX = 0;
                TagReadInfo[EpcStr].WeightedY = 0;
                TagReadInfo[EpcStr].Confidence = 0;
            }
            
        }
        private void OnDiagnosticsReported(ImpinjReader reader, DiagnosticReport report)
        {
            uint[] reportMetricsList = report.Metrics.ToArray();
            // Warning!!! Accessing diagnostic codes will not be supported in future releases.
            if (reportMetricsList[0] == 100)  // End of Cycle
            {
                Console.WriteLine("xArray=" + reader.Address+"  CycleTime=" + (int)reportMetricsList[1]/1000+"ms");
                // Store latest Cycle time
                if (CycleLengths.ContainsKey(reader.Address))
                    CycleLengths[reader.Address] = (int)reportMetricsList[1];
                else
                    CycleLengths.Add(reader.Address, (int)reportMetricsList[1]);
            }
        }
        private void CloseXArray(ImpinjReader reader)
        {
            // Apply the default settings before exiting.
            reader.ApplyDefaultSettings();

            // Disconnect from the reader.
            reader.Disconnect();
        }
        static void Main(string[] args)
        {
            new Program();
        }
    }
    class XArray 
    {
        public XArray(String Hostname, ushort Height, int FacilityXcm, int FacilityYcm, short Orientation, ushort Session)
        {
            this.Hostname = Hostname;
            this.Height = Height;
            this.FacilityXcm = FacilityXcm;
            this.FacilityYcm = FacilityYcm;
            this.Session = Session;
        }
        public string Hostname   { get; set; }
        public ushort Height     { get; set; }
        public int FacilityXcm   { get; set; }
        public int FacilityYcm   { get; set; }
        public short Orientation { get; set; }
        public ushort Session    { get; set; }
    }
    class TagReadInfo
    {
        public TagReadInfo()
        {
            this.Confidence = 0;
            this.WeightedX = 0;
            this.WeightedY = 0;
        }
        public double Confidence { get; set; }
        public double WeightedX { get; set; }
        public double WeightedY { get; set; }
    }
}
