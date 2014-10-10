using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TFTP
{
    /// <summary>
    /// Contains useful static helper methods
    /// </summary>
    static class Helper
    {
        //Thank you StackOverflow! http://stackoverflow.com/questions/472906/converting-a-string-to-byte-array
        /// <summary>
        /// Converts string to byte array
        /// </summary>
        /// <param name="str">string to convert</param>
        /// <returns></returns>
        public static byte[] GetBytes(string str)
        {
            byte[] bytes = new byte[str.Length * sizeof(char)];
            System.Buffer.BlockCopy(str.ToCharArray(), 0, bytes, 0, bytes.Length);
            return bytes;
        }

        /// <summary>
        /// Converts byte array to string
        /// </summary>
        /// <param name="bytes">Byte array to convert</param>
        /// <returns></returns>
        public static string GetString(byte[] bytes)
        {
            return System.Text.Encoding.ASCII.GetString(bytes).Trim();
        }

        /// <summary>
        /// Returns subarray of given array
        /// </summary>
        /// <typeparam name="T">Object type of array</typeparam>
        /// <param name="data">Original array</param>
        /// <param name="index">Index to begin</param>
        /// <param name="length">Length of new array</param>
        /// <returns></returns>
        public static T[] SubArray<T>(this T[] data, int index, int length)
        {
            T[] result = new T[length];
            Array.Copy(data, index, result, 0, length);
            return result;
        }
    }
}
