/*
 * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
 * User: Garraty
 * Date: 28.09.2015
 * Time: 22:25
 * 
 * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
 */
using System;
using System.Text; 								//Encoding
using System.Collections.Generic; //Dictionary, List
using System.Net;									//IPEndPoint
using System.Net.Sockets;					//UdpClient
using System.Threading;						//Thread
using MsgLib;											//Message
using System.Windows.Forms;

namespace ChatServer
{
	/// <summary>
	/// Description of UdpChatServer.
	/// </summary>
	public class UdpChatServer
	{
		private int m_iPort;
		private UdpClient client = null;
		private IPEndPoint remoteIPEndPoint = null;
		private IPEndPoint lastUsedIPEndPoint = null;
		private bool bRunning = true;
		private Thread m_tMainThread = null;
		//storing all messages there
		private Dictionary<int, MsgLib.Message> m_aMessages;
		//storing all the users' nicknames there
		private Dictionary<string, string> m_aNickNames;		
		//How many last messages do we need to request from the server when refreshing
		private int m_iLastMsgsAmount = 20;
		
		private NotifyIcon m_Notify = null;
		
		public UdpChatServer(int iPort, NotifyIcon notify)
		{
			m_iPort = iPort;
			m_aMessages = new Dictionary<int, MsgLib.Message>();
			m_aNickNames = new Dictionary<string, string>();
			client = new UdpClient(iPort);
			remoteIPEndPoint = new IPEndPoint(IPAddress.Any, 0);
			m_Notify = notify;
		}
		
		public void Start()
		{
			try
			{
				m_tMainThread = new Thread(Run);			
				m_tMainThread.Start();
				bRunning = true;
				Console.WriteLine("Server has been successfully started on {0}:{1}", remoteIPEndPoint.Address.ToString(), m_iPort);
				m_Notify.ShowBalloonTip(1000, "", "Server is now running on port " + m_iPort.ToString() + ". To check your IP, open CMD and type " +
				                        "'ipconfig /all'", new ToolTipIcon());
				
			}
			catch(Exception e)
			{
				Console.WriteLine("Couldn't start server:\n{0}", e.Message);
				MessageBox.Show(String.Format("Couldn't start server:\n{0}", e.Message));
				if (m_tMainThread != null)
					m_tMainThread.Abort();
				bRunning = false;
			}
		}
		
		private void Run()
		{
			while (bRunning)
			{
				//get message
				Console.WriteLine("Waiting for a client...");
				byte[] byteBuffer = client.Receive(ref remoteIPEndPoint);
				string sMessage = Encoding.UTF8.GetString(byteBuffer);
				Console.WriteLine("Received msg from {0} -> {1}", remoteIPEndPoint, sMessage);
				lastUsedIPEndPoint = remoteIPEndPoint;
				HandleMsg(sMessage);
			}
		}
		
