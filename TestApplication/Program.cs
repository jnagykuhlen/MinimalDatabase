using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

using MinimalDatabase;
using MinimalDatabase.Logging;

namespace TestApplication
{
    public class Program
    {
        private const string FilePath = "TestDB.db";

        public static void Main(string[] args)
        {
            // Logger.LoggingDevice = new ConsoleLoggingDevice();

            Console.WriteLine("Creating persistence service...");
            using (FilePersistenceService persistenceService = new FilePersistenceService(FilePath))
            {
                Console.WriteLine("Loading database...");
                Database database = new Database(persistenceService);
                
                uint storageId = database.StorageManager.AllocateStorage(10000000);

                using (Stream storageStream = database.StorageManager.GetStorageStream(storageId))
                {
                    string text = "Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet. Lorem ipsum dolor sit amet, consetetur sadipscing elitr, sed diam nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam erat, sed diam voluptua. At vero eos et accusam et justo duo dolores et ea rebum. Stet clita kasd gubergren, no sea takimata sanctus est Lorem ipsum dolor sit amet.";

                    long[] writePositions = new long[]
                    {
                        9956421,
                         151345,
                        6635489
                    };

                    string[] writeTexts = new string[]
                    {
                        text,
                        text.ToLower(),
                        text.ToUpper()
                    };

                    for (int i = 0; i < 3; ++i)
                    {
                        storageStream.Position = writePositions[i];

                        byte[] writeData = Encoding.ASCII.GetBytes(writeTexts[i]);
                        storageStream.Write(writeData, i * 13, 500);

                        Console.WriteLine("Written {0} Byte of data.", 500);
                    }

                    for (int i = 0; i < 3; ++i)
                    {
                        storageStream.Position = writePositions[i];

                        int offset = i * 17;
                        byte[] readData = new byte[600];
                        int readBytes = storageStream.Read(readData, offset, 500);

                        Console.WriteLine("Read {0} Byte of data: {1}", readBytes, Encoding.ASCII.GetString(readData, offset, readBytes));
                        Console.WriteLine();
                    }
                }

                database.StorageManager.DeallocateStorage(storageId);

                Console.WriteLine("Closing database...");
            }
            
            Console.WriteLine();
            Console.WriteLine("Press any key to exit.");
            Console.ReadKey(false);
        }
    }
}
