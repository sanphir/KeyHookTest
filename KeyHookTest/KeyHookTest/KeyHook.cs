using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text;

namespace HookTest
{
	/// <summary>
	/// Хук для клавиатуры
	/// </summary>
	public class KeyHook
	{
		private KeyHook()
		{
		}

		private static KeyHook _instance;

		/// <summary/>
		public static KeyHook Instance()
		{
			if (_instance == null)
				_instance = new KeyHook();

			return _instance;
		}

		/// <summary/>		
		public void Init()
		{
			ProcessModule currentModule = Process.GetCurrentProcess().MainModule;
			keyboardProcess = new LowLevelKeyboardProc(CaptureKey);
			ptrHook = SetWindowsHookEx(WH_KEYBOARD_LL, keyboardProcess, GetModuleHandle(currentModule.ModuleName), 0);
		}

		/// <summary>
		/// Включить захват текста
		/// </summary>
		public void StartCapture()
		{
			_isCapturedStarted = true;
			CapturedText = "";
		}

		/// <summary>
		/// Выключить захват текста
		/// </summary>
		public void StopCapture()
		{
			_isCapturedStarted = false;
		}

		private bool _isCapturedStarted = false;
		/// <summary>
		/// Захват текста включен
		/// </summary>
		public bool IsCapturedStarted => _isCapturedStarted;


		public event EventHandler CapturedTextChanged;

		private string _capturedText;
		/// <summary>
		/// Захваченный текст
		/// </summary>
		public string CapturedText
		{
			get => _capturedText;
			private set
			{
				_capturedText = value;
				CapturedTextChanged?.Invoke(this, EventArgs.Empty);
			}
		}


		[StructLayout(LayoutKind.Sequential)]
		private struct KBDLLHOOKSTRUCT
		{
			public Keys key;
		}

		private const int WH_KEYBOARD_LL = 13;
		private const int WM_KEYDOWN = 0x0100;
		private const int WM_KEYUP = 0x0101;

		[Flags]
		private enum KeyStates
		{
			None = 0,
			Down = 1,
			Toggled = 2
		}

		private StringBuilder Input = new StringBuilder(9);


		/// <summary>
		/// Функция обратного вызова для нажатий клавиатуры. Система вызывает эту функцию каждый раз, когда новое событие ввода с клавиатуры собирается быть отправлено в очередь ввода потока.
		/// </summary>
		/// <param name="nCode"></param>
		/// <param name="wParam">Идентификатор сообщения клавиатуры. Этот параметр может принимать одно из следующих сообщений: WM_KEYDOWN, WM_KEYUP, WM_SYSKEYDOWNили WM_SYSKEYUP.</param>
		/// <param name="lParam"></param>
		/// <returns></returns>
		private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

		#region DllImport
		[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
		private static extern short GetKeyState(int keyCode);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr SetWindowsHookEx(int id, LowLevelKeyboardProc callback, IntPtr hMod, uint dwThreadId);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr CallNextHookEx(IntPtr hook, int nCode, IntPtr wp, IntPtr lp);

		[DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern IntPtr GetModuleHandle(string name);

		[DllImport("user32.dll", SetLastError = true)]
		private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

		[DllImport("user32.dll")]
		static extern uint GetKeyboardLayoutList(int nBuff, [Out] IntPtr[] lpList);
		[DllImport("user32.dll")]
		private static extern IntPtr LoadKeyboardLayout(string pwszKLID, uint Flags);
		[DllImport("user32.dll")]
		static extern bool GetKeyboardLayoutName([Out] StringBuilder pwszKLID);
		[DllImport("user32.dll")]
		static extern IntPtr GetKeyboardLayout(uint idThread);
		[DllImport("user32.dll")]
		private static extern IntPtr GetForegroundWindow();
		[DllImport("user32.dll")]
		private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);
		#endregion

		private IntPtr ptrHook;

		private LowLevelKeyboardProc keyboardProcess;

		private const string PRESSED_MSG = "Нажата";
		private const string NOT_PRESSED_MSG = "Отпущена";
		private const string EN_KL = "00000409";
		private const string RU_KL = "00000419";

		private IntPtr CaptureKey(int nCode, IntPtr wParam, IntPtr lParam)
		{
			if (nCode >= 0)
			{
				var keyInfo = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));

				Console.WriteLine($"Состояние Shift: {(GetKeyState((int)Keys.Shift) < 0 ? PRESSED_MSG : NOT_PRESSED_MSG)}");
				Console.WriteLine($"Состояние LShift: {(GetKeyState((int)Keys.LShiftKey) < 0 ? PRESSED_MSG : NOT_PRESSED_MSG)}");
				Console.WriteLine($"Состояние RShift: {(GetKeyState((int)Keys.RShiftKey) < 0 ? PRESSED_MSG : NOT_PRESSED_MSG)}");
				Console.WriteLine($"Состояние NumLock: {(GetKeyState((int)Keys.NumLock) < 0 ? PRESSED_MSG : NOT_PRESSED_MSG)}");
				Console.WriteLine($"Состояние CapsLock: {(GetKeyState((int)Keys.CapsLock) < 0 ? PRESSED_MSG : NOT_PRESSED_MSG)}");

				if (_isCapturedStarted && wParam == (IntPtr)WM_KEYUP)
				{
					ProcessCapture(keyInfo);
				}

				//if (wParam == (IntPtr)WM_KEYDOWN)
				//{
				//	Console.WriteLine($"Нажата клавиша: {keyInfo.key}");
				//}
				//if (wParam == (IntPtr)WM_KEYUP)
				//{
				//	Console.WriteLine($"Отпущена клавиша: {keyInfo.key}");
				//}

				//if (keyInfo.key == Keys.ShiftKey && wParam == (IntPtr)WM_KEYDOWN)
				//{
				//	Console.WriteLine($"Нажата клавиша шифт: {keyInfo.key}");					
				//}

			}

