/*
 * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
 * User: Garraty
 * Date: 02.10.2015
 * Time: 20:54
 * 
 * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
 */
using System;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;
using MsgLib;

namespace ChatClient
{
	/// <summary>
	/// Description of MainForm_Checkers.
	/// </summary>
	public partial class MainForm
	{
		private bool NickNameCheck(ref string sNickName)
		{
			bool answ = true;
			char[] space = new char[1];
			space[0] = ' ';			
			sNickName = sNickName.Trim(space);
			if (sNickName == "")
				answ = false;
			return answ;
		}
		
		private string RandomString(List<string> list)
		{
			Random rand = new Random();
			string answ = list[rand.Next(list.Count)];
			return answ;
		}
		
		private bool CheckSendingMsg(string message)
		{
			bool answ = true;
			string msg = message;
			char[] space = new char[1];
			space[0] = ' ';
			msg = msg.Trim(space);
			
			if (msg == "" ||
			    msg == m_csBasicStartMessage)
				answ = false;
				
			return answ;
		}
	}
}
