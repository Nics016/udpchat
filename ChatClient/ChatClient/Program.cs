/*
 * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
 * User: Garraty
 * Date: 29.09.2015
 * Time: 14:19
 * 
 * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
 */
using System;
using System.Drawing;
using System.Windows.Forms;

namespace ChatClient
{
	/// <summary>
	/// Class with program entry point.
	/// </summary>
	internal sealed class Program
	{
		private static MainForm m_fMF;
		
		/// <summary>
		/// Program entry point.
		/// </summary>
		[STAThread]
		private static void Main(string[] args)
		{
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			m_fMF = new MainForm();
			m_fMF.Init();
			Application.Run(m_fMF);			
		}
		
		public static Form MF
		{
			get
			{
				return m_fMF;
			}
		}
		
		public static void SetMFSize(Point newSize)
		{
			m_fMF.Size = new Size(newSize);
		}
	}
}