			//отменить
			//return (IntPtr)1;
			return CallNextHookEx(ptrHook, nCode, wParam, lParam);
		}

		public bool IsShiftPressed => (GetKeyState((int)Keys.LShiftKey) < 0) || (GetKeyState((int)Keys.RShiftKey) < 0);
		public bool IsCapsLockOn => GetKeyState((int)Keys.CapsLock) == 1;
		public bool IsNumLockOn => GetKeyState((int)Keys.NumLock) == 1;

		private enum KeyLayout
		{
			En,
			Ru,
			Unknown
		}

		private void ProcessCapture(KBDLLHOOKSTRUCT keyInfo)
		{
			#region Неиспользуемые клавиши
			switch (keyInfo.key)
			{
				case Keys.F1:
				case Keys.F2:
				case Keys.F3:
				case Keys.F4:
				case Keys.F5:
				case Keys.F6:
				case Keys.F7:
				case Keys.F8:
				case Keys.F9:
				case Keys.F10:
				case Keys.F11:
				case Keys.F12:
				case Keys.F13:
				case Keys.F14:
				case Keys.F15:
				case Keys.F16:
				case Keys.F17:
				case Keys.F18:
				case Keys.F19:
				case Keys.F20:
				case Keys.F21:
				case Keys.F22:
				case Keys.F23:
				case Keys.F24:
				case Keys.BrowserBack:
				case Keys.BrowserForward:
				case Keys.BrowserRefresh:
				case Keys.BrowserStop:
				case Keys.BrowserSearch:
				case Keys.BrowserFavorites:
				case Keys.BrowserHome:
				case Keys.VolumeMute:
				case Keys.VolumeDown:
				case Keys.VolumeUp:
				case Keys.MediaNextTrack:
				case Keys.MediaPreviousTrack:
				case Keys.MediaStop:
				case Keys.MediaPlayPause:
				case Keys.LaunchMail:
				case Keys.SelectMedia:
				case Keys.LaunchApplication1:
				case Keys.LaunchApplication2:
				case Keys.LButton:
				case Keys.RButton:
					return;
			}
			#endregion

			#region Независимые от раскладки Клавиши
			switch (keyInfo.key)
			{
				case Keys.KeyCode:
					break;
				case Keys.Modifiers:
					break;
				case Keys.None:
					break;
				case Keys.Cancel:
					break;
				case Keys.MButton:
					break;
				case Keys.XButton1:
					break;
				case Keys.XButton2:
					break;
				case Keys.Back:
					break;
				case Keys.Tab:
					break;
				case Keys.LineFeed:
					break;
				case Keys.Clear:
					break;
				case Keys.Return:
					break;
				//case Keys.Enter:
				//	break;
				case Keys.ShiftKey:
					break;
				case Keys.ControlKey:
					break;
				case Keys.Menu:
					break;
				case Keys.Pause:
					break;
				case Keys.Capital:
					break;
				//case Keys.CapsLock:
				//	break;
				case Keys.KanaMode:
					break;
				//case Keys.HanguelMode:
				//	break;
				//case Keys.HangulMode:
				//	break;
				case Keys.JunjaMode:
					break;
				case Keys.FinalMode:
					break;
				case Keys.HanjaMode:
					break;
				//case Keys.KanjiMode:
				//	break;
				case Keys.Escape:
					break;
				case Keys.IMEConvert:
					break;
				case Keys.IMENonconvert:
					break;
				case Keys.IMEAccept:
					break;
				//case Keys.IMEAceept:
				//	break;
				case Keys.IMEModeChange:
					break;
				case Keys.Space:
					break;
				case Keys.Prior:
					break;
				//case Keys.PageUp:
				//	break;
				case Keys.Next:
					break;
				//case Keys.PageDown:
				//	break;
				case Keys.End:
					break;
				case Keys.Home:
					break;
				case Keys.Left:
					break;
				case Keys.Up:
					break;
				case Keys.Right:
					break;
				case Keys.Down:
					break;
				case Keys.Select:
					break;
				case Keys.Print:
					break;
				case Keys.Execute:
					break;
				case Keys.Snapshot:
					break;
				//case Keys.PrintScreen:
				//	break;
				case Keys.Insert:
					break;
				case Keys.Delete:
					break;
				case Keys.Help:
					break;
				case Keys.D0:
					CapturedText += IsShiftPressed ? ")" : "0";
					break;
				case Keys.D1:
					CapturedText += IsShiftPressed ? "!" : "1";
					break;
				case Keys.D8:
					CapturedText += IsShiftPressed ? "*" : "8";
					break;
				case Keys.D9:
					CapturedText += IsShiftPressed ? "(" : "9";
					break;
				case Keys.LWin:
					break;
				case Keys.RWin:
					break;
				case Keys.Apps:
					break;
				case Keys.Sleep:
					break;
				case Keys.NumPad0:
					if (IsNumLockOn) CapturedText += "0";
					break;
				case Keys.NumPad1:
					if (IsNumLockOn) CapturedText += "1";
					break;
				case Keys.NumPad2:
					if (IsNumLockOn) CapturedText += "2";
					break;
				case Keys.NumPad3:
					if (IsNumLockOn) CapturedText += "3";
					break;
				case Keys.NumPad4:
					if (IsNumLockOn) CapturedText += "4";
					break;
				case Keys.NumPad5:
					if (IsNumLockOn) CapturedText += "5";
					break;
				case Keys.NumPad6:
					if (IsNumLockOn) CapturedText += "6";
					break;
				case Keys.NumPad7:
					if (IsNumLockOn) CapturedText += "7";
					break;
				case Keys.NumPad8:
					if (IsNumLockOn) CapturedText += "8";
					break;
				case Keys.NumPad9:
					if (IsNumLockOn) CapturedText += "9";
					break;
				case Keys.Multiply:
					CapturedText += "*";
					break;
				case Keys.Add:
					CapturedText += "+";
					break;
				case Keys.Separator:// CapturedText += "/";
					break;
				case Keys.Subtract:
					CapturedText += "-";
					break;				
				case Keys.Divide:
					CapturedText += "/";
					break;
				case Keys.NumLock:
					break;
				case Keys.Scroll:
					break;
				case Keys.LShiftKey:
					break;
				case Keys.RShiftKey:
					break;
				case Keys.LControlKey:
					break;
				case Keys.RControlKey:
					break;
				case Keys.LMenu:
					break;
				case Keys.RMenu:
					break;				
				//case Keys.Oem1:
				//	break;
				case Keys.Oemplus:
					CapturedText += IsShiftPressed ? "+" : "=";
					break;				
				case Keys.OemMinus:
					CapturedText += IsShiftPressed ? "_" : "-";
					break;							
				//case Keys.Oem2:
				//	break;				
				//case Keys.Oem3:
				//break;				
				//case Keys.Oem4:
				//	break;				
				//case Keys.Oem5:
				//	break;				
				//case Keys.Oem6:
				//	break;				
				//case Keys.Oem7:
				//	break;
				case Keys.Oem8:
					break;
				case Keys.OemBackslash:
					break;
				//case Keys.Oem102:
				//	break;
				case Keys.ProcessKey:
					break;
				case Keys.Packet:
					break;
				case Keys.Attn:
					break;
				case Keys.Crsel:
					break;
				case Keys.Exsel:
					break;
				case Keys.EraseEof:
					break;
				case Keys.Play:
					break;
				case Keys.Zoom:
					break;
				case Keys.NoName:
					break;
				case Keys.Pa1:
					break;
				case Keys.OemClear:
					break;
				case Keys.Shift:
					break;
				case Keys.Control:
					break;
				case Keys.Alt:
					break;				
			}
			#endregion

			var input = GetInput();
			var keyLayout = KeyLayout.Unknown;
			switch (Input.ToString())
			{
				case EN_KL:
					keyLayout = KeyLayout.En;
					break;
				case RU_KL:
					keyLayout = KeyLayout.Ru;
					break;
				default:
					throw new Exception("Неподдерживаемая раскладка клавиатуры");
			}

			Trace.WriteLine($"Раскладка: {keyLayout}");
			if (keyLayout == KeyLayout.En)
			{
				switch (keyInfo.key)
				{
					case Keys.D2:
						CapturedText += IsShiftPressed ? "@" : "2";
						break;
					case Keys.D3:
						CapturedText += IsShiftPressed ? "#" : "3";
						break;
					case Keys.D4:
						CapturedText += IsShiftPressed ? "$" : "4";
						break;
					case Keys.D5:
						CapturedText += IsShiftPressed ? "%" : "5";
						break;
					case Keys.D6:
						CapturedText += IsShiftPressed ? "^" : "6";
						break;
					case Keys.D7:
						CapturedText += IsShiftPressed ? "&" : "7";
						break;
					case Keys.A:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "A" : "a";
						break;
					case Keys.B:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "B" : "b";
						break;
					case Keys.C:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "C" : "c";
						break;
					case Keys.D:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "D" : "d";
						break;
					case Keys.E:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "E" : "e";
						break;
					case Keys.F:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "F" : "f";
						break;
					case Keys.G:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "G" : "g";
						break;
					case Keys.H:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "H" : "h";
						break;
					case Keys.I:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "I" : "i";
						break;
					case Keys.J:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "J" : "j";
						break;
					case Keys.K:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "K" : "k";
						break;
					case Keys.L:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "L" : "l";
						break;
					case Keys.M:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "M" : "m";
						break;
					case Keys.N:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "N" : "n";
						break;
					case Keys.O:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "O" : "o";
						break;
					case Keys.P:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "P" : "p";
						break;
					case Keys.Q:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "Q" : "q";
						break;
					case Keys.R:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "R" : "r";
						break;
					case Keys.S:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "S" : "s";
						break;
					case Keys.T:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "T" : "t";
						break;
					case Keys.U:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "U" : "u";
						break;
					case Keys.V:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "V" : "v";
						break;
					case Keys.W:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "W" : "w";
						break;
					case Keys.X:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "X" : "x";
						break;
					case Keys.Y:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "Y" : "y";
						break;
					case Keys.Z:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "Z" : "z";
						break;
					case Keys.OemSemicolon:
						CapturedText += IsShiftPressed ? ":" : ";";
						break;
					//case Keys.Oem1:
					//	break;					
					case Keys.Oemcomma:
						CapturedText += IsShiftPressed ? "<" : ",";
						break;
					case Keys.OemPeriod:
						CapturedText += IsShiftPressed ? ">" : ".";
						break;
					case Keys.OemQuestion:
						CapturedText += IsShiftPressed ? "?" : "/";
						break;
					//case Keys.Oem2:
					//	break;
					case Keys.Oemtilde:
						CapturedText += IsShiftPressed ? "~" : "`";
						break;
					//case Keys.Oem3:
					//break;
					case Keys.OemOpenBrackets:
						CapturedText += IsShiftPressed ? "{" : "[";
						break;
					//case Keys.Oem4:
					//	break;
					case Keys.OemPipe:
						CapturedText += IsShiftPressed ? "|" : "\\";
						break;
					//case Keys.Oem5:
					//	break;
					case Keys.OemCloseBrackets:
						CapturedText += IsShiftPressed ? "}" : "]";
						break;
					//case Keys.Oem6:
					//	break;
					case Keys.OemQuotes:
						CapturedText += IsShiftPressed ? "\"" : "'";
						break;
					case Keys.Decimal:
						CapturedText += ".";
						break;
					//case Keys.Oem7:
					//	break;
					case Keys.Oem8:
						break;
					case Keys.OemBackslash:
						break;
					//case Keys.Oem102:
					//	break;
					case Keys.ProcessKey:
						break;
					case Keys.Packet:
						break;
					case Keys.Attn:
						break;
					case Keys.Crsel:
						break;
					case Keys.NoName:
						break;
					case Keys.Pa1:
						break;
					case Keys.OemClear:
						break;
				}
			}

			if (keyLayout == KeyLayout.Ru)
			{
				switch (keyInfo.key)
				{
					case Keys.D2:
						CapturedText += IsShiftPressed ? "\"" : "2";
						break;
					case Keys.D3:
						CapturedText += IsShiftPressed ? "№" : "3";
						break;
					case Keys.D4:
						CapturedText += IsShiftPressed ? ";" : "4";
						break;
					case Keys.D5:
						CapturedText += IsShiftPressed ? "%" : "5";
						break;
					case Keys.D6:
						CapturedText += IsShiftPressed ? ":" : "6";
						break;
					case Keys.D7:
						CapturedText += IsShiftPressed ? "?" : "7";
						break;
					case Keys.A:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "Ф" : "ф";
						break;
					case Keys.B:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "И" : "и";
						break;
					case Keys.C:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "С" : "с";
						break;
					case Keys.D:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "В" : "в";
						break;
					case Keys.E:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "У" : "у";
						break;
					case Keys.F:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "А" : "а";
						break;
					case Keys.G:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "П" : "п";
						break;
					case Keys.H:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "Р" : "р";
						break;
					case Keys.I:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "Ш" : "ш";
						break;
					case Keys.J:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "О" : "о";
						break;
					case Keys.K:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "Л" : "л";
						break;
					case Keys.L:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "Д" : "д";
						break;
					case Keys.M:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "Ь" : "ь";
						break;
					case Keys.N:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "Т" : "т";
						break;
					case Keys.O:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "Щ" : "щ";
						break;
					case Keys.P:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "З" : "з";
						break;
					case Keys.Q:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "Й" : "й";
						break;
					case Keys.R:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "К" : "к";
						break;
					case Keys.S:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "Ы" : "ы";
						break;
					case Keys.T:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "Е" : "е";
						break;
					case Keys.U:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "Г" : "г";
						break;
					case Keys.V:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "М" : "м";
						break;
					case Keys.W:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "Ц" : "ц";
						break;
					case Keys.X:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "Ч" : "ч";
						break;
					case Keys.Y:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "Н" : "н";
						break;
					case Keys.Z:
						CapturedText += IsCapsLockOn || IsShiftPressed ? "Я" : "я";
						break;
					case Keys.OemSemicolon:
						CapturedText += IsShiftPressed ? "Ж" : "ж";
						break;
					//case Keys.Oem1:
					//	break;					
					case Keys.Oemcomma:
						CapturedText += IsShiftPressed ? "Б" : "б";
						break;
					case Keys.OemPeriod:
						CapturedText += IsShiftPressed ? "Ю" : "ю";
						break;
					case Keys.OemQuestion:
						CapturedText += IsShiftPressed ? "," : ".";
						break;
					//case Keys.Oem2:
					//	break;
					case Keys.Oemtilde:
						CapturedText += IsShiftPressed ? "Ё" : "ё";
						break;
					//case Keys.Oem3:
					//break;
					case Keys.OemOpenBrackets:
						CapturedText += IsShiftPressed ? "Х" : "х";
						break;
					//case Keys.Oem4:
					//	break;
					case Keys.OemPipe:
						CapturedText += IsShiftPressed ? "/" : "\\";
						break;
					//case Keys.Oem5:
					//	break;
					case Keys.OemCloseBrackets:
						CapturedText += IsShiftPressed ? "Ъ" : "ъ";
						break;
					//case Keys.Oem6:
					//	break;
					case Keys.OemQuotes:
						CapturedText += IsShiftPressed ? "Э" : "э";
						break;
					case Keys.Decimal:
						CapturedText += ",";
						break;
					//case Keys.Oem7:
					//	break;
					case Keys.Oem8:
						break;
					case Keys.OemBackslash:
						break;
					//case Keys.Oem102:
					//	break;
					case Keys.ProcessKey:
						break;
					case Keys.Packet:
						break;
					case Keys.Attn:
						break;
					case Keys.Crsel:
						break;
					case Keys.NoName:
						break;
					case Keys.Pa1:
						break;
					case Keys.OemClear:
						break;
				}
			}

		}

		private StringBuilder GetInput()
		{
			IntPtr layout = GetKeyboardLayout(GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero));
			//ActivateKeyboardLayout((int)layout, 100);
			GetKeyboardLayoutName(Input);
			return Input;
		}
	}
}
