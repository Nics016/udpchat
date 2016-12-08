/*
 * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
 * User: Garraty
 * Date: 28.09.2015
 * Time: 22:24
 * 
 * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
 */
using System;
using System.Windows.Forms;
using System.Drawing;

namespace ChatServer
{
	class Program
	{
		private static NotifyIcon m_NotifyIcon;
		private static UdpChatServer server = null;
		
		public static void Main(string[] args)
		{
			#region <NOTIFY_ICON>
			m_NotifyIcon = new NotifyIcon();
			m_NotifyIcon.Icon = new Icon("ServerIcon.ico");
			m_NotifyIcon.Visible = true;
			m_NotifyIcon.DoubleClick += new EventHandler(m_NotifyIcon_DoubleClick);
			#endregion
			
			server = new UdpChatServer(8888, m_NotifyIcon);
			server.Start();
			//Console.Read();
			
			
		}

		static void m_NotifyIcon_DoubleClick(object sender, EventArgs e)
		{
			MessageBox.Show("1");
			server.Stop();
		}
		
		private static void HideInTray()
		{
			
		}
	}
}