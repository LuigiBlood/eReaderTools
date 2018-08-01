using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ecardtool
{
    class Program
    {
        static string[] cardTypeNames = { "Pokemon Viewer", "Pokemon Viewer",
            "Pokemon Application (Music A)", "Pokemon Application (Music A)",
            "Pokemon Application (Music B)", "Pokemon Application (Music B)",
            "Pokemon Attack", "Pokemon Attack",
            "Construction Escape", "Construction Escape",
            "Construction Action", "Construction Action",
            "Construction Melody Box", "Construction Melody Box",
            "Application", "Game Specific",
            "Pokemon Viewer", "Pokemon Viewer",
            "Pokemon Viewer", "Pokemon Viewer",
            "Pokemon Viewer", "Pokemon Viewer",
            "Pokemon Viewer", "Pokemon Viewer",
            "Pokemon Viewer", "Pokemon Viewer",
            "Pokemon Viewer", "Pokemon Viewer",
            "Pokemon Viewer", "Pokemon Viewer",
            "Application", "Game Specific"};

        static string[] cardRegionNames = {"Japan / Original", "North America / Oceania", "Japan / Plus",
            "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown", "Unknown"};

        static string[] stripNames = { "Unknown", "Short", "Long" };

        static void Main(string[] args)
        {
            Console.WriteLine("ecardtool v0.1");
            Console.WriteLine("-------------\nBy LuigiBlood\n");

            if (args.Length <= 0)
            {
                //No arguments, use help stuff
                Console.WriteLine("Usage: ecardtool [options] file\n");
                Console.WriteLine("[options] (not required):\n" +
                    "     -f     Fix Checksum\n" +
                    "\nNo options would output card info.");
            }
            else if (args.Length == 1)
            {
                if (args[0] == "-f")
                {
                    Console.WriteLine("Error: File is missing");
                    return;
                }

                //Output Card Info
                if (File.Exists(args[0]))
                {
                    OutputCardInfo(args[0]);
                    return;
                }
                else
                {
                    Console.WriteLine("Error: File not found");
                    return;
                }
            }
            else
            {
                if (args[0] == "-f")
                {
                    if (File.Exists(args[1]))
                    {
                        FixCardChecksum(args[1]);
                        return;
                    }
                    else
                    {
                        Console.WriteLine("Error: File not found");
                        return;
                    }
                }
                else
                {
                    Console.WriteLine("Error: Unknown Option");
                    return;
                }
            }
        }

        static int DoDataChecksum(byte[] data)
        {
            int checksum = 0;
            for (int i = 0; i < data.Length; i += 2)
            {
                checksum += ((data[i] << 8) | data[i + 1]);
            }
            checksum = (checksum & 0xFFFF) ^ 0xFFFF;

            return checksum;
        }

        static byte DoHeaderChecksum(byte[] header)
        {
            byte hcheck = (byte)(header[0x0C] ^ header[0x0D] ^ header[0x10] ^ header[0x11]
                ^ header[0x26] ^ header[0x27] ^ header[0x28] ^ header[0x29]
                ^ header[0x2A] ^ header[0x2B] ^ header[0x2C] ^ header[0x2D]);

            return hcheck;
        }

        static byte DoGlobalChecksum(byte[] header, byte[] data)
        {
            byte gcheck = 0;
            for (int i = 0; i < (header.Length - 1); i++)
            {
                gcheck += header[i];
            }
            for (int i = 0; i < (data.Length / 0x30); i++)
            {
                byte xor = 0;
                for (int j = 0; j < 0x30; j++)
                {
                    xor ^= data[(i * 0x30) + j];
                }
                gcheck += xor;
            }
            gcheck ^= 0xFF;

            return gcheck;
        }

        static void FixCardChecksum(string path)
        {
            FileStream file = new FileStream(path, FileMode.Open);
            byte[] header = new byte[0x30];
            //Read header
            file.Seek(0, SeekOrigin.Begin);
            file.Read(header, 0, 0x30);

            //Check if it is an e-Reader Card
            if (!(header[0x00] == 0x00 && header[0x01] == 0x30 && header[0x04] == 0x00 && header[0x05] == 0x01 && header[0x15] == 0x19 && header[0x19] == 0x08))
            {
                Console.WriteLine("This is not an e-Reader Card.");
                file.Close();
                return;
            }

            int datasize = ((header[0x06] << 8) | header[0x07]);

            //Read data
            byte[] data = new byte[datasize];
            file.Seek(0x30, SeekOrigin.Begin);
            file.Read(data, 0, datasize);

            int dataChecksum = DoDataChecksum(data);
            header[0x13] = (byte)(dataChecksum >> 8);
            header[0x14] = (byte)dataChecksum;

            header[0x2E] = DoHeaderChecksum(header);
            header[0x2F] = DoGlobalChecksum(header, data);

            file.Seek(0, SeekOrigin.Begin);
            file.Write(header, 0, header.Length);

            file.Close();

            Console.WriteLine("Card Checksum Fixed.");
        }

        static void OutputCardInfo(string path)
        {
            FileStream file = new FileStream(path, FileMode.Open);
            byte[] header = new byte[0x30];
            //Read header
            file.Seek(0, SeekOrigin.Begin);
            file.Read(header, 0, 0x30);

            //Check if it is an e-Reader Card
            if (!(header[0x00] == 0x00 && header[0x01] == 0x30 && header[0x04] == 0x00 && header[0x05] == 0x01 && header[0x15] == 0x19 && header[0x19] == 0x08))
            {
                Console.WriteLine("This is not an e-Reader Card.");
                file.Close();
                return;
            }

            int datasize = ((header[0x06] << 8) | header[0x07]);

            //Read data
            byte[] data = new byte[datasize];
            file.Seek(0x30, SeekOrigin.Begin);
            file.Read(data, 0, datasize);

            file.Close();

            //Header Info
            int stripsize = (header[0x06] << 8) | header[0x07];
            int cardtype = ((header[0x03] & 1) << 4) | (header[0x0C] >> 4);
            int cardregion = header[0x0D] & 0xF;
            int striptype = header[0x0E];
            int cardID = (header[0x10] << 8) | header[0x11];
            int stripDataCheck = (header[0x13] << 8) | header[0x14];
            int stripnumber = ((header[0x26] & 0x1E) >> 1);
            int stripamount = ((header[0x27] & 1) << 4) | ((header[0x26] & 0xE0) >> 5);
            int striptotalsize = (header[0x28] << 7) | (header[0x27] >> 1);
            //Flags
            bool datasave = ((header[0x29] & 1) == 1);
            bool striptitle = ((header[0x29] & 2) == 2);
            bool apptype = ((header[0x29] & 4) == 4);
            byte cardHeadCheck = header[0x2E];
            byte cardGlobalCheck = header[0x2F];

            //Output info to console
            Console.WriteLine("e-Reader Card Information:");
            Console.WriteLine("  - Card Type: 0x" + cardtype.ToString("X2") + " (" + cardTypeNames[cardtype] + ")");
            Console.WriteLine("  - Card Region: 0x" + cardregion.ToString("X2") + " (" + cardRegionNames[cardregion] + ")");
            Console.WriteLine("  - Card ID: 0x" + cardID.ToString("X4"));

            Console.WriteLine("\n  - Strip Type: 0x" + striptype.ToString("X2") + " (" + stripNames[striptype] + ")");
            Console.WriteLine("  - Strip Size: 0x" + stripsize.ToString("X4") + " bytes");
            Console.WriteLine("  - Strip Number: " + stripnumber.ToString() + " of " + stripamount.ToString());
            Console.WriteLine("  - Total Data Size: 0x" + striptotalsize.ToString("X4") + " bytes");

            if (stripDataCheck == DoDataChecksum(data))
                Console.WriteLine("  - Strip Data Checksum: 0x" + stripDataCheck.ToString("X4") + " (OK)");
            else
                Console.WriteLine("  - Strip Data Checksum: 0x" + stripDataCheck.ToString("X4") + " (Error)");

            if (cardHeadCheck == DoHeaderChecksum(header))
                Console.WriteLine("  - Strip Header Checksum: 0x" + cardHeadCheck.ToString("X2") + " (OK)");
            else
                Console.WriteLine("  - Strip Header Checksum: 0x" + cardHeadCheck.ToString("X2") + " (Error)");

            if (cardGlobalCheck == DoGlobalChecksum(header, data))
                Console.WriteLine("  - Strip Global Checksum: 0x" + cardGlobalCheck.ToString("X2") + " (OK)");
            else
                Console.WriteLine("  - Strip Global Checksum: 0x" + cardGlobalCheck.ToString("X2") + " (Error)");

            if (datasave)
                Console.WriteLine("\n  - Save Permission: Yes");
            else
                Console.WriteLine("\n  - Save Permission: No");

            if (striptitle)
                Console.WriteLine("  - Strip Titles: Yes");
            else
                Console.WriteLine("  - Strip Titles: No");

            if (cardtype == 2 || cardtype == 3 || cardtype == 4 || cardtype == 5 || cardtype == 0xE || cardtype == 0x1E)
            {
                if (apptype)
                    Console.WriteLine("  - Application Type: NES");
                else
                    Console.WriteLine("  - Application Type: GBA/Z80");
            }
        }
    }
}
