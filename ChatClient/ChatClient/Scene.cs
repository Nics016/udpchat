/*
 * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
 * User: Garraty
 * Date: 29.09.2015
 * Time: 14:19
 * 
 * ~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~~
 */
using System;
using System.Windows.Forms;
using System.Collections.Generic; // List

namespace ChatClient
{
	/// <summary>
	/// Description of Scene.
	/// </summary>
	public class Scene
	{
		//where do we display our elements
		private Form m_Form;
		
		//Contains all the elements
		private List<System.Windows.Forms.Control> m_aElements;
		
		public Scene(Form form)
		{			
			m_Form = form;
			m_aElements = new List<Control>();			
		}
		
		//add element to the scene
		public void Add(Control newElement)
		{
			m_aElements.Add(newElement);
		}
		
		//removes element from the scene
		public void Remove(Control element)
		{
			if (m_aElements.Contains(element))
			{
				m_Form.Controls.Remove(element);
			  m_aElements.Remove(element);
			  element.Hide();
			  element = null;			   
			}
		}
		
		public void Initialize()
		{
			//adds all new elements to the form's controls
			foreach(Control curElement in m_aElements)
			{
				//only add if it is not already there
				if (!m_Form.Controls.Contains(curElement))
				{
					m_Form.Controls.Add(curElement);
					curElement.Hide();
				}
			}
		}
		
		//shows all the elements in the scene
		public void Show()
		{
			foreach(Control curElement in m_aElements)
			{
				curElement.Show();
			}
		}
		
		//hides all the elements
		public void Hide()
		{
			foreach(Control curElement in m_aElements)
			{
				curElement.Hide();
			}
		}
	}
}
