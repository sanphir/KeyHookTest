using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HookTest
{
	public partial class Form1 : Form
	{
		public Form1()
		{
			InitializeComponent();
			KeyHook.Instance().CapturedTextChanged += Form1_CapturedTextChanged;
		}

		private void Form1_CapturedTextChanged(object sender, EventArgs e)
		{
			textBox2.Text = KeyHook.Instance().CapturedText;
		}

		private void button1_Click(object sender, EventArgs e)
		{
			textBox1.Text = "";
			KeyHook.Instance().StartCapture();
		}
	}
}
