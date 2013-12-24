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
using System.Linq;
using System.IO;
using System.Text;
using System.Security.Cryptography;
using System.Threading.Tasks;

namespace com.avilance.Starrybound.Util
{
    public class Utils
    {
        public static string ByteArrayToString(byte[] buffer)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            foreach (byte b in buffer)
                sb.Append(b.ToString("X2"));

            return (sb.ToString());
        }

        public static string ByteToBinaryString(byte byteIn)
        {
            StringBuilder out_string = new StringBuilder();
            byte mask = 128;
            for (int i = 7; i >= 0; --i)
            {
                out_string.Append((byteIn & mask) != 0 ? "1" : "0");
                mask >>= 1;
            }
            return out_string.ToString();
        }

        public static string StarHashPassword(string message, string salt, int rounds)
        {
            SHA256 sha256 = SHA256.Create();
            byte[] messageBuffer = sha256.ComputeHash(Encoding.UTF8.GetBytes(message));
            byte[] saltBuffer = Encoding.UTF8.GetBytes(salt);

            while (rounds > 0)
            {
                MemoryStream ms = new MemoryStream();
                ms.Write(messageBuffer, 0, messageBuffer.Length);
                ms.Write(saltBuffer, 0, saltBuffer.Length);
                messageBuffer = sha256.ComputeHash(ms.ToArray());
                rounds--;
            }

            return Convert.ToBase64String(messageBuffer);
        }

        public static string GenerateSecureSalt()
        {
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] buffer = new byte[24];
            rng.GetNonZeroBytes(buffer);
            return Convert.ToBase64String(buffer);
        }    
    }
}
