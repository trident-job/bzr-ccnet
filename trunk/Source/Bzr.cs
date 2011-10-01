// Bazaar VCS Plugin for CruiseControl.NET
// 2011 - trident.job@gmail.com
// http://code.google.com/p/bzr-ccnet/ 
//
//Copyright (C) 2007 Sandy Dunlop (sandy@sorn.net)
//
//This program is free software; you can redistribute it and/or
//modify it under the terms of the GNU General Public License
//as published by the Free Software Foundation; either version 2
//of the License, or (at your option) any later version.
//
//This program is distributed in the hope that it will be useful,
//but WITHOUT ANY WARRANTY; without even the implied warranty of
//MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//GNU General Public License for more details.
//
//You should have received a copy of the GNU General Public License
//along with this program; if not, write to the Free Software
//Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.

using System;
using System.Globalization;
using System.IO;
using Exortech.NetReflector;
using ThoughtWorks.CruiseControl.Core.Config;
using ThoughtWorks.CruiseControl.Core.Util;

namespace ThoughtWorks.CruiseControl.Core.Sourcecontrol
{
	[ReflectorType("bzr")]
	public class Bzr : ProcessSourceControl
	{
		private IFileSystem _fileSystem;
		public const string DefaultExecutable = "bzr";
		public static readonly string BzrLogDateFormat = "yyyy-MM-dd,HH:mm:ss";

        #region Constructors
		public Bzr(ProcessExecutor executor, IHistoryParser parser, IFileSystem fileSystem)
            : base(parser, executor)
		{
			_fileSystem = fileSystem;
		}

		public Bzr()
		    : this(new ProcessExecutor(), new BzrHistoryParser(), new SystemIoFileSystem())
		{
		}
		#endregion

        #region Properties
		[ReflectorProperty("autoGetSource", Required = false)]
		public bool AutoGetSource = true;

		[ReflectorProperty("branchUrl", Required = false)]
		public string BranchUrl;

		[ReflectorProperty("executable", Required = false)]
		public string Executable = DefaultExecutable;

		[ReflectorProperty("tagOnSuccess", Required = false)]
		public bool TagOnSuccess = false;

		[ReflectorProperty("webUrlBuilder", InstanceTypeKey="type", Required = false)]
		public IModificationUrlBuilder UrlBuilder;

		[ReflectorProperty("workingDirectory", Required = false)]
		public string WorkingDirectory;
        #endregion
        
		public string FormatCommandDate(DateTime date)
		{
			//Bazaar uses this style of dates in its logs: 2007-09-26,17:29:09
			//return date.ToUniversalTime().ToString(BzrLogDateFormat, CultureInfo.InvariantCulture);
			return date.ToLocalTime().ToString(BzrLogDateFormat, CultureInfo.InvariantCulture);
		}

		public override Modification[] GetModifications(IIntegrationResult from, IIntegrationResult to)
		{
			ProcessResult result = Execute(NewHistoryProcessInfo(from, to));
			Modification[] modifications = ParseModifications(result, from.StartTime, to.StartTime);
			if (UrlBuilder != null)
			{
				UrlBuilder.SetupModification(modifications);
			}
			return modifications;
		}

		public override void LabelSourceControl(IIntegrationResult result)
		{
			if (TagOnSuccess && result.Succeeded)
			{
				Execute(NewTagProcessInfo(result));
			}
		}

		public override void GetSource(IIntegrationResult result)
		{
			if (! AutoGetSource) return;

			if (DoesBzrDirectoryExist(result))
			{
				UpdateSource(result);
			}
			else
			{
				CheckoutSource(result);
			}
		}

		private void CheckoutSource(IIntegrationResult result)
		{
			// Netx line Commented as it doesn't compile
            //if (StringUtil.IsBlank(BranchUrl))
			
            //	throw new ConfigurationException("<branchUrl> configuration element must be specified in order to automatically checkout source from Bazaar.");
			Execute(NewCheckoutProcessInfo(result));
		}

		private void UpdateSource(IIntegrationResult result)
		{
			Execute(NewGetSourceProcessInfo(result));
		}

		private bool DoesBzrDirectoryExist(IIntegrationResult result)
		{
			string bzrDirectory = Path.Combine(result.BaseFromWorkingDirectory(WorkingDirectory.Trim()), ".bzr");
			return _fileSystem.DirectoryExists(bzrDirectory);
		}

		private void AppendRevision(ProcessArgumentBuilder buffer, int revision)
		{
			buffer.AppendIf(revision > 0, "-r {0}", revision.ToString());
		}

        #region command-executors
		private ProcessInfo NewCheckoutProcessInfo(IIntegrationResult result)
		{
			ProcessArgumentBuilder buffer = new ProcessArgumentBuilder();
			buffer.AppendArgument("checkout");
			buffer.AppendArgument("--lightweight");
			buffer.AppendArgument(BranchUrl.Trim());
			buffer.AppendArgument(result.BaseFromWorkingDirectory(WorkingDirectory));
			return NewProcessInfo(buffer.ToString(), result);
		}

		private ProcessInfo NewGetSourceProcessInfo(IIntegrationResult result)
		{
			ProcessArgumentBuilder buffer = new ProcessArgumentBuilder();
			buffer.Append("update");
			//AppendRevision(buffer, StringTo result.LastChangeNumber);
			return NewProcessInfo(buffer.ToString(), result);
		}

		private ProcessInfo NewTagProcessInfo(IIntegrationResult result)
		{
			ProcessArgumentBuilder buffer = new ProcessArgumentBuilder();
			buffer.AppendArgument("tag");
			//AppendRevision(buffer, result.LastChangeNumber);
			buffer.AddArgument(result.Label);
			return NewProcessInfo(buffer.ToString(), result);
		}

		private ProcessInfo NewHistoryProcessInfo(IIntegrationResult from, IIntegrationResult to)
		{
			ProcessArgumentBuilder buffer = new ProcessArgumentBuilder();
			buffer.AddArgument("log");
			//Just ask for the last revision. This is enough to check if we need to update the working tree.
			buffer.AppendArgument(string.Format("-r -1", FormatCommandDate(from.StartTime), FormatCommandDate(to.StartTime)));
			buffer.AddArgument(BranchUrl.Trim());
			return NewProcessInfo(buffer.ToString(), to);
		}

		private ProcessInfo NewProcessInfo(string args, IIntegrationResult result)
		{
			return new ProcessInfo(Executable, args, result.BaseFromWorkingDirectory(WorkingDirectory));
		}
		#endregion
	}
}
