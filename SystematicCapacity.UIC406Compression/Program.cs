﻿using System;
using SystematicCapacity.Core;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SystematicCapacity.UIC406Compression
{
    internal class Program
    {
        /// <summary>
        /// basic data directory
        /// </summary>
        static internal string basicDataDirectory;

        /// <summary>
        /// solution output data directory
        /// </summary>
        static internal string solutionDataDirectory;

        /// <summary>
        /// applied compression method
        /// </summary>
        static CompressionMethods? compressionMethod;

        /// <summary>
        /// global data repository
        /// </summary>
        internal static GlobalDataRepository DataRepository;
        
        /// <summary>
        /// output file level, 0: only solution files; 1: with debug files
        /// </summary>
        internal static int OutputLevel;

        /// <summary>
        /// the timetable compression handler
        /// </summary>
        static TimetableCompressionHandler timetableCompressionHandler;

        static void Main(string[] args)
        {
            Console.WriteLine("\n\n\n******\nA railway timetable compression program implementing UIC leaflet code 406");
            Console.WriteLine("Composed by Zhengwen Liao (zwliao@bjtu.edu.cn), Beijing Jiaotong University\n******\n");

            // generate the global data repository. A *.dll library from SystematicCapacity.Core assembly is needed.
            DataRepository = new GlobalDataRepository();

            // read timetable compression configuration from *.json file 
            ReadConfig("./Data/Config.json");

            // read required data from a given directory
            ReadData(basicDataDirectory);

            // generate a timetable handler associated with the selected method 
            switch (compressionMethod)
            {
                case CompressionMethods.IntegerProgramming:
                    timetableCompressionHandler = new LPTimetableCompressionHandler();
                    break;
                case CompressionMethods.EventActivityNetwork:
                    timetableCompressionHandler = new EANTimetableCompressionHandler();
                    break;
                default:
                    timetableCompressionHandler = null;
                    break;
            }

            // run the timetable compression handler
            if (timetableCompressionHandler!=null)
            {
                timetableCompressionHandler.Execute();
            }

            // output a compressed timetable to a given directory
            WriteData(solutionDataDirectory);

            // terminate the program
            Console.WriteLine("UIC 406 timetable compression terminated. Press Enter to escape.");
            Console.Read();
        }

        static void ReadData(string directory)
        {
            // read basic data
            try
            {
                DataRepository.ReadStationData(directory + "Station.csv");
                DataRepository.ReadSegmentData(directory + "SegmentTrack.csv", directory + "SegmentTrackPara.csv");
                DataRepository.ReadTrainData(directory + "Train.csv", directory + "TrainOperation.csv");
            }
            catch(Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Basic data reading error: {0}", ex.Message);
                Console.ResetColor();
            }
            
            // read the original timetable ready for compressing
            ReadOrgTimetable(directory + "OrgTimetable.csv");

            // read timetable compression configuration. The parameters are organized as *.json format
            ReadConfig(directory + "Config.json");
        }

        static void ReadOrgTimetable(string fileName)
        {
            try
            {
                StreamReader sr = new StreamReader(fileName);
                sr.ReadLine();
                string line = sr.ReadLine();
                while (line != null && line.Trim() != "")
                {
                    string[] data = line.Split(',');
                    Train tr = DataRepository.serviceRepo.TrainList.Find(x => x.ID == data[0]);
                    Station sta = DataRepository.infraRepo.StationSet.Find(x => x.ID == data[1]);
                    tr.Timetable.Add(sta, new System.Collections.Generic.Dictionary<string, int>());
                    tr.Timetable[sta].Add("arrival", Convert.ToInt32(data[2]));;
                    tr.Timetable[sta].Add("departure", Convert.ToInt32(data[3]));
                    line = sr.ReadLine();
                }
                sr.Close();
            }
            catch(Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Original timetable reading error: {0}", ex.Message);
                Console.ResetColor();
            }
        }

        static void ReadConfig(string fileName)
        {
            try
            {
                StreamReader sr = File.OpenText(fileName);

                JObject jobj = JObject.Parse(sr.ReadToEnd());

                basicDataDirectory = jobj.Property("basic_data_directory").Value.ToString();
                solutionDataDirectory = jobj.Property("solution_data_directory").Value.ToString();
                compressionMethod = (CompressionMethods)Enum.Parse(typeof(CompressionMethods),
                    jobj.Property("compression_method").Value.ToString());
                OutputLevel = Convert.ToInt32(jobj.Property("output_file_level").Value.ToString());

                sr.Close();
            }
            catch(Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Configuration reading error: {0}", ex.Message);
                Console.ResetColor();
            }
        }

        static void WriteData(string directory)
        {

        }
    }
}
