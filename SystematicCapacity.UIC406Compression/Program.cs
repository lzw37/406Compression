using System;
using SystematicCapacity.Core;
using System.IO;

namespace SystematicCapacity.UIC406Compression
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("\n\n\n******\nA railway timetable compression program implementing UIC leaflet code 406");
            Console.WriteLine("Composed by Zhengwen Liao (zwliao@bjtu.edu.cn), Beijing Jiaotong University\n******\n");

            // generate the global data repository. A *.dll library from SystematicCapacity.Core assembly is needed.
            GlobalDataRepository dataRepository = new GlobalDataRepository();

            // read required data from 'Data' directory
            ReadData(dataRepository, "Data/");

            Console.WriteLine("UIC 406 timetable compression terminated. Press Enter to escape.");
            Console.Read();
        }

        static void ReadData(GlobalDataRepository dataRepository, string directory)
        {
            // read fundamental data
            try
            {
                dataRepository.ReadStationData(directory + "Station.csv");
                dataRepository.ReadSegmentData(directory + "SegmentTrack.csv", directory + "SegmentTrackPara.csv");
                dataRepository.ReadTrainData(directory + "Train.csv", directory + "TrainOperation.csv");
            }
            catch(Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Fundamental data reading error: {0}", ex.Message);
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

            }
            catch(Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Configuration reading error: {0}", ex.Message);
                Console.ResetColor();
            }
        }
    }
}
