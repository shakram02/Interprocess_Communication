using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace ChildProcess
{
    internal class ChildProcess
    {
        private static void Main(string[] args)
        {
            Console.WriteLine("Child process started");

            string mmfName = args[0];
            List<int> matRow = new List<int>(); // Store the row passed by the parent process

            for (int i = 1; i < args.Length; i++)
            {
                Console.WriteLine("Recieved Arg:" + args[i]);
                matRow.Add(int.Parse(args[i]));
            }

            using (MemoryMappedFile mmf = MemoryMappedFile.OpenExisting(mmfName))
            {
                int[][] arrInts;
                using (MemoryMappedViewStream stream = mmf.CreateViewStream())
                {
                    BinaryReader reader = new BinaryReader(stream);

                    // Read the array from the first process
                    //arrInts = ArrayRead(reader);
                    BinaryFormatter bf = new BinaryFormatter();
                    arrInts = (int[][])bf.Deserialize(stream);
                }
                using (MemoryMappedViewStream input = mmf.CreateViewStream())
                {
                    BinaryWriter writer = new BinaryWriter(input);
                    BinaryFormatter bf = new BinaryFormatter();

                    bf.Serialize(input, arrInts);
                    bf.Serialize(input, new int[] { 5, 3 });
                }
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        /// <summary>Writes an array to shared memory</summary>
        /// <param name="arrInts">Array to write to shared memory</param>
        /// <param name="writer">Writer with access to shared memory stream</param>
        private static void ArrayWriteDoubled(int[][] arrInts, BinaryWriter writer)
        {
            writer.Write(arrInts.GetLength(0));
            writer.Write(arrInts[0].GetLength(0));

            for (int i = 0; i < arrInts.GetLength(0); i++)
                for (int j = 0; j < arrInts[i].GetLength(0); j++)
                    writer.Write(arrInts[i][j] * 2 + 2);
        }

        /// <summary>Reads an array from shared memory</summary>
        /// <param name="reader">Reader with access to shared memory stream</param>
        private static int[][] ArrayRead(BinaryReader reader)
        {
            int rowLen = reader.ReadInt32();
            int colLen = reader.ReadInt32();

            Console.WriteLine("Child read col length:" + colLen);
            Console.WriteLine("Child read row length:" + rowLen);

            int[][] arrInts = new int[rowLen][];
            for (int i = 0; i < rowLen; i++)
            {
                arrInts[i] = new int[colLen];

                for (int j = 0; j < colLen; j++)
                    arrInts[i][j] = reader.ReadInt32();
            }
            return arrInts;
        }
    }
}