/*
 * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
 * User: Garraty
 * Date: 30.09.2015
 * Time: 21:34
 * 
 * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
 */
using System;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using MsgLib;

namespace ChatClient
{
	/// <summary>
	/// Description of MainForm_Actions.
	/// </summary>
	public partial class MainForm
	{
		void btnConnect_Click(object sender, EventArgs e)
		{
			#region <CONNECT>
			// Sanity check
			if (m_tbIP == null || m_tbPort == null || m_tbNickname == null)
				throw new Exception("MainForm::btnConnect_Click - some TextBox is <null>");
			
			//get nickName from the textbox
			string sNickName = m_tbNickname.Text;
			
			//IP
			string sIP = m_tbIP.Text;
			
			//Port
			string sPort = m_tbPort.Text;
			
			if (!NickNameCheck(ref sNickName))
			{
				MessageBox.Show("Please, enter your nickname");
				return;
			}
			
			//try to create endpoint for the server
			try
			{
				m_ServerEndPoint = new IPEndPoint(IPAddress.Parse(sIP), Int32.Parse(sPort));
			}
			catch
			{
				MessageBox.Show("Invalid IP format");
				return;
			}
			
			//try to login on the server
			string sLoginMsg = MsgLib.Message.MakeCommand(MsgLib.Message.command.login);
			sLoginMsg += sNickName;
			MsgLib.Message.SendMessageTo(sLoginMsg, m_ServerEndPoint, client);
			
			//get answer from the server
			string sServerLoginAnswer = GetAnswerFromServer();
			//we use this variables because we don't want to lose the value of our response
			string sServerResult = sServerLoginAnswer; 
			//error while receiving answer from the server
			if (sServerLoginAnswer == null)
					return;
			//server sent errorLogin msg
			else if (MsgLib.Message.GetCommand(ref sServerResult) == (int)MsgLib.Message.command.loginFailed)
			{
				MessageBox.Show(sServerResult);
				return;
			}
			
			//is login successful?
			HandleMessage(sServerLoginAnswer);
			
			//Conection Checker Thread
			m_tConnectionChecker = new Thread(CheckConnection);
			m_tConnectionChecker.Start();
			
			//Sending messages' queue
			m_tMsgSender = new Thread(DispatchQueueItem);
			m_tMsgSender.Start();
			#endregion
		}	
		
		private void HandleMessage(string msg)
		{
			#region <HANDLE_MESSAGE>
			int iCommand = MsgLib.Message.GetCommand(ref msg);
			switch (iCommand)
			{
				case (int)MsgLib.Message.command.loginSuccess:
					m_bConnected = true;
					HideSceneLogin();
					ShowSceneMain();
					RefreshMessages();
					//reset cursor
					this.Invoke((MethodInvoker)delegate{m_lbChat.SetSelected(m_lbChat.Items.Count - 1, true);});
					break;
					
				case (int)MsgLib.Message.command.receive:
					//check id. if it doesn't fit, refresh messages
					if (Convert.ToInt32(msg) != (m_iLastMsg + 1))
					{
						RefreshMessages();
					}
					else
					{
						string sMessage = m_tbNickname.Text + ": " + m_tbMessage.Text;
						MsgLib.Message.InsertCurTime(ref sMessage);
						m_lbChat.Items.Add(sMessage);
						m_tbMessage.Text = "";
						m_lbChat.SetSelected(m_lbChat.Items.Count - 1, true);
						m_tbMessage.Focus();
						m_iLastMsg++;
					}
					break;
					
				case (int)MsgLib.Message.command.check:
					//server tries to check us, it is not our echo msg
					if (msg == "server")
					{
						string sEchoAnswer = MsgLib.Message.MakeCommand(MsgLib.Message.command.check) + "server";
						MsgLib.Message.SendMessageTo(sEchoAnswer, m_ServerEndPoint, client);
					}
					break;
			}
			#endregion
		}
		
