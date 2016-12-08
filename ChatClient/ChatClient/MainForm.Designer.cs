/*
 * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
 * User: Garraty
 * Date: 29.09.2015
 * Time: 14:19
 * 
 * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
 */
using System.Windows.Forms;
using System;

namespace ChatClient
{
	partial class MainForm
	{
		/// <summary>
		/// Designer variable used to keep track of non-visual components.
		/// </summary>
		private System.ComponentModel.IContainer components = null;
		
		/// <summary>
		/// Disposes resources used by the form.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing) {
				if (components != null) {
					components.Dispose();
				}
			}
			base.Dispose(disposing);
		}
		
		/// <summary>
		/// This method is required for Windows Forms designer support.
		/// Do not change the method contents inside the source code editor. The Forms designer might
		/// not be able to load this method if it was changed manually.
		/// </summary>
		private void InitializeComponent()
		{
			// 
			// MainForm
			// 
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.Text = "ChatClient";
			this.Name = "MainForm";		
			this.Closed += new EventHandler(MainForm_Closed);
			//Tray Icon
			m_Notify = new NotifyIcon();
			m_Notify.Icon = new System.Drawing.Icon("ClientIcon.ico");
			m_Notify.DoubleClick += new EventHandler(m_Notify_DoubleClick);
		}
		
	}
}
