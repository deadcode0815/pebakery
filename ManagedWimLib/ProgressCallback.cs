﻿/*
    Licensed under LGPLv3

    Derived from wimlib's original header files
    Copyright (C) 2012, 2013, 2014 Eric Biggers

    C# Wrapper written by Hajin Jang
    Copyright (C) 2017-2018 Hajin Jang

    This file is free software; you can redistribute it and/or modify it under
    the terms of the GNU Lesser General Public License as published by the Free
    Software Foundation; either version 3 of the License, or (at your option) any
    later version.

    This file is distributed in the hope that it will be useful, but WITHOUT
    ANY WARRANTY; without even the implied warranty of MERCHANTABILITY or FITNESS
    FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public License for more
    details.

    You should have received a copy of the GNU Lesser General Public License
    along with this file; if not, see http://www.gnu.org/licenses/.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace ManagedWimLib
{
    #region ProgressCallback delegate
    public delegate CallbackStatus ProgressCallback(ProgressMsg msg, object info, object progctx);
    #endregion

    #region ManagedWimLibCallback
    internal class ManagedProgressCallback
    {
        private readonly ProgressCallback _callback;
        private readonly object _userData;

        internal NativeMethods.NativeProgressFunc NativeFunc { get; private set; }

        public ManagedProgressCallback(ProgressCallback callback, object userData)
        {
            _callback = callback ?? throw new ArgumentNullException("callback");
            _userData = userData;

            // Avoid GC by keeping ref here
            NativeFunc = NativeCallback;
        }

        private CallbackStatus NativeCallback(ProgressMsg msgType, IntPtr info, IntPtr progctx)
        {
            object pInfo = null;

            if (_callback != null)
            {
                switch (msgType)
                {
                    case ProgressMsg.WRITE_STREAMS:
                        pInfo = (ProgressInfo_WriteStreams)Marshal.PtrToStructure(info, typeof(ProgressInfo_WriteStreams));
                        break;
                    case ProgressMsg.SCAN_BEGIN:
                    case ProgressMsg.SCAN_DENTRY:
                    case ProgressMsg.SCAN_END:
                        pInfo = (ProgressInfo_Scan)Marshal.PtrToStructure(info, typeof(ProgressInfo_Scan));
                        break;
                    case ProgressMsg.EXTRACT_SPWM_PART_BEGIN:
                    case ProgressMsg.EXTRACT_IMAGE_BEGIN:
                    case ProgressMsg.EXTRACT_TREE_BEGIN:
                    case ProgressMsg.EXTRACT_FILE_STRUCTURE:
                    case ProgressMsg.EXTRACT_STREAMS:
                    case ProgressMsg.EXTRACT_METADATA:
                    case ProgressMsg.EXTRACT_TREE_END:
                    case ProgressMsg.EXTRACT_IMAGE_END:
                        pInfo = (ProgressInfo_Extract)Marshal.PtrToStructure(info, typeof(ProgressInfo_Extract));
                        break;
                    case ProgressMsg.RENAME:
                        pInfo = (ProgressInfo_Rename)Marshal.PtrToStructure(info, typeof(ProgressInfo_Rename));
                        break;
                    case ProgressMsg.UPDATE_BEGIN_COMMAND:
                    case ProgressMsg.UPDATE_END_COMMAND:
                        ProgressInfo_UpdateBase _base = (ProgressInfo_UpdateBase)Marshal.PtrToStructure(info, typeof(ProgressInfo_UpdateBase));
                        pInfo = _base.ToManaged();
                        break;
                    case ProgressMsg.VERIFY_INTEGRITY:
                    case ProgressMsg.CALC_INTEGRITY:
                        pInfo = (ProgressInfo_Integrity)Marshal.PtrToStructure(info, typeof(ProgressInfo_Integrity));
                        break;
                    case ProgressMsg.SPLIT_BEGIN_PART:
                    case ProgressMsg.SPLIT_END_PART:
                        pInfo = (ProgressInfo_Split)Marshal.PtrToStructure(info, typeof(ProgressInfo_Split));
                        break;
                    case ProgressMsg.REPLACE_FILE_IN_WIM:
                        pInfo = (ProgressInfo_Replace)Marshal.PtrToStructure(info, typeof(ProgressInfo_Replace));
                        break;
                    case ProgressMsg.WIMBOOT_EXCLUDE:
                        pInfo = (ProgressInfo_WimBootExclude)Marshal.PtrToStructure(info, typeof(ProgressInfo_WimBootExclude));
                        break;
                    case ProgressMsg.UNMOUNT_BEGIN:
                        pInfo = (ProgressInfo_Unmount)Marshal.PtrToStructure(info, typeof(ProgressInfo_Unmount));
                        break;
                    case ProgressMsg.DONE_WITH_FILE:
                        pInfo = (ProgressInfo_DoneWithFile)Marshal.PtrToStructure(info, typeof(ProgressInfo_DoneWithFile));
                        break;
                    case ProgressMsg.BEGIN_VERIFY_IMAGE:
                    case ProgressMsg.END_VERIFY_IMAGE:
                        pInfo = (ProgressInfo_VerifyImage)Marshal.PtrToStructure(info, typeof(ProgressInfo_VerifyImage));
                        break;
                    case ProgressMsg.VERIFY_STREAMS:
                        pInfo = (ProgressInfo_VerifyStreams)Marshal.PtrToStructure(info, typeof(ProgressInfo_VerifyStreams));
                        break;
                    case ProgressMsg.TEST_FILE_EXCLUSION:
                        pInfo = (ProgressInfo_TestFileExclusion)Marshal.PtrToStructure(info, typeof(ProgressInfo_TestFileExclusion));
                        break;
                    case ProgressMsg.HANDLE_ERROR:
                        pInfo = (ProgressInfo_HandleError)Marshal.PtrToStructure(info, typeof(ProgressInfo_HandleError));
                        break;
                }

                return _callback(msgType, pInfo, _userData);
            }
            else
            {
                return CallbackStatus.CONTINUE;
            }
        }
    }
    #endregion

    #region ProgressInfo
    /// <summary>
    /// Valid on the message WRITE_STREAMS.  
    /// This is the primary message for tracking the progress of writing a WIM file.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct ProgressInfo_WriteStreams
    {
        /// <summary>
        /// An upper bound on the number of bytes of file data that will be written.
        /// This number is the uncompressed size; the actual size may be lower due to compression. 
        /// In addition, this number may decrease over time as duplicated file data is discovered.
        /// </summary>
        public ulong TotalBytes;
        /// <summary>
        /// An upper bound on the number of distinct file data "blobs" that will be written. 
        /// This will often be similar to the "number of files", but for several reasons 
        /// (hard links, named data streams, empty files, etc.) it can be different. 
        /// In addition, this number may decrease over time as duplicated file data is discovered.
        /// </summary>
        public ulong TotalStreams;
        /// <summary>
        /// The number of bytes of file data that have been written so far. 
        /// This starts at 0 and ends at TotalBytes.
        /// This number is the uncompressed size; the actual size may be lower due to compression.
        /// </summary>
        public ulong CompletedBytes;
        /// <summary>
        /// The number of distinct file data "blobs" that have been written so far. 
        /// This starts at 0 and ends at total_streams.
        /// </summary>
        public ulong CompletedStreams;
        /// <summary>
        /// The number of threads being used for data compression; or, if no compression is being performed, this will be 1.
        /// </summary>
        public uint NumThreads;
        /// <summary>
        /// The compression type being used, as one of the CompressionType enums. 
        /// </summary>
        public CompressionType CompressionType;
        /// <summary>
        /// The number of on-disk WIM files from which file data is being exported into the output WIM file.
        /// This can be 0, 1, or more than 1, depending on the situation.
        /// </summary>
        public uint TotalParts;
        /// <summary>
        /// This is currently broken and will always be 0. 
        /// </summary>
        public uint CompletedParts;
    }

    /// <summary>
    /// Valid on messages SCAN_BEGIN, SCAN_DENTRY, and SCAN_END.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct ProgressInfo_Scan
    {
        /// <summary>
        /// Dentry scan status, valid on SCAN_DENTRY.
        /// </summary>
        public enum ScanDentryStatus : uint
        {
            /// <summary>
            /// File looks okay and will be captured.
            /// </summary>
            OK = 0,
            /// <summary>
            /// File is being excluded from capture due to the capture configuration.
            /// </summary>
            EXCLUDED = 1,
            /// <summary>
            /// File is being excluded from capture due to being of an unsupported type. 
            /// </summary>
            UNSUPPORTED = 2,
            /// <summary>
            /// The file is an absolute symbolic link or junction that points into the capture directory, and
            /// reparse-point fixups are enabled, so its target is being adjusted. 
            /// (Reparse point fixups can be disabled with the flag AddFlags.NORPFIX.)
            /// </summary>
            FIXED_SYMLINK = 3,
            /// <summary>
            /// Reparse-point fixups are enabled, but the file is an absolute symbolic link or junction that does not
            /// point into the capture directory, so its target is <b>not</b> being adjusted.
            /// </summary>
            NOT_FIXED_SYMLINK = 4,
        }

        /// <summary>
        /// Top-level directory being scanned; or, when capturing an NTFS volume with AddFlags.NTFS, 
        /// this is instead the path to the file or block device that contains the NTFS volume being scanned. 
        /// </summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        public string Source;
        /// <summary>
        /// Path to the file (or directory) that has been scanned, valid on SCAN_DENTRY.
        /// When capturing an NTFS volume with ::WIMLIB_ADD_FLAG_NTFS, this path will be relative to the root of the NTFS volume. 
        /// </summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        public string CurPath;
        /// <summary>
        /// Dentry scan status, valid on SCAN_DENTRY. 
        /// </summary>
        public ScanDentryStatus Status;
        /// <summary>
        /// - wim_target_path
        /// Target path in the image.  Only valid on messages
        /// SCAN_BEGIN and
        /// SCAN_END.
        /// 
        /// - symlink_target
        /// For SCAN_DENTRY and a status of WIMLIB_SCAN_DENTRY_FIXED_SYMLINK or WIMLIB_SCAN_DENTRY_NOT_FIXED_SYMLINK,
        /// this is the target of the absolute symbolic link or junction.
        /// </summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        public string WimTargetPathSymlinkTarget;
        /// <summary>
        /// The number of directories scanned so far, not counting excluded/unsupported files.
        /// </summary>
        public ulong NumDirsScanned;
        /// <summary>
        /// The number of non-directories scanned so far, not counting excluded/unsupported files.
        /// </summary>
        public ulong NumNonDirsScanned;
        /// <summary>
        /// The number of bytes of file data detected so far, not counting excluded/unsupported files.
        /// </summary>
        public ulong NumBytesScanned;
    }

    /// <summary>
    /// Valid on messages
    /// EXTRACT_SPWM_PART_BEGIN,
    /// EXTRACT_IMAGE_BEGIN,
    /// EXTRACT_TREE_BEGIN,
    /// EXTRACT_FILE_STRUCTURE,
    /// EXTRACT_STREAMS,
    /// EXTRACT_METADATA,
    /// EXTRACT_TREE_END, and
    /// EXTRACT_IMAGE_END.
    ///
    /// Note: most of the time of an extraction operation will be spent extracting file data, and the application will receive
    /// EXTRACT_STREAMS during this time. Using completed_bytes and @p total_bytes, the application can calculate a
    /// percentage complete.  However, there is no way for applications to know which file is currently being extracted.
    /// This is by design because the best way to complete the extraction operation is not necessarily file-by-file.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct ProgressInfo_Extract
    {
        /// <summary>
        /// The 1-based index of the image from which files are being extracted.
        /// </summary>
        public uint Image;
        /// <summary>
        /// Extraction flags being used. 
        /// </summary>
        public uint ExtractFlags;
        /// <summary>
        /// If the WimStruct from which the extraction being performed has a backing file, 
        /// then this is an absolute path to that backing file. Otherwise, this is null.
        /// </summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        public string WimFileName;
        /// <summary>
        /// Name of the image from which files are being extracted, or the empty string if the image is unnamed.
        /// </summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        public string ImageName;
        /// <summary>
        /// Path to the directory or NTFS volume to which the files are being extracted.
        /// </summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        public string Target;
        /// <summary>
        /// Reserved.
        /// </summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        private string Reserved;
        /// <summary>
        /// The number of bytes of file data that will be extracted. 
        /// </summary>
        public ulong TotalBytes;
        /// <summary>
        /// The number of bytes of file data that have been extracted so far.
        /// This starts at 0 and ends at TotalBytes.
        /// </summary>
        public ulong CompletedBytes;
        /// <summary>
        /// The number of file streams that will be extracted. This will often be similar to the "number of files", 
        /// but for several reasons (hard links, named data streams, empty files, etc.) it can be different.
        /// </summary>
        public ulong TotalStreams;
        /// <summary>
        /// The number of file streams that have been extracted so far.
        /// This starts at 0 and ends at @p total_streams.
        /// </summary>
        public ulong CompletedStreams;
        /// <summary>
        /// Currently only used for
        /// EXTRACT_SPWM_PART_BEGIN. 
        /// </summary>
        public uint PartNumber;
        /// <summary>
        /// Currently only used for
        /// EXTRACT_SPWM_PART_BEGIN.
        /// </summary>
        public uint TotalParts;
        /// <summary>
        /// Currently only used for
        /// EXTRACT_SPWM_PART_BEGIN.
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        public byte[] Guid;
        /// <summary>
        /// For EXTRACT_FILE_STRUCTURE and EXTRACT_METADATA messages, 
        /// this is the number of files that have been processed so far.
        /// Once the corresponding phase of extraction is complete, this value will be equal to EndFileCount. 
        /// </summary>
        public ulong CurrentFileCount;
        /// <summary>
        /// For EXTRACT_FILE_STRUCTURE and EXTRACT_METADATA messages, 
        /// this is total number of files that will be processed.
        /// 
        /// This number is provided for informational purposes only, e.g. for a progress bar. 
        /// This number will not necessarily be equal to the number of files actually being extracted.
        /// This is because extraction backends are free to implement an extraction algorithm that might be more efficient than
        /// processing every file in the "extract file structure" and "extract file metadata" phases.
        /// For example, the current implementation of the UNIX extraction backend will create
        /// files on-demand during the "extract file data" phase.
        /// Therefore, when using that particular extraction backend, EndFileCount will only include directories and empty files.
        /// </summary>
        public ulong EndFileCount;
    }

    /// <summary>
    /// Valid on messages RENAME.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct ProgressInfo_Rename
    {
        /// <summary>
        /// Name of the temporary file that the WIM was written to.
        /// </summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        public string From;
        /// <summary>
        /// Name of the original WIM file to which the temporary file is
        /// being renamed.
        /// </summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        public string To;
    }

    /// <summary>
    /// Valid on messages UPDATE_BEGIN_COMMAND and UPDATE_END_COMMAND.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct ProgressInfo_Update
    {
        /// <summary>
        /// Name of the temporary file that the WIM was written to.
        /// </summary>
        public UpdateCommand Command;
        /// <summary>
        /// Number of update commands that have been completed so far.
        /// </summary>
        public uint CompletedCommands;
        /// <summary>
        /// Number of update commands that are being executed as part of this call to Wim.UpdateImage().
        /// </summary>
        public uint TotalCommands;
    }

    /// <summary>
    /// Valid on messages UPDATE_BEGIN_COMMAND and UPDATE_END_COMMAND.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    internal struct ProgressInfo_UpdateBase
    {
        /// <summary>
        /// Name of the temporary file that the WIM was written to.
        /// </summary>
        private IntPtr CommandPtr;
        private UpdateCommand32 Cmd32 => (UpdateCommand32)Marshal.PtrToStructure(CommandPtr, typeof(UpdateCommand32));
        private UpdateCommand64 Cmd64 => (UpdateCommand64)Marshal.PtrToStructure(CommandPtr, typeof(UpdateCommand64));
        public UpdateCommand Command
        {
            get
            {
                switch (IntPtr.Size)
                {
                    case 4:
                        return Cmd32.ToManagedClass();
                    case 8:
                        return Cmd64.ToManagedClass();
                    default:
                        throw new PlatformNotSupportedException();
                }
            }
        }
        /// <summary>
        /// Number of update commands that have been completed so far.
        /// </summary>
        public uint CompletedCommands;
        /// <summary>
        /// Number of update commands that are being executed as part of this call to Wim.UpdateImage().
        /// </summary>
        public uint TotalCommands;

        public ProgressInfo_Update ToManaged()
        {
            return new ProgressInfo_Update()
            {
                Command = this.Command,
                CompletedCommands = this.CompletedCommands,
                TotalCommands = this.TotalCommands,
            };
        }
    }

    /// <summary>
    /// Valid on messages VERIFY_INTEGRITY and CALC_INTEGRITY.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct ProgressInfo_Integrity
    {
        /// <summary>
        /// The number of bytes in the WIM file that are covered by integrity checks.
        /// </summary>
        public ulong TotalBytes;
        /// <summary>
        /// The number of bytes that have been checksummed so far.
        /// This starts at 0 and ends at TotalBytes.
        /// </summary>
        public ulong CompletedBytes;
        /// <summary>
        /// The number of individually checksummed "chunks" the integrity-checked region is divided into.
        /// </summary>
        public uint TotalChunks;
        /// <summary>
        /// The number of chunks that have been checksummed so far.
        /// This starts at 0 and ends at TotalChunks.
        /// </summary>
        public uint CompletedChunks;
        /// <summary>
        /// The size of each individually checksummed "chunk" in the integrity-checked region.
        /// </summary>
        public uint ChunkSize;
        /// <summary>
        /// For VERIFY_INTEGRITY messages, this is the path to the WIM file being checked.
        /// </summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        public string FileName;
    }

    /// <summary>
    /// Valid on messages SPLIT_BEGIN_PART and
    /// ::SPLIT_END_PART.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct ProgressInfo_Split
    {
        /// <summary>
        /// Total size of the original WIM's file and metadata resources (compressed).
        /// </summary>
        public ulong TotalBytes;
        /// <summary>
        /// Number of bytes of file and metadata resources that have been copied out of the original WIM so far.
        /// Will be 0 initially, and equal to TotalBytes at the end.
        /// </summary>
        public ulong CompletedBytes;
        /// <summary>
        /// Number of the split WIM part that is about to be started (SPLIT_BEGIN_PART) or has just been finished (SPLIT_END_PART).
        /// </summary>
        public uint CurPartNumber;
        /// <summary>
        /// Total number of split WIM parts that are being written.
        /// </summary>
        public uint TotalParts;
        /// <summary>
        /// Name of the split WIM part that is about to be started (SPLIT_BEGIN_PART) or has just been finished (SPLIT_END_PART).
        /// Since wimlib v1.7.0, the library user may change this when receiving SPLIT_BEGIN_PART in order to
        /// cause the next split WIM part to be written to a different location.
        /// </summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        public string PartName;
    }

    /// <summary>
    /// Valid on messages REPLACE_FILE_IN_WIM
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct ProgressInfo_Replace
    {
        /// <summary>
        /// Path to the file in the image that is being replaced.
        /// </summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        public string PathInWim;
    }

    /// <summary>
    /// Valid on messages WIMBOOT_EXCLUDE 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct ProgressInfo_WimBootExclude
    {
        /// <summary>
        /// Path to the file in the image.
        /// </summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        public string PathInWim;
        /// <summary>
        /// Path to which the file is being extracted .
        /// </summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        public string ExtractionInWim;
    }

    /// <summary>
    /// Valid on messages UNMOUNT_BEGIN.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct ProgressInfo_Unmount
    {
        /// <summary>
        /// Path to directory being unmounted.
        /// </summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        public string MountPoint;
        /// <summary>
        /// Path to WIM file being unmounted.
        /// </summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        public string MountedWim;
        /// <summary>
        /// 1-based index of image being unmounted.
        /// </summary>
        public uint MountedImage;
        /// <summary>
        /// Flags that were passed to Wim.MountImage() when the mountpoint was set up.
        /// </summary>
        public uint MountFlags;
        /// <summary>
        /// Flags passed to Wim.MountImage().
        /// </summary>
        public uint UnmountFlags;
    }

    /// <summary>
    /// Valid on messages DONE_WITH_FILE.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct ProgressInfo_DoneWithFile
    {
        /// <summary>
        /// Path to the file whose data has been written to the WIM file,
        /// or is currently being asynchronously compressed in memory,
        /// and therefore is no longer needed by wimlib.
        ///
        /// WARNING: The file data will not actually be accessible in the WIM file until the WIM file has been completely written.
        /// Ordinarily you should not treat this message as a green light to go ahead and delete the specified file, since
        /// that would result in data loss if the WIM file cannot be successfully created for any reason.
        ///
        /// If a file has multiple names (hard links), DONE_WITH_FILE will only be received for one name.
        /// Also, this message will not be received for empty files or reparse points (or symbolic links),
        /// unless they have nonempty named data streams.
        /// </summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        public string PathToFile;
    }

    /// <summary>
    /// Valid on messages BEGIN_VERIFY_IMAGE and END_VERIFY_IMAGE. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct ProgressInfo_VerifyImage
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string WimFile;
        public uint TotalImages;
        public uint CurrentImage;
    }

    /// <summary>
    /// Valid on messages VERIFY_STREAMS.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct ProgressInfo_VerifyStreams
    {
        [MarshalAs(UnmanagedType.LPWStr)]
        public string WimFile;
        public uint TotalStreams;
        public uint TotalBytes;
        public uint CurrentStreams;
        public uint CurrentBytes;
    }

    /// <summary>
    /// Valid on messages TEST_FILE_EXCLUSION.
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct ProgressInfo_TestFileExclusion
    {
        /// <summary>
        /// Path to the file for which exclusion is being tested.
        ///
        /// UNIX capture mode:  The path will be a standard relative or absolute UNIX filesystem path.
        ///
        /// NTFS-3G capture mode:  The path will be given relative to the root of the NTFS volume, with a leading slash.
        ///
        /// Windows capture mode:  The path will be a Win32 namespace path to the file.
        /// </summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        public string Path;
        /// <summary>
        /// Indicates whether the file or directory will be excluded from capture or not. 
        /// This will be false by default.
        /// The progress function can set this to true if it decides that the file needs to be excluded.
        /// </summary>
        public bool WillExclude;
    }

    /// <summary>
    /// Valid on messages HANDLE_ERROR. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct ProgressInfo_HandleError
    {
        /// <summary>
        /// Path to the file for which the error occurred, or NULL if not relevant.
        /// </summary>
        [MarshalAs(UnmanagedType.LPWStr)]
        public string Path;
        /// <summary>
        /// The wimlib error code associated with the error.
        /// </summary>
        public int ErrorCode;
        /// <summary>
        /// Indicates whether the error will be ignored or not.
        /// This will be false by default; the progress function may set it to true.
        /// </summary>
        public bool WillIgnore;
    }
    #endregion
}