		void RefreshMessages()
		{
			#region <REFRESH_MESSAGES>
			string sRefresh = MsgLib.Message.MakeCommand(MsgLib.Message.command.refresh);
			sRefresh += (m_iLastMsg + 1).ToString();
			//spin if we are already rcving smthing
			Spin();
			m_bRefreshing = true;
			MsgLib.Message.SendMessageTo(sRefresh, m_ServerEndPoint, client);
						
			string sMessage = "";
			string sStopRefresh = MsgLib.Message.MakeCommand(MsgLib.Message.command.stopRefreshing);
			//first get iStartID and refresh the last msg if needed
			int iStartID = 0;
			try
			{
				iStartID = Convert.ToInt32(GetAnswerFromServer());
			}
			catch
			{
				//smth went wrong
				m_bRefreshing = false;
				return;
			}
			m_iLastMsg = iStartID - 1;
			do
			{
				sMessage = GetAnswerFromServer();
				if (sMessage == null)
					return;
				if (sMessage != sStopRefresh)
				{
					//if not the last msg, just add it to the chat
					m_lbChat.Items.Add(sMessage);
					//if notifications are on and app is in tray, display msg
					if (m_cbShowMessagesInTray.Checked && m_Notify.Visible)
					{
						m_Notify.ShowBalloonTip(1, "Новое сообщение", sMessage, new ToolTipIcon());
						//refresh the cursor
						this.Invoke((MethodInvoker)delegate{m_lbChat.SetSelected(m_lbChat.Items.Count - 1, true);});
					}
					m_iLastMsg++;
				}							
			} while(sMessage != sStopRefresh);
			m_bRefreshing = false;
			#endregion
		}
		
		void btnSendMessage_Click(object sender, EventArgs e)
		{
			#region <SEND_MESSAGE>			
			//sending msg to the servers
			string sMessage = m_tbMessage.Text;
			//check msg format. if it is wrong, return funny msg
			if (!CheckSendingMsg(sMessage))
			{
				MessageBox.Show(RandomString(m_aWrongMessageText));
				return;
			}
			    
			//create a message to be send to the server
			string sSendMsg = MsgLib.Message.MakeCommand(MsgLib.Message.command.receive);
			sSendMsg += sMessage;
			
			AddMessageToQueue(sSendMsg);
			#endregion
		}
		
		string GetAnswerFromServer()
		{			
			#region <GET_ANSWER_FROM_SERVER>
			string answ = null;
			m_aBytesAnsw = null;
			//receiving answ in thread
			m_bReceived = false;
			m_tReceiver = new Thread(ReceiveBytesAnswerFromServer);		
			m_tReceiver.Start();
			//spin for a while waiting for the started thread to become alive
			//while (!m_tReceiver.IsAlive);
			//wait for an answer from the server
			WaitForRespond(5000);
			//Thread.Sleep(m_iWaitForAnswer);
			//if still not received, disconnect
			
			//wait until thread finishes
			//m_tReceiver.Join();
			
			if (m_aBytesAnsw != null)
				answ = Encoding.UTF8.GetString(m_aBytesAnsw);
			else
				LostConnection();
			return answ;
			#endregion
		}
		
		void LostConnection()
		{
			#region <LOST_CONNECTION>
			//stop receiving
			if (m_tReceiver != null)
			{
				//stop the threads
				m_tReceiver.Abort();
				CloseClientConnection();				
			}
			
			//reset variables
			m_bRefreshing = false;
			m_bReceived = true;
			
			//stop checking
			if (m_tConnectionChecker != null)
				m_tConnectionChecker.Abort();
			
			HideSceneMain();
			ShowSceneLogin();
			if (m_bConnected == true)
				MessageBox.Show("The connection to the server was lost");
			else
				MessageBox.Show("Couldn't connect to the server. Please, check your connection and try again");
			m_bConnected = false;
			//reset variables for the chat
			ResetChat();
			#endregion
		}
		
		#region Extraneous functions
		
		#region <Thread_Messages_Sender>
		void AddMessageToQueue(string sMessage)
		{
			if (m_qMessages.Count < 5)
				m_qMessages.Enqueue(sMessage);
			else
				MessageBox.Show("Please, wait...");
		}
		
		//function for a thread m_tMsgSender
		//sends messages to the server from the queue
		void DispatchQueueItem()
		{
			while (m_bConnected)
			{
				Thread.Sleep(m_ciMessageSendingRate);
				//only check if we are currently connected
				if (!m_bConnected)
					return;
				
				//if we have nothing to be send, continue
				if (m_qMessages.Count == 0)
					continue;
				
				//firstly, take the next message to be send from the queue 
				string sMessage = m_qMessages.Dequeue();
				
				Spin();
			
				//m_bReceived = false;
				MsgLib.Message.SendMessageTo(sMessage, m_ServerEndPoint, client);
			
				string sAnsw = GetAnswerFromServer();
				if (sAnsw == null)
					return;
				this.Invoke((MethodInvoker)delegate{HandleMessage(sAnsw);});
			}
		}
		#endregion
		
