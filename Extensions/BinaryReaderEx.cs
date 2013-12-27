/* 
 * Starrybound Server
 * Copyright 2013, Avilance Ltd
 * Created by Zidonuke (zidonuke@gmail.com) and Crashdoom (crashdoom@avilance.com)
 * 
 * This file is a part of Starrybound Server.
 * Starrybound Server is free software: you can redistribute it and/or modify it under the terms of the GNU General Public License as published by the Free Software Foundation, either version 3 of the License, or (at your option) any later version.
 * Starrybound Server is distributed in the hope that it will be useful, but WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU General Public License for more details.
 * You should have received a copy of the GNU General Public License along with Starrybound Server. If not, see http://www.gnu.org/licenses/.
*/

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using com.avilance.Starrybound.Util;

namespace com.avilance.Starrybound.Extensions
{
    public static class BinaryReaderEx
    {
        public static byte[] ReadStarByteArray(this BinaryReader read)
        {
            ulong size = read.ReadVarUInt64();
            return read.ReadBytes((int)size);
        }

        public static string ReadStarString(this BinaryReader read)
        {
            ulong size = read.ReadVarUInt64();
            if (size > 0)
                return Encoding.UTF8.GetString(read.ReadBytes((int)size));
            else
                return "";
        }

        public static byte[] ReadStarUUID(this BinaryReader read)
        {
            bool exists = read.ReadBoolean();
            if(exists)
                return read.ReadBytes(16);
            return new byte[16];
        }

        public static WorldCoordinate ReadStarWorldCoordinate(this BinaryReader read)
        {
            string sector = read.ReadStarString();
            if (sector != "")
            {
                int x = read.ReadInt32BE();
                int y = read.ReadInt32BE();
                int z = read.ReadInt32BE();
                int planet = read.ReadInt32BE();
                int satellite = read.ReadInt32BE();
                return new WorldCoordinate(sector, x, y, z, planet, satellite);
            }
            else
                return null;
        }

        public static SystemCoordinate ReadStarSystemCoordinate(this BinaryReader read)
        {
            string sector = read.ReadStarString();
            if (sector != "")
            {
                int x = read.ReadInt32BE();
                int y = read.ReadInt32BE();
                int z = read.ReadInt32BE();
                return new SystemCoordinate(sector, x, y, z);
            }
            else
                return null;
        }

        public static Dictionary<string, WorldCoordinate> ReadStarCelestialLog(this BinaryReader read)
        {
            Dictionary<string, WorldCoordinate> returnList = new Dictionary<string, WorldCoordinate>();
            byte[] celestialLog = read.ReadStarByteArray();
            BinaryReader celestialRead = new BinaryReader(new MemoryStream(celestialLog));
            uint numVisited = celestialRead.ReadVarUInt32();
            for (int i = 0; i < numVisited; i++)
            {
                celestialRead.ReadStarSystemCoordinate();
            }
            uint numSectors = celestialRead.ReadVarUInt32();
            for (int i = 0; i < numSectors; i++)
            {
                celestialRead.ReadStarString();
                celestialRead.ReadBoolean();
            }
            byte unk = celestialRead.ReadByte();
            SystemCoordinate coords = celestialRead.ReadStarSystemCoordinate(); //Seems to be the current system coords.
            WorldCoordinate curLoc = celestialRead.ReadStarWorldCoordinate();
            if (curLoc != null)
            {
                returnList.Add("loc", curLoc);
            }
            WorldCoordinate curHome = celestialRead.ReadStarWorldCoordinate();
            if (curHome != null)
            {
                returnList.Add("home", curHome);
            }
            return returnList;
        }

        public static List<object> ReadStarVariant(this BinaryReader read)
        {
            List<object> result = new List<object>();
            byte type = read.ReadByte();
            switch (type)
            {
                case 2:
                    result.Add(read.ReadDoubleBE());
                    break;
                case 3:
                    result.Add(read.ReadBoolean());
                    break;
                case 4:
                    result.Add(read.ReadVarInt64());
                    break;
                case 5:
                    result.Add(read.ReadStarString());
                    break;
                case 6:
                    uint size = read.ReadVarUInt32();
                    for(int i=0; i < size; i++)
                    {
                        result.Add(read.ReadStarVariant());
                    }
                    break;
                case 7:
                    uint size2 = read.ReadVarUInt32();
                    for(int i=0; i < size2; i++)
                    {
                        Dictionary<string, object> VariantMap = new Dictionary<string,object>();
                        VariantMap.Add(read.ReadStarString(), read.ReadStarVariant());
                        result.Add(VariantMap);
                    }
                    break;
            }
            return result;
        }

