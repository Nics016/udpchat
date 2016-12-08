/*
 * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
 * User: Garraty
 * Date: 28.09.2015
 * Time: 22:54
 * 
 * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
 */
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace MsgLib
{
	/// <summary>
	/// Description of Message.
	/// </summary>
		
	public class Message
	{
		public enum command
		{
			receive = 0,
			refresh,
			continueReceiving, //client expects more messages from server (updating)
			stopRefreshing,		 //means last message has been received, this command comes after all the messages
			login,						 //client tries to login
			loginFailed,       //server answers that login failed
			loginSuccess,
			check							 //connection check
		}
		
		private static int	 m_iNextID = 1;
		private int 				 m_iID;
		private string 			 m_sText;
		
		public Message(string msg)
		{
			m_iID = this.GetNextID();
			m_sText = msg;
		}
		
		private int GetNextID()
		{
				int answ = m_iNextID;
				m_iNextID++;
				return answ;
		}
		
		public int ID
		{
			get
			{
				return m_iID;
			}
		}
		
		public string Text
		{
			get
			{
				return m_sText;
			}
		}
		
		public static int LastUsedID
		{
			get
			{
				return m_iNextID-1;
			}
		}
		
		private static string InsertZero(string s)
		{
			string answ = s;
			
			if (answ.Length < 2)
				answ = "0" + answ;
			
			return answ;
		}
		
		public static void InsertCurTime(ref string s)
		{
			string sTime = String.Format("{0}:{1}", InsertZero(DateTime.Now.Hour.ToString()), InsertZero(DateTime.Now.Minute.ToString()));
			string answ = String.Format("[{0}] {1}", sTime, s);
			s = answ;
		}
		
		//get command and cut it off from the message
		public static int GetCommand(ref string msg)
		{
			int answ = -2;
			try
			{
				string sCom = msg.Substring(0, msg.IndexOf(' '));
				answ = Convert.ToInt32(sCom);
				//cut remainings of the command
				if (msg != sCom)
					msg = msg.Substring(sCom.Length + 1, msg.Length - sCom.Length - 1);
			}
			catch
			{
				answ = -1;
			}
			
			return answ;
		}
		
		public static void SendMessageTo(string msg, IPEndPoint ipEndPoint, UdpClient client)
		{
			byte[] byteBuffer = Encoding.UTF8.GetBytes(msg);
			try
			{
				client.Send(byteBuffer, byteBuffer.Length, ipEndPoint);
			}
			
			catch(Exception e)
			{
				Console.WriteLine("Failed to send msg '{0}' - {1}", msg, e.Message);
			}
		}
		
		//simply creates a string from the enum's digit
		//TODO: Make all the conversions in client and server's 
		//application use this method, so it will add much more flexibility
		public static string MakeCommand(MsgLib.Message.command com)
		{
			string answ = ((int)com).ToString() + " ";
			return answ;
		}
	}
}
