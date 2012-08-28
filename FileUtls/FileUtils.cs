#region License
/*
Permission is hereby granted, free of
charge, to any person obtaining a copy of
this software and associated documentation
 files (the "Software"), to deal in the
Software without restriction, including
without limitation the rights to use, copy,
 modify, merge, publish, distribute,
sublicense, and/or sell copies of the
Software, and to permit persons to whom the
 Software is furnished to do so, subject to
the following conditions:


The above copyright notice and this
permission notice shall be included in all
 copies or substantial portions of the
Software.


THE SOFTWARE IS PROVIDED "AS IS", WITHOUT
WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
INCLUDING BUT NOT LIMITED TO THE WARRANTIES
 OF MERCHANTABILITY, FITNESS FOR A
PARTICULAR PURPOSE AND NONINFRINGEMENT. IN
NO EVENT SHALL THE AUTHORS OR COPYRIGHT
HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR
 OTHER LIABILITY, WHETHER IN AN ACTION OF
CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR
 THE USE OR OTHER DEALINGS IN THE SOFTWARE.

 * User: Eddie
 * Date: 8/28/2012
 * Time: 2:58 PM
 * 
 */
#endregion
using System;
using System.Collections.Generic;
using FileLockInfo;
using System.Diagnostics;

namespace System.IO
{
	/// <summary>
	/// Extra functions that are not in
	/// System.IO.File class
	/// </summary>
	public static class FileUtils
	{
		/// <summary>
		/// Force delete a file. If the file isnt locked, it will be deleted normally
		/// If the file is locked, all processes that are locking the file will be
		/// terminated and then the file will be deleted.
		/// If a process locking the file cant be terminated, then the file will be
		/// deleted after reboot.
		/// </summary>
		/// <param name="file">The file to delete</param>
		/// <param name="onFailsetReboot">If the file cant be deleted, set the file to be deleted on reboot</param>
		/// <returns>The result of the force delete</returns>
		public static Result forceDelete(string file, bool onFailsetReboot) {
			if (!File.Exists(file))
				return Result.File_Not_Found;
			FileInfo fi = new FileInfo(file);
			return forceDelete(fi, onFailsetReboot);
		}
		/// <summary>
		/// Force delete a file. If the file isnt locked, it will be deleted normally
		/// If the file is locked, all processes that are locking the file will be
		/// terminated and then the file will be deleted.
		/// If a process locking the file cant be terminated, then the file will be
		/// deleted after reboot.
		/// </summary>
		/// <param name="file">The file to delete</param>
		/// <param name="onFailsetReboot">If the file cant be deleted, set the file to be deleted on reboot</param>
		/// <returns>The result of the force delete</returns>
		public static Result forceDelete(FileInfo file, bool onFailsetReboot) {
			if (Win32Processes.GetProcessesLockingFile(file.FullName).Count > 0) {
				List<Process> temp = Win32Processes.GetProcessesLockingFile(file.FullName);
				foreach (Process current in temp) {
					if (current.Id == Process.GetCurrentProcess().Id)
						continue;
					try {
						current.Kill();
					} catch {
						break;
					}
				}
				if (temp.Count > 0) {
					if (!onFailsetReboot)
						return Result.Failed;
					if (!Native_Methods.NativeMethods.MoveFileEx(file.FullName, null, Native_Methods.MoveFileFlags.DelayUntilReboot))
						return Result.Failed_Unable_to_schedule_for_reboot;
					return Result.After_Reboot;
				}
			}
			try {
				file.Delete();
				return Result.Deleted;
			} catch (IOException) {
				if (!onFailsetReboot)
					return Result.Failed;
				if (!Native_Methods.NativeMethods.MoveFileEx(file.FullName, null, Native_Methods.MoveFileFlags.DelayUntilReboot))
					return Result.Failed_Unable_to_schedule_for_reboot;
				return Result.After_Reboot;
			} catch (Security.SecurityException) {
				if (!onFailsetReboot)
					return Result.Failed_Security_Exception;
				if (!Native_Methods.NativeMethods.MoveFileEx(file.FullName, null, Native_Methods.MoveFileFlags.DelayUntilReboot))
					return Result.Failed_Unable_to_schedule_for_reboot;
				return Result.After_Reboot;
			} catch (UnauthorizedAccessException) {
				if (!onFailsetReboot)
					return Result.Failed_UnauthorizedAccess_Exception;
				if (!Native_Methods.NativeMethods.MoveFileEx(file.FullName, null, Native_Methods.MoveFileFlags.DelayUntilReboot))
					return Result.Failed_Unable_to_schedule_for_reboot;
				return Result.After_Reboot;
			}
		}
	}
	
	public enum Result : int
	{
		/// <summary>
		/// This is returned if the file was deleted
		/// </summary>
		Deleted = 0,
		/// <summary>
		/// This is returned if the file couldnt be deleted
		/// because there was a Security Exception
		/// </summary>
		Failed_Security_Exception = 1,
		/// <summary>
		/// This is returned if the file couldnt be deleted
		/// but was scheduled to be deleted after reboot
		/// </summary>
		After_Reboot = 2,
		/// <summary>
		/// This is returned if the file was moved
		/// </summary>
		Moved = 3,
		/// <summary>
		/// This is returned if the action was a success
		/// </summary>
		Success = 4,
		/// <summary>
		/// This is returned if the file could not
		/// be found
		/// </summary>
		File_Not_Found = 5,
		/// <summary>
		/// This is called if the file couldnt be deleted
		/// because there was an unauthorizedAccess Exception
		/// </summary>
		Failed_UnauthorizedAccess_Exception = 6,
		/// <summary>
		/// This is called if the file couldnt be deleted
		/// AND couldnt be scheduled to be deleted after reboot
		/// </summary>
		Failed_Unable_to_schedule_for_reboot = 7,
		/// <summary>
		/// This is returned if the action failed
		/// </summary>
		Failed = 8
	}
}