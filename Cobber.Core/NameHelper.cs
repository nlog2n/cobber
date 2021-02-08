using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;

namespace Cobber.Core
{
    public enum NameMode
    {
        Unreadable,
        ASCII,
        Letters
    }

    /// <summary>
    /// Renaming helper, with options including unreadable,ascii, letters.
    /// It also stores a random which decides program output.
    /// </summary>
    public class NameHelper
    {
        public static NameHelper Instance; 

        MD5 md5 = MD5.Create();

        public int seed;
        public Random Random; // based on given seed

        CobberDatabase Db;

        internal NameHelper(string seedStr, CobberDatabase db)
        {
            seedStr = "12345d678";

            if (string.IsNullOrEmpty(seedStr)) // random seed
            {
                this.seed = Environment.TickCount;  // TODO: bugs
                //this.seed = typeof(Cobber).Assembly.GetName().Version.ToString().GetHashCode();
            }
            else  // fixed seed
            {
                if (int.TryParse(seedStr, out seed))
                {
                    // given seed integer
                }
                else
                {
                    seed = seedStr.GetHashCode(); 
                }
            }

            this.Random = new Random(this.seed);
            this.Db = db;

            Instance = this; // added on 20140724
        }

        #region Get new names or random names
        public string GetNewName(string originalName)
        {
            return GetNewName(originalName, NameMode.Letters); 
        }

        // all rename functions will fall into this function
        public string GetNewName(string originalName, NameMode mode)
        {
            string ret;
            switch (mode)
            {
                case NameMode.Unreadable: ret = RenameUnreadable(originalName); break;
                case NameMode.ASCII: ret = RenameASCII(originalName); break;
                case NameMode.Letters: ret = RenameLetters(originalName); break;
                default: throw new InvalidOperationException();
            }
            if (Db != null)
            {
                Db.AddEntry("Rename", originalName, ret);
            }
            return ret;
        }

        string GetRandomString()
        {
            byte[] ret = new byte[8];
            this.Random.NextBytes(ret);
            return Convert.ToBase64String(ret);

            //return Guid.NewGuid().ToString();
        }
        public string GetRandomName()
        {
            return GetNewName(GetRandomString());
        }
        public string GetRandomName(NameMode mode)
        {
            return GetNewName(GetRandomString(), mode);
        }

        string RenameUnreadable(string originalName)
        {
            BitArray arr = new BitArray(md5.ComputeHash(Encoding.UTF8.GetBytes(originalName)));

            Random rand = new Random(originalName.GetHashCode() * seed);
            byte[] xorB = new byte[arr.Length / 8];
            rand.NextBytes(xorB);
            BitArray xor = new BitArray(xorB);

            BitArray result = arr.Xor(xor);
            byte[] buff = new byte[result.Length / 8];
            result.CopyTo(buff, 0); 
            
            StringBuilder ret = new StringBuilder();
            int m = 0;
            for (int i = 0; i < buff.Length; i++)
            {
                m = buff[i] + (m << 8);
                while (m >= 32)
                {
                    ret.Append((char)(m % 32 + 1));
                    m /= 32;
                }
            }
            return ret.ToString(); 
        }

        string RenameASCII(string originalName)
        {
            BitArray arr = new BitArray(md5.ComputeHash(Encoding.UTF8.GetBytes(originalName)));

            Random rand = new Random(originalName.GetHashCode() * seed);
            byte[] xorB = new byte[arr.Length / 8];
            rand.NextBytes(xorB);
            BitArray xor = new BitArray(xorB);

            BitArray result = arr.Xor(xor);
            byte[] ret = new byte[result.Length / 8];
            result.CopyTo(ret, 0);

            return Convert.ToBase64String(ret);
        }
       
        string RenameLetters(string originalName)
        {
            // generate a random bit array, based on original name and my seed
            BitArray arr = new BitArray(md5.ComputeHash(Encoding.UTF8.GetBytes(originalName)));

            Random rand = new Random(originalName.GetHashCode() * seed);
            byte[] xorB = new byte[arr.Length / 8];
            rand.NextBytes(xorB);
            BitArray xor = new BitArray(xorB);

            BitArray result = arr.Xor(xor);
            byte[] buff = new byte[result.Length / 8];
            result.CopyTo(buff, 0);

            // convert it into a string
            StringBuilder ret = new StringBuilder();
            int m = 0;
            for (int i = 0; i < buff.Length; i++)
            {
                m = buff[i] + (m << 8);
                while (m >= 52)  // A-Z, a-z
                {
                    int n = m % 52;
                    char c = (n < 26 ? (char)('A' + n) : (char)('a' + (n - 26)));
                    ret.Append(c);

                    m /= 52;
                }
            }
            return ret.ToString();
        }

        #endregion

        public RijndaelManaged CreateRijndael()
        {
            RijndaelManaged ret = new RijndaelManaged();
            byte[] key = new byte[ret.KeySize / 8];
            this.Random.NextBytes(key);  // random key
            ret.Key = key;
            byte[] iv = new byte[ret.BlockSize / 8];
            this.Random.NextBytes(iv);  // random IV
            ret.IV = iv;
            return ret;
        }
    }
}
