using System;
using System.ComponentModel.Design;

namespace SystematicCapacity.UIC406Compression
{
    /// <summary>
    /// the main class of timetable compression excecution
    /// </summary>
    public abstract class TimetableCompressionHandler
    {
        /// <summary>
        /// applied timetable compression method
        /// </summary>
        public CompressionMethods Method { get; set; }

        public void Execute()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Compressing timetable...\nApplied method: {0}", Method.ToString());
            Console.ResetColor();

            Initialize();
            Compress();
            ParseCompressedTimetable();

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine("Timetable compression finished!");
            Console.ResetColor();
        }

        /// <summary>
        /// initialize the timetable compression models
        /// </summary>
        protected virtual void Initialize()
        {

        }

        /// <summary>
        /// compress the timetable
        /// </summary>
        protected virtual void Compress()
        {

        }

        /// <summary>
        /// parse the raw solution data to a formal timetable
        /// </summary>
        protected virtual void ParseCompressedTimetable()
        {

        }
    }

    /// <summary>
    /// timetable compression method enumerations
    /// </summary>
    public enum CompressionMethods
    {
        /// <summary>
        /// invoking a commercial linear programming solver to obtain the compressed timetable
        /// </summary>
        IntegerProgramming,

        /// <summary>
        /// invoking a key-path finding algorithm on the event-activity network
        /// </summary>
        EventActivityNetwork,

        /// <summary>
        /// invoking a max-plus algebra to calculate the compressed timetable
        /// </summary>
        MaxPlusAutomata,
    }
}