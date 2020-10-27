using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ronpaTool
{
    class Program
    {
        static void Main(string[] args)
        {
            var ext = Path.GetExtension(args[0]);
            if (ext == ".obb") ExtractOBB(args[0]);
            else if (ext == ".ab") FindMP4(args[0]);
            else Console.WriteLine($"Unknown extension: {ext}. This program worked with .obb and .ab (movie) file formats.");
        }

        static void ExtractOBB(string obbName)
        {
            using (BinaryReader reader = new BinaryReader(File.OpenRead(obbName)))
            {
                var fileName = Path.GetFileNameWithoutExtension(obbName);
                var header = reader.ReadBytes(3);
                var unknown = reader.ReadInt32();
                var files = reader.ReadUInt32();
                for (int i = 0; i < files; i++)
                {
                    var stringSize = reader.ReadByte();
                    var pathString = Encoding.UTF8.GetString(reader.ReadBytes(stringSize));
                    var offset = reader.ReadInt32();
                    var size = reader.ReadInt32();
                    var savePosition = reader.BaseStream.Position + 0x10;
                    reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                    var file = reader.ReadBytes(size);
                    var folderName = Path.GetDirectoryName(pathString);
                    Directory.CreateDirectory($@"{fileName}\{folderName}");
                    Console.WriteLine($@"Extracting: {pathString}");
                    File.WriteAllBytes($@"{fileName}\{pathString}", file);
                    reader.BaseStream.Seek(savePosition, SeekOrigin.Begin);
                }
                Console.WriteLine("");
                Console.WriteLine($@"{files} files successfuly extracted! Press any key to exit.");
                Console.ReadKey();
            }
        }

        static void RepackOBB(string input, string outputObb)
        {

        }

        static void FindMP4(string fileName)
        {
            var extension = Path.GetExtension(fileName);
            var nameWE = Path.GetFileNameWithoutExtension(fileName);
            if (extension != ".ab") return;

            byte[] mask = { 0x00, 0x00, 0x00, 0x1C, 0x66, 0x74, 0x79, 0x70, 0x6D, 0x70, 0x34, 0x32 };

            byte[] input = File.ReadAllBytes(fileName);
            var offset = SearchBytePattern(mask, input);
            var length = input.Length;
            var size = length - offset;
            using (BinaryReader reader = new BinaryReader(File.OpenRead(fileName)))
            {
                reader.BaseStream.Seek(offset, SeekOrigin.Begin);
                if (size == length)
                {
                    Console.WriteLine($"Unsupported file format.");
                    return;
                }
                var file = reader.ReadBytes((int)size);
                File.WriteAllBytes(nameWE + ".mp4", file);
            }
        }

        static public long SearchBytePattern(byte[] pattern, byte[] bytes)
        {
            List<int> positions = new List<int>();
            int patternLength = pattern.Length;
            int totalLength = bytes.Length;
            byte firstMatchByte = pattern[0];
            for (int i = 0; i < totalLength; i++)
            {
                if (firstMatchByte == bytes[i] && totalLength - i >= patternLength)
                {
                    byte[] match = new byte[patternLength];
                    Array.Copy(bytes, i, match, 0, patternLength);
                    if (match.SequenceEqual<byte>(pattern))
                    {
                        positions.Add(i);
                        i += patternLength - 1;
                    }
                }
            }
            try
            {
                return positions[0];
            }
            catch (ArgumentOutOfRangeException)
            {
                return 0;
            }

        }
    }
}
