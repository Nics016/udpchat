/*
 * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
 * User: Garraty
 * Date: 29.09.2015
 * Time: 14:19
 * 
 * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
 */
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ChatClient
{
	/// <summary>
	/// Description of MainForm.
	/// </summary>
	public partial class MainForm : Form
	{
		//remember, you can manipulate scenes only from the GUI's thread
		//or by using 'Invoke'
		private Scene m_sceneMain;
		private Scene m_sceneLogin;
		
		//NotifyIcon
		private NotifyIcon m_Notify;
		private bool m_bNotifyShown = false;
		
		//to have an access to the input
		private TextBox m_tbIP = null;
		private TextBox m_tbPort = null;
		private TextBox m_tbNickname = null;
		private ListBox m_lbChat = null;
		private TextBox m_tbMessage = null;
		private Button  m_btnSend = null;
		private CheckBox m_cbShowMessagesInTray = null;
		private const string m_csBasicStartMessage = "<Введите сообщение>";
		
		//client's socket variables
		UdpClient client = null;
		IPEndPoint m_ServerEndPoint = null;
		//are we currently connected to the server?
		private bool m_bConnected = false;
		//lattest message's received id
		private int m_iLastMsg;
		
		//Threads' variables
		private byte[] m_aBytesAnsw = null;
		//true means we are receiving smth
		private bool m_bReceived;
		//true means we are refreshing currently
		private bool m_bRefreshing;
		//receiving answ in thread
		private	Thread m_tReceiver;		
		
		//CheckConnection's thread check delay in ms
		private const int m_ciConnectionCheckRate = 2000;
		//checks present of the connection between client and server
		//every few seconds. also refreshes msgs
		private Thread m_tConnectionChecker;
		
		//MessageSender's thread sending delay in ms
		private const int m_ciMessageSendingRate = 100;
		//sends messages from queue if there are such
		private Thread m_tMsgSender;
		//dynamic queue containing messages to be send
		private Queue<string> m_qMessages;
		
		//MESSAGES
		private List<string> m_aWrongMessageText = null;
		
		public MainForm()
		{
			client = new UdpClient();
			m_iLastMsg = 0;
			m_bReceived = true;
			InitializeComponent();
			m_bRefreshing = false;
			m_qMessages = new Queue<string>();
			//prompts
			m_aWrongMessageText = new List<string>();
			FillTheMessages();
		}	
		
		private void FillTheMessages()
		{
			#region hardcoding is there
			//filling out the messages (usually warning) to show
			//just for fun
			m_aWrongMessageText.Add("Пожалуйста, напишите что-нибудь");
			m_aWrongMessageText.Add("Я знаю, очень сложно нажимать клавиши на клавиатуре, но, пожалуйста, хотя бы попробуйте");
			m_aWrongMessageText.Add("Вы устали, я знаю. Но напишите что-нибудь, пожалуйста");
			m_aWrongMessageText.Add("Пустое сообщение");
			m_aWrongMessageText.Add("Я не могу отправить ваше сообщение в чат - оно слишком хорошо, чтобы его кто-то увидел");
			m_aWrongMessageText.Add("Это все, на что вы способны?");
			m_aWrongMessageText.Add("Добавьте точку, пожалуйста");
			m_aWrongMessageText.Add("Серьезно?");
			m_aWrongMessageText.Add("Нет, я не могу отправить ЭТО");
			m_aWrongMessageText.Add("Вызываемый абонент недоступен. Пожалуйста, уточните запрос.");
			m_aWrongMessageText.Add("Эта программа была создана не для того, чтобы отправлять пустые сообщения");
			m_aWrongMessageText.Add("Создатель данного приложения приносит свои глубочайшие извинения, но отправить данное сообщение не представляется возможным");
			m_aWrongMessageText.Add("Вы знаете китайский? Если да, то напишите что-нибудь на китайском, пожалуйста - "
			                       +" все лучше, чем пустое сообщение");
			#endregion
		}
		
		private void LoadSceneLogin()
		{
			m_sceneLogin = new Scene(Program.MF);
			#region GroupBox 'Server'
			//group box "Server"
			GroupBox gbServer = new GroupBox();
			gbServer.Size = new Size(new Point(350, 70));
			gbServer.Location = new Point(20, 20);
			gbServer.Text = "Сервер";
			m_sceneLogin.Add(gbServer);
			
			//label "IP"
			Label labIP = new Label();
			labIP.Size = new Size(new Point(20, 20));
			labIP.Text = "IP:";
			labIP.Location = new Point(20, 20);
			gbServer.Controls.Add(labIP);
			
			//text box "IP"
			TextBox tbIP = new TextBox();
			tbIP.Size = new Size(new Point(150, 20));
			tbIP.Location = new Point(labIP.Left + labIP.Width + 10, labIP.Top);
			tbIP.Text = "127.0.0.1";
			tbIP.KeyPress += new KeyPressEventHandler(Connect_KeyPress);
			m_tbIP = tbIP;
			gbServer.Controls.Add(tbIP);
			
			//label "Port"
			Label labPort = new Label();
			labPort.Size = new Size(new Point(30, 20));
			labPort.Location = new Point(tbIP.Left + tbIP.Width + 10, tbIP.Top);
			labPort.Text = "Port";
			gbServer.Controls.Add(labPort);
			
			//text box "Port"
			TextBox tbPort = new TextBox();
			tbPort.Size = new Size(new Point(40, 20));
			tbPort.Location = new Point(labPort.Left + labPort.Width + 10, labPort.Top);
			tbPort.Text = "8888";
			tbPort.Enabled = false;
			m_tbPort = tbPort;
			gbServer.Controls.Add(tbPort);
			#endregion
			
			#region GroupBox 'User'
			GroupBox gbUser = new GroupBox();
			gbUser.Size = new Size(350, 60);
			gbUser.Location = new Point(gbServer.Left, gbServer.Top + gbServer.Height + 10);
			gbUser.Text = "Пользователь";
			m_sceneLogin.Add(gbUser);
			
			Label labNickName = new Label();
			labNickName.Size = new Size(39, 20);
			labNickName.Location = new Point(20, 20);
			labNickName.Text = "Имя:";
			gbUser.Controls.Add(labNickName);
			
			TextBox tbNickName = new TextBox();
			tbNickName.Size = new Size(100, 20);
			tbNickName.Location = new Point(labNickName.Left + labNickName.Width + 5, labNickName.Top);
			tbNickName.Text = "Незнакомец";
			tbNickName.KeyPress += new KeyPressEventHandler(Connect_KeyPress);
			m_tbNickname = tbNickName;
			gbUser.Controls.Add(tbNickName);
			
			Button btnConnect = new Button();
			btnConnect.Size = new Size(100, 40);
			btnConnect.Location = new Point(gbUser.Left + gbUser.Width / 2 - btnConnect.Width / 2, gbUser.Top + gbUser.Height + 30);
			btnConnect.Text = "Присоединиться";
			btnConnect.Click += new EventHandler(btnConnect_Click);
			m_sceneLogin.Add(btnConnect);
			#endregion
			
			m_sceneLogin.Initialize();
		}
		
		private void LoadSceneMain()
		{
			#region MainScene Elements
			m_sceneMain = new Scene(Program.MF);
			//Button 'Hide to Tray'
			Button m_btnHide = new Button();
			m_btnHide.Size = new Size(60, 20);
			m_btnHide.Location = new Point(310, 0);
			m_btnHide.Text = "Скрыть";
			m_btnHide.Click += new EventHandler(m_btnHide_Click);
			m_sceneMain.Add(m_btnHide);
			
			//Checkbox && Label 'Show msgs when in tray'
			CheckBox cbShowMsgsInTray = new CheckBox();
			cbShowMsgsInTray.Text = "Всплывающие сообщения";
			cbShowMsgsInTray.Size = new Size(cbShowMsgsInTray.Text.Length * 8 + 10, 20);
			cbShowMsgsInTray.Location = new Point(20, 0);
			cbShowMsgsInTray.Checked = true;
			m_cbShowMessagesInTray = cbShowMsgsInTray;
			m_sceneMain.Add(cbShowMsgsInTray);
			
			//box for all the messages
			ListBox lbChat = new ListBox();
			lbChat.Size = new Size(350, 250);
			lbChat.Location = new Point(20, 25);
			m_lbChat = lbChat;
			m_sceneMain.Add(lbChat);
			
			TextBox tbMessage = new TextBox();
			tbMessage.Size = new Size(lbChat.Width - 100, 20);
			tbMessage.Location = new Point(lbChat.Left, lbChat.Top + lbChat.Height + 10);
			tbMessage.Text = m_csBasicStartMessage;
			tbMessage.Click += new EventHandler(tbMessage_Click);
			tbMessage.KeyPress += new KeyPressEventHandler(tbMessage_KeyPress);
			m_tbMessage = tbMessage;
			m_sceneMain.Add(tbMessage);
			
			Button btnSendMessage = new Button();
			btnSendMessage.Size = new Size(80, 20);
			btnSendMessage.Location = new Point(tbMessage.Left + tbMessage.Width + 10, tbMessage.Top);
			btnSendMessage.Text = "Отправить";
			btnSendMessage.Click += new EventHandler(btnSendMessage_Click);
			m_btnSend = btnSendMessage;
			m_sceneMain.Add(btnSendMessage);
			#endregion
			
			m_sceneMain.Initialize();
		}
		
		private void ShowSceneMain()
		{
			Program.SetMFSize(new Point(400, 350));
			m_sceneMain.Show();
		}	
		
		private void HideSceneMain()
		{
			m_sceneMain.Hide();
		}
			
		private void ShowSceneLogin()
		{
			Program.SetMFSize(new Point(400, 300));
			m_sceneLogin.Show();
		}
		
		private void HideSceneLogin()
		{
			m_sceneLogin.Hide();
		}
		
		public void Init()
		{
			LoadSceneLogin();
			LoadSceneMain();
			
			ShowSceneLogin();
		}
	}
}
