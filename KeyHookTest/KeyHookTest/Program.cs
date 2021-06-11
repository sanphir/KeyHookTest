using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HookTest
{
	static class Program
	{
		/// <summary>
		///  The main entry point for the application.
		/// </summary>
		[STAThread]
		static void Main()
		{
			KeyHook.Instance().Init();
			KeyHook.Instance().StartCapture();
			Application.SetHighDpiMode(HighDpiMode.SystemAware);
			Application.EnableVisualStyles();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new Form1());
		}

		//static void test()
		//{
		//	var gHook = new GlobalKeyboardHook();
		//	gHook.KeyDown += new KeyEventHandler(gHook_KeyDown);
		//	foreach (Keys key in Enum.GetValues(typeof(Keys)))
		//		gHook.HookedKeys.Add(key);
		//	String date = DateTime.Now.Year + "_" + DateTime.Now.Month + "_" + DateTime.Now.Day + "_" + DateTime.Now.Hour + "_" + DateTime.Now.Minute + "_" + DateTime.Now.Second;
		//	sw = new StreamWriter("TestFile_" + date + ".txt");
		//	sw.WriteLine(DateTime.Now);
		//	timer1.Start();
		//	textBox1.Text = DateTime.Now.ToString();
		//	gHook.hook();
		//}
	}
}
