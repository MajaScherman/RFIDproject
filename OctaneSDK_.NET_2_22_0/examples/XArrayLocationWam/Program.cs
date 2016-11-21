////////////////////////////////////////////////////////////////////////////////
//
//    xArray Location and Wam Example
//
////////////////////////////////////////////////////////////////////////////////

using System;
using Impinj.OctaneSdk;
using System.Collections.Generic;

namespace OctaneSdkExamples
{
    class Program
    {
        // Create an instance of the ImpinjReader class.
        static ImpinjReader reader = new ImpinjReader();
        // Shared between Both Roles
        ReaderMode READER_MODE = ReaderMode.AutoSetDenseReaderDeepScan;     // Recommended moded for xArray
        
        // WAM Role Stuff
        const SearchMode WAM_SEARCH_MODE         = SearchMode.SingleTarget;
        const int WAM_SESSION                    = 3;                          // With just one xArray could use either session 2 or 3
        // If just inventorying a few hundreds Monza tags you can use single target with suppression especially
        // when using a fast reader mode like ReaderMode.AutoSetDenseReaderDeepScan.
        // Use the next 2 commented out lines to define SingleTarget TagFocus.
     /* const SearchMode WAM_SEARCH_MODE         = SearchMode.TagFocus;  
        const int WAM_SESSION                    = 1; 
     */

        const int TAG_POPULATION_ESTIMATE       = 2;     // Use a small estimate when using a combination of 52 xArray beams and tag suppression.
                                                         // Typically the beam is only finding a small subset of the total number of tags.
        // Location Role Stuff
        const int XARRAY_HEIGHT                = 280;    // This is for a low ceiling
        const int COMPUTE_WINDOW               = 10;     // Fairly short compute window
        const int TAG_AGE_INTERVAL             = 60;
        const int UPDATE_INTERVAL              = 10;     // How often you receive updates in seconds
        const ushort LOCATION_SESSION          = 2;      // When using multiple xArrays use a combination of Session 2 and 3

        // Run Timing parameters. 
        // Note: Increase duration for larger tag populations
        const int WAM_ROLE_DURATION            = 30 * 1000;                   // Time in milliseconds to run WAM
        const int LOCATION_ROLE_DURATION       = COMPUTE_WINDOW *1000 +500;   // Get enough time to run one compute window
        const int INTERATIONS                  = 2;                           // WAM and Location Roles Interations
        const int SESSION_2_OR_3_PERSISTENCE = 120 * 1000;                    // If Using WAM Session 2 or 3 wait for tags to decay before restarting WAM Role
                                                                              // MR6 has long decay time so wait at least 2 minutes
                                                                              // Your tags may vary
        // Collect tags read and their counts per inventory round
        Dictionary<string, Tag>            WamTags = new Dictionary<string, Tag>();
        Dictionary<string, LocationReport> LocTags = new Dictionary<string, LocationReport>();

