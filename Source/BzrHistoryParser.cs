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
using System.Collections;
using System.Globalization;
using System.IO;
using System.Text;
using ThoughtWorks.CruiseControl.Core.Util;

namespace ThoughtWorks.CruiseControl.Core.Sourcecontrol
{
	public class BzrHistoryParser : IHistoryParser
	{
		private static readonly string BZR_REVISION_DELIM = 
			"------------------------------------------------------------";
		private string _currentLine;
		private TextReader _bzrLog;

		public Modification[] Parse(TextReader bzrLog, DateTime from, DateTime to)
		{
		    _bzrLog = bzrLog;
			ArrayList mods = new ArrayList();
			while (null!=(_currentLine = _bzrLog.ReadLine()))
			{
			    if (_currentLine.StartsWith(BZR_REVISION_DELIM))
			    {
                      Modification rev = ParseRevision();
                      if (from.CompareTo(rev.ModifiedTime)<0)
                      {
                          mods.Add(rev);
                      }
			    }
			}
			return (Modification[]) mods.ToArray(typeof (Modification));
		}

		private Modification ParseRevision()
		{
			Modification modification = new Modification();
			_currentLine = _bzrLog.ReadLine();
			if (_currentLine.StartsWith("revno"))
			{
				modification.Version = _currentLine.Substring("revno:".Length).Trim();
				_currentLine = _bzrLog.ReadLine();
			}
			if (_currentLine.StartsWith("committer"))
			{
				modification.UserName = _currentLine.Substring("committer:".Length).Trim();
				_currentLine = _bzrLog.ReadLine();
			}
			if (_currentLine.StartsWith("branch nick"))
			{
				//string branchNick = _currentLine.Substring("branch nick:".Length).Trim();
				_currentLine = _bzrLog.ReadLine();
			}
			if (_currentLine.StartsWith("timestamp"))
			{
				string timestamp = _currentLine.Substring("timestamp:".Length).Trim();
				int s = timestamp.IndexOf(" ");
				modification.ModifiedTime = 
				    DateTime.Parse(timestamp.Substring(s+1,10) + "T"+timestamp.Substring(s+12,8));
				_currentLine = _bzrLog.ReadLine();
			}
			if (_currentLine.StartsWith("message"))
			{
				_currentLine = _bzrLog.ReadLine();
    			StringBuilder message = new StringBuilder();
    			while (_currentLine != null && !_currentLine.StartsWith(BZR_REVISION_DELIM))
    			{
    				if (message.Length > 0)
    				{
    					message.Append(Environment.NewLine);
    				}
    				message.Append(_currentLine);
    				_currentLine = _bzrLog.ReadLine();
    			}
				modification.Comment = message.ToString();
			}
			return modification;
		}
	}
}
