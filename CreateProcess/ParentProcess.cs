using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace CreateProcess
{
    internal class ParentProcess
    {
        private static void Main(string[] args)
        {
            using (MemoryMappedFile mmf = MemoryMappedFile.CreateNew("memfile", 128))
            {
                using (MemoryMappedViewStream stream = mmf.CreateViewStream())
                {
                    BinaryWriter writer = new BinaryWriter(stream);
                    int[][] arrInts = new int[5][];
                    InitializeArray(arrInts);
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(stream, arrInts);
                    //ArrayWrite(arrInts, writer);
                }
                string[] cmdArgs = new int[] { 121, 32, 321 }.Select(i => i.ToString()).ToArray();
                Console.WriteLine("Starting the child process");
                // Command line args are separated by a space
                Process p = Process.Start("ChildProcess.exe", $"memfile {string.Join(" ", cmdArgs)}");

                Console.WriteLine("Waiting child to die");

                p.WaitForExit();
                Console.WriteLine("Child died");

                using (MemoryMappedViewStream stream = mmf.CreateViewStream())
                {
                    BinaryReader reader = new BinaryReader(stream);
                    //ArrayRead(reader);
                    BinaryFormatter bf = new BinaryFormatter();

                    int[][] arr = (int[][])bf.Deserialize(stream);

                    dynamic arr2 = bf.Deserialize(stream);  // Converted to int[] at runtime
                    Console.WriteLine();
                    Console.WriteLine("Reading child output..");
                }
            }
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }

        /// <summary>Writes an array to shared memory</summary>
        /// <param name="arrInts">Array to write to shared memory</param>
        /// <param name="writer">Writer with access to shared memory stream</param>
        private static void ArrayWrite(int[][] arrInts, BinaryWriter writer)
        {
            writer.Write(arrInts.GetLength(0));
            writer.Write(arrInts[0].GetLength(0));

            for (int i = 0; i < arrInts.GetLength(0); i++)
            {
                for (int j = 0; j < arrInts[i].GetLength(0); j++)
                {
                    writer.Write(arrInts[i][j]);
                    Console.Write(" " + arrInts[i][j]);
                }
                Console.WriteLine();
            }
        }

        /// <summary>Reads an array from shared memory</summary>
        /// <param name="reader">Reader with access to shared memory stream</param>
        private static void ArrayRead(BinaryReader reader)
        {
            int rowLen, colLen;
            Console.WriteLine("parent reading row length: " + (rowLen = reader.ReadInt32()));
            Console.WriteLine("parent reading col length: " + (colLen = reader.ReadInt32()));

            int[][] arrInts = new int[rowLen][];
            for (int i = 0; i < rowLen; i++)
            {
                arrInts[i] = new int[colLen];
                for (int j = 0; j < colLen; j++)
                {
                    arrInts[i][j] = reader.ReadInt32();
                    Console.Write("  " + arrInts[i][j]);
                }
                Console.WriteLine();
            }
        }

        private static void InitializeArray(int[][] array)
        {
            for (int i = 0; i < array.GetLength(0); i++)
            {
                array[i] = new int[6];
                for (int j = 0; j < 6; j++)
                {
                    array[i][j] = j * 2;
                }
            }
        }
    }
}

// There is no subtle way to clear the memory mapped file, so each process that starts reading the
// memory mapped file will read it from the very begininng making it unconvenient to handle the
// reading / writing process, an alternative solution is that the parent process will write the whole
// matrix in the shared memory then the children read it, after that the child writes back to the
// shared memory then the parent reads the child's result using the same stream to avoid losing the
// current pointer location the child row array will be passed to it as a command line argument the
// main problem is because we can't cast the shared memory to multiple objects as in linux because C#
// enforces type safety.