        public Program()
        {
            try
            {
                reader.Connect(SolutionConstants.ReaderHostname);

                Console.WriteLine("WAM with Location: " + SolutionConstants.ReaderHostname);

                for (int i = 0; i < INTERATIONS; i++)
                {
                    // WAM Role
                    Console.WriteLine("Running WAM. Please wait " + WAM_ROLE_DURATION / 1000 + " Sec." + " Session=" + WAM_SESSION + " Target=" + WAM_SEARCH_MODE);
                    SetupWamMode();
                    System.Threading.Thread.Sleep(WAM_ROLE_DURATION);
                    ShutdownWamMode();
                    Console.WriteLine("WAM Results:  TagsRead=" + WamTags.Count);
                    foreach (var item in WamTags)
                    {
                        Tag tag = item.Value;
                        Console.WriteLine(item.Key + "  Ant=" + tag.AntennaPortNumber+ "\tRSSI=" + tag.PeakRssiInDbm);
                    }
                    Console.WriteLine();
                    WamTags.Clear();

                    // Location Role
                    Console.WriteLine("Running Location. Please wait " + LOCATION_ROLE_DURATION / 1000 + " sec.");
                    SetupLocationMode();
                    System.Threading.Thread.Sleep(LOCATION_ROLE_DURATION);
                    ShutdownLocationMode();
                    Console.WriteLine("Location Results: " + LocTags.Count +" Tags Read");
                    foreach (var item in LocTags)
                    {
                        LocationReport tag = item.Value;
                        Console.WriteLine(item.Key + "\tReadCount=" + tag.ConfidenceFactors.ReadCount + "\tX=" + tag.LocationXCm + "\tY=" + tag.LocationYCm);
                    }
                    LocTags.Clear();
                    Console.WriteLine();
                    // Wait for tag percistance to complete before starting WAM again
                    if ((i < INTERATIONS - 1) && (WAM_SESSION == 2 || WAM_SESSION == 3)) {
                        Console.WriteLine("Wait " + SESSION_2_OR_3_PERSISTENCE/1000 + " Sec. for tag percistance to complete before starting WAM again");
                        System.Threading.Thread.Sleep(SESSION_2_OR_3_PERSISTENCE);
                    }
                }
                // Apply the default settings before exiting.
                reader.ApplyDefaultSettings();
                // Disconnect from the reader.
                reader.Disconnect();
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
        // WAM code
        public void SetupWamMode()
        {
            Settings settings = reader.QueryDefaultSettings();
            // Put the xArray into location mode
            settings.XArray.Mode = XArrayMode.WideAreaMonitor;

            settings.Report.IncludeAntennaPortNumber = true;
            settings.Report.IncludePeakRssi = true;
            // Set the reader mode, search mode and session
            settings.ReaderMode = READER_MODE;
            settings.SearchMode = WAM_SEARCH_MODE;
            settings.Session = WAM_SESSION;
            settings.TagPopulationEstimate = TAG_POPULATION_ESTIMATE;

            // Enable all Antennas (Beams)
            settings.Antennas.EnableAll();
            // Gen2 Filtering
            settings = SetupFilter(settings);

            reader.ApplySettings(settings);
            reader.TagsReported += OnTagsReported;

            // Start reading.
            reader.Start();
        }
        public void ShutdownWamMode()
        {
            reader.Stop();
            reader.TagsReported -= OnTagsReported;
        }
        // Location code
        public void SetupLocationMode()
        {
            // Add Locations Report delegate
            reader.LocationReported += OnLocationReported;
            // Start with defaults
            Settings settings = reader.QueryDefaultSettings();
            // Put the xArray into location mode
            settings.XArray.Mode = XArrayMode.Location;

            // Enable all three report types
            settings.XArray.Location.EntryReportEnabled = true;
            settings.XArray.Location.UpdateReportEnabled = true;
            settings.XArray.Location.ExitReportEnabled = true;

            // The HeightCm of the xArray, in centimeters
            settings.XArray.Placement.HeightCm = XARRAY_HEIGHT;
            settings.XArray.Placement.FacilityXLocationCm = 0;
            settings.XArray.Placement.FacilityYLocationCm = 0;
            settings.XArray.Placement.OrientationDegrees = 0;

            // Motion Window and Tag age
            settings.XArray.Location.ComputeWindowSeconds = COMPUTE_WINDOW;
            settings.ReaderMode = READER_MODE;
            settings.Session = LOCATION_SESSION;
            settings.XArray.Location.TagAgeIntervalSeconds = TAG_AGE_INTERVAL;
            settings.XArray.Location.UpdateIntervalSeconds = UPDATE_INTERVAL;

            // Gen2 Filtering
            settings = SetupFilter(settings);

            // Apply and start Reading in Location mode
            reader.ApplySettings(settings);
            reader.Start();
        }
        public void ShutdownLocationMode()
        {
            reader.Stop();
            // Remove location report delegate
            reader.LocationReported += OnLocationReported;
        }

        // Main Program
        static void Main(string[] args)
        {
            new Program();
        }

        // Location Reports
        void OnLocationReported(ImpinjReader reader, LocationReport report)
        {
            string EpcStr = report.Epc.ToHexString();
            // Collect tags read from the last location report
            if (LocTags.ContainsKey(EpcStr))
                LocTags.Remove(EpcStr);
            LocTags.Add(EpcStr, report);
            // Comment out next line to see L every time a tag is reported
            // Console.Write("L");
        }
        // WAM reports
        void OnTagsReported(ImpinjReader sender, TagReport report)
        {
            foreach (Tag tag in report)
            {
                string EpcStr = tag.Epc.ToHexString();
                // Collect tags read and their counts per inventory round
                if (WamTags.ContainsKey(EpcStr))
                    WamTags.Remove(EpcStr);
                WamTags.Add(EpcStr, tag);
                // Comment out next line to see W every time a tag is reported
                // Console.Write("W");
            }
        }
        Settings SetupFilter(Settings settings)
        {
            
        /*  // Filter out extranious tags
            settings.Filters.TagFilter1.TagMask = "9999";  // Only match tags with EPCs with this prefix
            settings.Filters.TagFilter1.BitCount = 16; 
            settings.Filters.TagFilter1.MemoryBank = MemoryBank.Epc;
            settings.Filters.TagFilter1.BitPointer = BitPointers.Epc;
            settings.Filters.TagFilter1.BitCount = 16;  
            settings.Filters.Mode = TagFilterMode.OnlyFilter1;  // Use just one filter
        */    
            return settings;
        }
    }
}