        public static short ReadInt16BE(this BinaryReader read)
        {
            byte[] buffer = read.ReadBytes(2);
            Array.Reverse(buffer);
            return BitConverter.ToInt16(buffer, 0);
        }

        public static int ReadInt32BE(this BinaryReader read)
        {
            byte[] buffer = read.ReadBytes(4);
            Array.Reverse(buffer);
            return BitConverter.ToInt32(buffer, 0);
        }

        public static uint ReadUInt32BE(this BinaryReader read)
        {
            byte[] buffer = read.ReadBytes(4);
            Array.Reverse(buffer);
            return BitConverter.ToUInt32(buffer, 0);
        }

        public static long ReadInt64BE(this BinaryReader read)
        {
            byte[] buffer = read.ReadBytes(8);
            Array.Reverse(buffer);
            return BitConverter.ToInt64(buffer, 0);
        }

        public static float ReadSingleBE(this BinaryReader read)
        {
            byte[] buffer = read.ReadBytes(4);
            Array.Reverse(buffer);
            return BitConverter.ToSingle(buffer, 0);
        }

        public static double ReadDoubleBE(this BinaryReader read)
        {
            byte[] buffer = read.ReadBytes(8);
            Array.Reverse(buffer);
            return BitConverter.ToDouble(buffer, 0);
        }

        public static byte ReadVarByte(this BinaryReader read)
        {
            var result = ToTarget(read, 8);
            return (byte)result;
        }

        public static short ReadVarInt16(this BinaryReader read)
        {
            var result = ToTarget(read, 16);
            return (short)Decode((short)result);
        }

        public static ushort ReadVarUInt16(this BinaryReader read)
        {
            var result = ToTarget(read, 16);
            return (ushort)result;
        }

        public static int ReadVarInt32(this BinaryReader read)
        {
            var result = ToTarget(read, 32);
            return (int)Decode((int)result);
        }

        public static uint ReadVarUInt32(this BinaryReader read)
        {
            var result = ToTarget(read, 32);
            return (uint)result;
        }

        public static long ReadVarInt64(this BinaryReader read)
        {
            var result = ToTarget(read, 64);
            return Decode((long)result);
        }

        public static ulong ReadVarUInt64(this BinaryReader read)
        {
            var result = ToTarget(read, 64);
            return result;
        }

        private static long Decode(long value)
        {
            if ((value & 1) == 0x00)
                return (value >> 1);
            else
                return -(value >> 1);
        }

        private static ulong ToTarget(BinaryReader read, int sizeBites)
        {
            var buffer = new byte[10];
            var pos = 0;

            int shift = 0;
            ulong result = 0;

            for (;;)
            {
                ulong byteValue = read.ReadByte();
                buffer[pos++] = (byte)byteValue;

                result = (result << 7) | byteValue & 0x7f;

                if (shift > sizeBites)
                    throw new OverflowException("Variable length quantity is too long. (must be " + sizeBites + ")");

                if ((byteValue & 0x80) == 0x00)
                {
                    var bytes = new byte[pos];
                    Buffer.BlockCopy(buffer, 0, bytes, 0, pos);
                    return result;
                }

                shift += 7;
            }

            throw new ArithmeticException("Cannot decode variable length quantity from stream.");
        }

        public static byte[] ReadFully(this BinaryReader read, int requiredSize)
        {
            byte[] buffer = new byte[requiredSize];
            MemoryStream ms = new MemoryStream();
            int curSize = 0;

            while(curSize < requiredSize)
            {
                int size = read.Read(buffer, 0, requiredSize - curSize);
                ms.Write(buffer, 0, size);
                curSize += size;
            }
            return ms.ToArray();
        }
    }
}
