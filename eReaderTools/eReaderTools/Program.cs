using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace eReaderTools
{
    static class Program
    {
        /// <summary>
        /// Point d'entrée principal de l'application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }

        public static bool VerifyDataChecksumCard(Stream file)
        {
            byte[] header = new byte[0x30];
            //Read header
            file.Seek(0, SeekOrigin.Begin);
            file.Read(header, 0, 0x30);

            int datasize = ((header[0x06] << 8) | header[0x07]);

            //Read data
            byte[] data = new byte[datasize];
            file.Seek(0x30, SeekOrigin.Begin);
            file.Read(data, 0, datasize);

            file.Close();

            //Data Checksum
            int checksum = 0;
            for (int i = 0; i < datasize; i += 2)
            {
                checksum += ((data[i] << 8) | data[i + 1]);
            }
            checksum = (checksum & 0xFFFF) ^ 0xFFFF;
            MessageBox.Show(checksum.ToString("X4"));

            //Header Checksum
            byte hcheck = 0;
            hcheck = (byte)(header[0x0C] ^ header[0x0D] ^ header[0x10] ^ header[0x11]
                ^ header[0x26] ^ header[0x27] ^ header[0x28] ^ header[0x29]
                ^ header[0x2A] ^ header[0x2B] ^ header[0x2C] ^ header[0x2D]);
            MessageBox.Show(hcheck.ToString("X2"));

            //Global Checksum
            byte gcheck = 0;
            for (int i = 0; i < (header.Length - 1); i++)
            {
                gcheck += header[i];
            }
            for (int i = 0; i < (datasize / 0x30); i++)
            {
                byte xor = 0;
                for (int j = 0; j < 0x30; j++)
                {
                    xor ^= data[(i * 0x30) + j];
                }
                gcheck += xor;
            }
            gcheck ^= 0xFF;
            MessageBox.Show(gcheck.ToString("X2"));

            return (checksum == ((header[0x13] << 8) | header[0x14]));
        }

        public static void FixChecksumCard(string filename)
        {
            FileStream file = new FileStream(filename, FileMode.Open);

            byte[] header = new byte[0x30];
            //Read header
            file.Seek(0, SeekOrigin.Begin);
            file.Read(header, 0, 0x30);

            int datasize = ((header[0x06] << 8) | header[0x07]);

            //Read data
            byte[] data = new byte[datasize];
            file.Seek(0x30, SeekOrigin.Begin);
            file.Read(data, 0, datasize);

            //Data Checksum
            int checksum = 0;
            for (int i = 0; i < datasize; i += 2)
            {
                checksum += ((data[i] << 8) | data[i + 1]);
            }
            checksum = (checksum & 0xFFFF) ^ 0xFFFF;
            header[0x13] = (byte)(checksum >> 8);
            header[0x14] = (byte)(checksum);

            //Header Checksum
            byte hcheck = (byte)(header[0x0C] ^ header[0x0D] ^ header[0x10] ^ header[0x11]
                ^ header[0x26] ^ header[0x27] ^ header[0x28] ^ header[0x29]
                ^ header[0x2A] ^ header[0x2B] ^ header[0x2C] ^ header[0x2D]);

            header[0x2E] = hcheck;

            //Global Checksum
            byte gcheck = 0;
            for (int i = 0; i < (header.Length - 1); i++)
            {
                gcheck += header[i];
            }
            for (int i = 0; i < (datasize / 0x30); i++)
            {
                byte xor = 0;
                for (int j = 0; j < 0x30; j++)
                {
                    xor ^= data[(i * 0x30) + j];
                }
                gcheck += xor;
            }
            gcheck ^= 0xFF;
            header[0x2F] = gcheck;

            file.Seek(0, SeekOrigin.Begin);
            file.Write(header, 0, header.Length);

            file.Close();
        }
    }
}
