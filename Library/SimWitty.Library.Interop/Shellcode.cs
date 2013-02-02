// <copyright file="Shellcode.cs" company="SimWitty (http://www.simwitty.org)">
//     Copyright © 2013 and distributed under the BSD license.
// </copyright>

namespace SimWitty.Library.Interop
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using System.Runtime.InteropServices;

    /// <summary>
    /// The Shellcode class wraps Win32 API calls for loading and executing x86 shellcode via Kernel32.
    /// </summary>
    public class Shellcode
    {
        /// <summary>
        /// Internal shellcode byte array
        /// </summary>
        private byte[] shellcode;

        /// <summary>
        /// Internal pointer to the memory buffer containing the shellcode
        /// </summary>
        private IntPtr pointer = IntPtr.Zero;

        /// <summary>
        /// Initializes a new instance of the <see cref="Shellcode" /> class.
        /// </summary>
        /// <param name="codeInBytes">An array of bytes containing executable shellcode.</param>
        public Shellcode(byte[] codeInBytes)
        {
            // Pre-flight check - no nulls, please
            if (codeInBytes == null) throw new ApplicationException("The shellcode used to initialize this class cannot be null.");

            // Set the shellcode
            this.shellcode = codeInBytes;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Shellcode" /> class.
        /// </summary>
        /// <param name="codeInBase64">A Base64 encoded string containing executable shellcode.</param>
        public Shellcode(string codeInBase64)
        {
            // Pre-flight check - no empties
            if (codeInBase64 == null) throw new ApplicationException("The shellcode used to initialize this class cannot be null.");
            if (codeInBase64 == string.Empty) throw new ApplicationException("The shellcode used to initialize this class cannot be empty.");

            // Set the shellcode
            this.shellcode = Convert.FromBase64String(codeInBase64);
        }

        /// <summary>
        /// Gets the pointer to the memory buffer where the shellcode will be loaded and executed.
        /// </summary>
        public IntPtr Pointer
        {
            get
            {
                // Allocate Read/Write/Execute memory buffer
                if (this.pointer == IntPtr.Zero)
                {
                    UIntPtr size = (UIntPtr)(this.shellcode.Length + 1);
                    NativeMethods.AllocationType type = NativeMethods.AllocationType.RESERVE | NativeMethods.AllocationType.COMMIT;
                    this.pointer = NativeMethods.VirtualAlloc(IntPtr.Zero, size, type, NativeMethods.MemoryProtection.EXECUTE_READWRITE);
                }

                // If the allocation failed, throw the exception
                if (this.pointer == IntPtr.Zero) throw new ApplicationException("The shellcode could not be allocated in memory.");

                return this.pointer;
            }
        }

        /// <summary>
        /// Execute the shellcode loaded into the current class.
        /// </summary>
        public void Execute()
        {
            try
            {
                // Copy shellcode into the memory buffer
                Marshal.Copy(this.shellcode, 0, this.Pointer, this.shellcode.Length);

                // Get pointer to function and execute
                NativeMethods.ExecuteDelegate exe =
                    (NativeMethods.ExecuteDelegate)Marshal.GetDelegateForFunctionPointer(this.Pointer, typeof(NativeMethods.ExecuteDelegate));
                exe();
            }
            catch (AccessViolationException)
            {
                // Ignore:
                // Unhandled Exception: System.AccessViolationException: 
                // Attempted to read or write protected memory. This is often an indication that other memory is corrupt.
            }
            finally
            {
                // Free the memory buffer
                NativeMethods.VirtualFree(this.Pointer, 0, NativeMethods.FreeType.MEM_RELEASE);
            }
        }

        #region Win32 API interop definitions

        /// <summary>
        /// NativeMethods for interacting with NTFS ADS
        /// </summary>
        internal static class NativeMethods
        {
            /// <summary>
            /// The delegate controls the marshaling behavior signature passed as a function pointer to execute the shellcode.
            /// </summary>
            /// <returns>Execution status</returns>
            [UnmanagedFunctionPointerAttribute(CallingConvention.Cdecl)]
            public delegate int ExecuteDelegate();

            /// <summary>
            /// Kernel32 flags to reserve or commit a region of pages in the virtual address space of the calling process.
            /// </summary>
            [Flags]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1602:EnumerationItemsMustBeDocumented", Justification = "The flag names are self-explanatory.")]
            internal enum AllocationType : uint
            {
                COMMIT = 0x1000,
                RESERVE = 0x2000,
                RESET = 0x80000,
                LARGE_PAGES = 0x20000000,
                PHYSICAL = 0x400000,
                TOP_DOWN = 0x100000,
                WRITE_WATCH = 0x200000
            }

            /// <summary>
            /// Kernel32 flags to decommit, release, or both, a region of pages within the virtual address space of a specified process.
            /// </summary>
            [Flags]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1602:EnumerationItemsMustBeDocumented", Justification = "The flag names are self-explanatory.")]
            internal enum FreeType : uint
            {
                MEM_DECOMMIT = 0x4000,
                MEM_RELEASE = 0x8000
            }

            /// <summary>
            /// Kernel32 flags used to set the memory-protection options when allocating or protecting a page in memory. 
            /// </summary>
            [Flags]
            [System.Diagnostics.CodeAnalysis.SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1602:EnumerationItemsMustBeDocumented", Justification = "The flag names are self-explanatory.")]
            internal enum MemoryProtection : uint
            {
                EXECUTE = 0x10,
                EXECUTE_READ = 0x20,
                EXECUTE_READWRITE = 0x40,
                EXECUTE_WRITECOPY = 0x80,
                NOACCESS = 0x01,
                READONLY = 0x02,
                READWRITE = 0x04,
                WRITECOPY = 0x08,
                GUARD_Modifierflag = 0x100,
                NOCACHE_Modifierflag = 0x200,
                WRITECOMBINE_Modifierflag = 0x400
            }

            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            internal static extern IntPtr CreateThread(IntPtr threadAttributes, uint stackSize, IntPtr startAddress, IntPtr parameter, uint creationFlags, IntPtr threadId);

            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            internal static extern IntPtr VirtualAlloc(IntPtr startAddress, UIntPtr size, AllocationType allocationType, MemoryProtection protect);

            [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool VirtualFree(IntPtr startAddress, uint stackSize, FreeType freeType);
        }
        #endregion
    }
}