		private void HandleMsg(string msg)
		{
			//get number of the command (enum)
			int iCommand = MsgLib.Message.GetCommand(ref msg);
			
			switch (iCommand)
			{
				//new Message from a client
				case (int)MsgLib.Message.command.receive:			
					//get nickname of the sender
					string sSenderNickName = m_aNickNames[lastUsedIPEndPoint.Address.ToString()];
					string sMessageText = sSenderNickName + ": " + msg;
					//insert time
					MsgLib.Message.InsertCurTime(ref sMessageText);
					MsgLib.Message message = new MsgLib.Message(sMessageText);
					//finally, add msg to the array
					m_aMessages.Add(message.ID, message);
					string sSuccessReceive = MsgLib.Message.MakeCommand(MsgLib.Message.command.receive);
					//send the latest used ID, so if it won't correspond 
					//with client's, he will try to refresh
					sSuccessReceive += MsgLib.Message.LastUsedID.ToString();
					MsgLib.Message.SendMessageTo(sSuccessReceive, lastUsedIPEndPoint, client);
					break;
					
				//sending all the messages starting from iStartID to client
				case (int) MsgLib.Message.command.refresh:
					//sanity check
					if (lastUsedIPEndPoint == null)
						return;
					//the last msg, which the client received
					int iLastID = Convert.ToInt32(msg);
					
					//if client didn't refresh for too long, give him only few last messages
					int iStartID = Math.Max(iLastID, MsgLib.Message.LastUsedID - m_iLastMsgsAmount);
					//send StartID so the client will know what to expect from the server
					MsgLib.Message.SendMessageTo(iStartID.ToString(), lastUsedIPEndPoint, client);
					//send all the messages starting from iStartID
					for (int iCurMsg = iStartID; iCurMsg <= MsgLib.Message.LastUsedID; iCurMsg++)
					{
						MsgLib.Message.SendMessageTo(m_aMessages[iCurMsg].Text, lastUsedIPEndPoint, client);
					}
					//saying goodbye to the client, we are done
					string sGoodbye = MsgLib.Message.MakeCommand(MsgLib.Message.command.stopRefreshing);
					//sGoodbye += m_aMessages[Message.LastUsedID];
					MsgLib.Message.SendMessageTo(sGoodbye, lastUsedIPEndPoint, client);
					break;
					
				case (int) MsgLib.Message.command.login:
					//nickname already exists?		
					//did user connect previously?
					if (m_aNickNames.ContainsKey(lastUsedIPEndPoint.Address.ToString()))
					{
						string sOldNick = m_aNickNames[lastUsedIPEndPoint.Address.ToString()];
						//nick is still the same
						if (sOldNick == msg)
						{
							SendSuccessMessage(String.Format("[{0} at {1}] user has been reconnected", msg, lastUsedIPEndPoint.Address.ToString()));
						}
						//the user has changed the nickname
						else
						{
							m_aNickNames[lastUsedIPEndPoint.Address.ToString()] = msg;
							SendSuccessMessage(String.Format("[{0} at {1}] user has been reconnected with ne nickname", msg, lastUsedIPEndPoint.Address.ToString()));
						}
					}
					//if he never connected, check whether nickname is unique
					else if (m_aNickNames.ContainsValue(msg))
					{
						string sNickExists = MsgLib.Message.MakeCommand(MsgLib.Message.command.loginFailed);
						sNickExists += "This nickname is  already registered in this chat. Please, enter another nickname";
						//nickName already registered
						MsgLib.Message.SendMessageTo(sNickExists, lastUsedIPEndPoint, client);
					}
					else
					{
						//add new nickname to the array
						string sIP = lastUsedIPEndPoint.Address.ToString();
						string sNewNickName = msg;
						m_aNickNames.Add(sIP, sNewNickName);
						//send success message to client
						SendSuccessMessage(String.Format("[{0} at {1}] new user has been connected", msg, lastUsedIPEndPoint.Address.ToString()));
					}
					break;
					
				case (int)MsgLib.Message.command.check:
					//only respond if it is not echo
					if (msg != "server")
					{
						MsgLib.Message.SendMessageTo(MsgLib.Message.MakeCommand(MsgLib.Message.command.check), lastUsedIPEndPoint, client);
					}
					break;
			}
		}
		
		public void SendSuccessMessage(string sMessage)
		{
			//messages client that he has been loggined successfully
			string sSuccessLogin = MsgLib.Message.MakeCommand(MsgLib.Message.command.loginSuccess);
			//insert time
			MsgLib.Message.InsertCurTime(ref sMessage);
			Console.WriteLine(sMessage);
			MsgLib.Message msgConnected = new MsgLib.Message(sMessage);
			m_aMessages.Add(msgConnected.ID, msgConnected);
			//informing client about successful login
		  MsgLib.Message.SendMessageTo(sSuccessLogin, lastUsedIPEndPoint, client);
		}
		
		public void Stop()
		{
			if (m_tMainThread != null)
			{
				client.Close();
				m_tMainThread.Abort();
				bRunning = false;				
				m_tMainThread.Join();
				m_tMainThread = null;
			}
		}
	}
}