		#region <OnEvents>
		void Connect_KeyPress(object sender, KeyPressEventArgs e)
		{
			if ((int)e.KeyChar == 13)
				btnConnect_Click(this, new EventArgs());
		}
		
		void tbMessage_KeyPress(object sender, KeyPressEventArgs e)
		{
			if ((int)e.KeyChar == 13)
				btnSendMessage_Click(this, new EventArgs());
		}
				
		void tbMessage_Click(object sender, EventArgs e)
		{
			if (m_tbMessage.Text == m_csBasicStartMessage)
				m_tbMessage.Text = "";
		}
		void m_tReceiver_Tick(object sender, EventArgs e)
		{
			ReceiveBytesAnswerFromServer();
		}
		
		//Hide to tray
		void m_btnHide_Click(object sender, EventArgs e)
		{
			this.Hide();
			m_Notify.Visible = true;
			if (m_cbShowMessagesInTray.Checked)
			{
				if (!m_bNotifyShown)
				{
					//display this tip only in the first time
					m_Notify.ShowBalloonTip(1000, "", "Чат скрыт. Чтобы развернуть, кликните на эту иконку дважды", new ToolTipIcon());
					m_bNotifyShown = true;
				}
			}
		}
		
		//Show app
		void m_Notify_DoubleClick(object sender, EventArgs e)
		{
			this.Show();
			m_Notify.Visible = false;
		}
				
		void MainForm_Closed(object sender, EventArgs e)
		{
			//tidy up after ourselves
			m_bConnected = false;
			client.Close();
			
			//terminate Message Receiver
			if (m_tMsgSender != null)
			{
				m_tMsgSender.Abort();
				m_tMsgSender.Join();
			}
			
			//terminate Connection Checker
			if (m_tConnectionChecker != null)
			{
				m_tConnectionChecker.Abort();
				m_tConnectionChecker.Join();
			}			
		}
		#endregion
		
		#region <Thread_ConnectionChecker>
		void CheckConnection()
		{			
			while (m_bConnected)
			{
				Thread.Sleep(m_ciConnectionCheckRate);
				//only check if we are currently connected
				if (!m_bConnected)
					return;
				Spin();
				//sending check-msgs every X seconds
				string sCheck = MsgLib.Message.MakeCommand(MsgLib.Message.command.check);
				MsgLib.Message.SendMessageTo(sCheck, m_ServerEndPoint, client);
				try
				{
					string sCheckRespond = GetAnswerFromServer();
					RefreshMessages();
				}
				catch
				{
					//Do stuff on UI's hot sweety thread
					this.Invoke((MethodInvoker)delegate{LostConnection();});
				}				
			}
		}
		#endregion
		
		//for thread Receiver
		void ReceiveBytesAnswerFromServer()
		{			
			try
			{
				Thread.Sleep(0);
				byte[] answ = client.Receive(ref m_ServerEndPoint);
				m_aBytesAnsw = answ;
				m_bReceived = true;
			}
			catch
			{
			}
		}
		
		void CloseClientConnection()
		{
			client.Close();
			client = new UdpClient();
		}
		
		void WaitForRespond(int iTimeToWait)
		{
			int iTimeRemaining = iTimeToWait;
			const int iSleepTime = 20;
			while (iTimeRemaining > 0)
			{
				Thread.Sleep(20);
				iTimeRemaining -= iSleepTime;
				if (m_bReceived)
				{
					break;
				}
			}
			
			//if still not received, disconnect
			if (!m_bReceived)
			{
				LostConnection();
			}
		}
		
		void Spin()
		{
			//everything has to have its limits
			int iLimit = m_ciConnectionCheckRate / 10;
			int iCur = 0;
			while (!m_bReceived || m_bRefreshing)
			{
				Thread.Sleep(1);
				iCur++;
				if (iCur > iLimit)
					return;
			}
		}
		
		void ResetChat()
		{
			m_lbChat.Items.Clear();
			m_iLastMsg = 0;
		}
		#endregion
	}
}
