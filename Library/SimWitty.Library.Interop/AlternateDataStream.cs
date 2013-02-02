// <copyright file="AlternateDataStream.cs" company="SimWitty (http://www.simwitty.org)">
//     Copyright © 2013 and distributed under the BSD license.
// </copyright>

namespace SimWitty.Library.Interop
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.Runtime.InteropServices;
    
    /// <summary>
    /// Alternate Data Stream interface wraps Win32 API calls for reading and writing to NTFS ADS.
    /// </summary>
    public static class AlternateDataStream
    {
        // How To Use NTFS Alternate Data Streams (Article ID: 105763)
        // http://support.microsoft.com/default.aspx?scid=kb;en-us;105763

        /// <summary>
        /// Read bytes from an NTFS Alternate Data Stream (ADS)
        /// </summary>
        /// <param name="fileName">Full path to the file on an NTFS formatted local volume</param>
        /// <param name="streamName">ADS stream name in the file</param>
        /// <returns>All bytes read from the stream</returns>
        public static byte[] Read(string fileName, string streamName)
        {
            // Name the stream
            string streamname = string.Format(CultureInfo.CurrentCulture, "{0}:{1}", fileName, streamName);

            // Get a handle for reading stream 
            IntPtr handle = NativeMethods.CreateFile(
                streamname,
                (uint)NativeMethods.FileFlags.Generic_Read,
                (uint)NativeMethods.FileFlags.File_Share_Read,
                IntPtr.Zero,
                (uint)NativeMethods.FileFlags.Open_Existing,
                0,
                IntPtr.Zero);

            // Did we get the handle? -1 or MaxValue is returned if not.
            if (handle == (IntPtr)(-1)) return null;
            if (handle == (IntPtr)int.MaxValue) return null;

            // Get the file length
            uint length = NativeMethods.GetFileSize(handle, IntPtr.Zero);

            // Read the entire file
            byte[] result = new byte[length];
            uint bytesRead = uint.MinValue;
            bool success = NativeMethods.ReadFile(handle, result, length, ref bytesRead, IntPtr.Zero);

            if (!success)
            {
                // The write failed, so release the handle and throw an error
                NativeMethods.CloseHandle(handle);
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            // Release the file handle
            NativeMethods.CloseHandle(handle);
            return result;
        }

        /// <summary>
        /// Get the size of the Alternate Data Stream
        /// </summary>
        /// <param name="fileName">Full path to the file on an NTFS formatted local volume</param>
        /// <param name="streamName">ADS stream name in the file</param>
        /// <returns>All bytes in the stream</returns>
        public static uint FileStreamSize(string fileName, string streamName)
        {
            // Name the stream
            string streamname = string.Format(CultureInfo.CurrentCulture, "{0}:{1}", fileName, streamName);

            // Get a handle for reading stream 
            IntPtr handle = NativeMethods.CreateFile(
                streamname,
                (uint)NativeMethods.FileFlags.Generic_Read,
                (uint)NativeMethods.FileFlags.File_Share_Read,
                IntPtr.Zero,
                (uint)NativeMethods.FileFlags.Open_Existing,
                0,
                IntPtr.Zero);

            // Determine the file size and then close the handle
            uint output = NativeMethods.GetFileSize(handle, IntPtr.Zero);
            NativeMethods.CloseHandle(handle);
            return output;
        }

        /// <summary>
        /// Write text to an NTFS Alternate Data Stream (ADS)
        /// </summary>
        /// <param name="fileName">Full path to the file on an NTFS formatted local volume</param>
        /// <param name="streamName">ADS stream name in the file</param>
        /// <param name="text">A Unicode text string to write to the stream</param>
        public static void Write(string fileName, string streamName, string text)
        {
            Write(fileName, streamName, System.Text.Encoding.Unicode.GetBytes(text));
        }

        /// <summary>
        /// Write bytes to an NTFS Alternate Data Stream (ADS)
        /// </summary>
        /// <param name="fileName">Full path to the file on an NTFS formatted local volume</param>
        /// <param name="streamName">ADS stream name in the file</param>
        /// <param name="bytes">The bytes to write to the stream</param>
        public static void Write(string fileName, string streamName, byte[] bytes)
        {
            // Name the stream
            string streamname = string.Format(CultureInfo.CurrentCulture, "{0}:{1}", fileName, streamName);

            // Get a handle for reading stream 
            IntPtr handle = NativeMethods.CreateFile(
                streamname,
                (uint)NativeMethods.FileFlags.Generic_Write,
                (uint)NativeMethods.FileFlags.File_Share_Write,
                IntPtr.Zero,
                (uint)NativeMethods.FileFlags.Create_Always,
                0,
                IntPtr.Zero);

            // Get the file length
            uint length = (uint)bytes.Length;

            // Write the entire byte array
            uint result = uint.MinValue;
            bool success = NativeMethods.WriteFile(handle, bytes, length, ref result, IntPtr.Zero);

            if (!success)
            {
                // The write failed, so release the handle and throw an error
                NativeMethods.CloseHandle(handle);
                throw new Win32Exception(Marshal.GetLastWin32Error());
            }

            // Release the file handle
            NativeMethods.CloseHandle(handle);
            return;
        }

        #region Win32 API interop definitions

        /// <summary>
        /// NativeMethods for interacting with NTFS ADS
        /// </summary>
        internal static class NativeMethods
        {
            /// <summary>
            /// Win32 API flags for file access
            /// </summary>
            [Flags]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1602:EnumerationItemsMustBeDocumented", Justification = "The flag names are self-explanatory.")]
            internal enum FileFlags : uint
            {
                Create_Always = 2,
                Create_New = 1,
                File_Share_Read = 1,
                File_Share_Write = 2,
                Generic_Read = 0x80000000,
                Generic_Write = 0x40000000,
                Open_Always = 4,
                Open_Existing = 3
            }

            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool CloseHandle(IntPtr hFile);

            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool WriteFile(IntPtr hFile, byte[] lpBuffer, uint nNumberOfBytesToWrite, ref uint lpNumberOfBytesWritten, IntPtr lpOverlapped);

            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            internal static extern IntPtr CreateFile(string filename, uint desiredAccess, uint shareMode, IntPtr attributes, uint creationDisposition, uint flagsAndAttributes, IntPtr templateFile);

            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            internal static extern uint GetFileSize(IntPtr hFile, IntPtr size);

            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool ReadFile(IntPtr hFile, byte[] buffer, uint byteToRead, ref uint bytesRead, IntPtr lpOverlapped);
        }
        #endregion
    }
}
