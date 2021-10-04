using System;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Security.Cryptography.Xml;

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

		private bool _isInit = false;

		/// <summary/>		
		public void Init()
		{
			if (_isInit) return;

			ProcessModule currentModule = Process.GetCurrentProcess().MainModule;
			keyboardProcess = new LowLevelKeyboardProc(CaptureKey);
			ptrHook = SetWindowsHookEx(WH_KEYBOARD_LL, keyboardProcess, GetModuleHandle(currentModule.ModuleName), 0);

			_isInit = true;
		}

		/// <summary>
		/// Включить захват текста
		/// </summary>
		public void StartCapture()
		{
			_isCapturedStarted = true;
			SetCapturedText("");
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
		public string CapturedText => _capturedText;

		private void SetCapturedText(string value)
		{
			if (string.IsNullOrEmpty(_capturedText) || string.IsNullOrEmpty(value))
			{
				_capturedText = value;
				_cursorPosition = _capturedText.Length;
			}
			else if (_cursorPosition == _capturedText.Length)
			{
				_capturedText += value;
				_cursorPosition = _capturedText.Length;
			}
			else if (_cursorPosition == 0)
			{
				_capturedText = value + _capturedText;
				_cursorPosition++;
			}
			else
			{
				_capturedText = _capturedText.Substring(0, _cursorPosition - 1) + value + _capturedText.Substring(_cursorPosition, _capturedText.Length - _cursorPosition);
				_cursorPosition++;
			}

			CapturedTextChanged?.Invoke(this, EventArgs.Empty);
		}

		int _cursorPosition = 0;

		private void CursorForward()
		{
			if (_cursorPosition < _capturedText.Length)
			{
				++_cursorPosition;
			}
		}

		private void CursorBackward()
		{
			if (_cursorPosition > 0)
			{
				--_cursorPosition;
			}
		}

		private void CursorBackSpace()
		{
			FixCursor();
			if (string.IsNullOrEmpty(_capturedText) || _cursorPosition == 0) return;

			if (_cursorPosition == _capturedText.Length)
			{
				_capturedText = _capturedText.Substring(0, _cursorPosition - 1);
			}
			else
			{
				_capturedText = _capturedText.Substring(0, _cursorPosition - 1) + _capturedText.Substring(_cursorPosition, _capturedText.Length - _cursorPosition);
			}
			_cursorPosition--;
			CapturedTextChanged?.Invoke(this, EventArgs.Empty);
		}

		private void CursorDelete()
		{
			FixCursor();
			if (string.IsNullOrEmpty(_capturedText) || _cursorPosition == _capturedText.Length) return;

			_capturedText = _capturedText.Substring(0, _cursorPosition) + _capturedText.Substring(_cursorPosition + 1, _capturedText.Length - _cursorPosition - 1);

			CapturedTextChanged?.Invoke(this, EventArgs.Empty);
		}

		private void FixCursor()
		{
			if (_cursorPosition > _capturedText.Length)
				_cursorPosition = _capturedText.Length;
			if (_cursorPosition < 0)
				_cursorPosition = 0;
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

		private const string PRESSED_MSG = "НАЖАТ";
		private const string NOT_PRESSED_MSG = "ОТПУЩЕНА";
		private const string EN_KL = "00000409";
		private const string RU_KL = "00000419";
		private static readonly object _locker = new object();

		/// <summary>
		/// Очередь обработки ввода
		/// </summary>
		private readonly ConcurrentQueue<(KBDLLHOOKSTRUCT keyInfo, IntPtr wParam)> _capturedQueue = new ConcurrentQueue<(KBDLLHOOKSTRUCT keyInfo, IntPtr wParam)>();

		private IntPtr CaptureKey(int nCode, IntPtr wParam, IntPtr lParam)
		{
			if (nCode >= 0)
			{
				var keyInfo = (KBDLLHOOKSTRUCT)Marshal.PtrToStructure(lParam, typeof(KBDLLHOOKSTRUCT));
				(KBDLLHOOKSTRUCT, IntPtr) keyStepParams = (keyInfo, wParam);
				_capturedQueue.Enqueue(keyStepParams);

				//KBDLLHOOKSTRUCT newKeyInfo = new KBDLLHOOKSTRUCT { 
				//	key  = Keys.Multiply
				//};
				//Marshal.StructureToPtr(newKeyInfo, lParam, true);
				//return CallNextHookEx(ptrHook, nCode, wParam, lParam);
			}
			ProcessQueue();
			//отменить
			//return (IntPtr)1;			
			return CallNextHookEx(ptrHook, nCode, wParam, lParam);
		}

		/// <summary>
		/// идет обработка очереди
		/// </summary>
		private volatile bool _isProcessingQueue = false;

		private void ProcessQueue()
		{
			if (_isProcessingQueue) return;

			lock(_locker)
			{
				_isProcessingQueue = true;
			}

			while (_capturedQueue.Count > 0)
			{
				Trace.WriteLine($"Элементов в очереди обработки {_capturedQueue.Count}");

				if (!_capturedQueue.TryDequeue(out (KBDLLHOOKSTRUCT keyInfo, IntPtr wParam) keyStep)) continue;				

				var wParam = keyStep.wParam;
				var keyInfo = keyStep.keyInfo;
				var keyStateStr = wParam == (IntPtr)WM_KEYDOWN ? PRESSED_MSG : NOT_PRESSED_MSG;
				Trace.WriteLine($"Начата обработка очереди {keyStateStr} {keyInfo.key}");

				if (wParam == (IntPtr)WM_KEYDOWN)
				{
					Trace.WriteLine($"Нажата клавиша: {keyInfo.key}");
					switch (keyInfo.key)
					{
						case Keys.LShiftKey:
						case Keys.RShiftKey:
						case Keys.Shift:
						case Keys.ShiftKey:
							lock (_locker)
							{
								_isShiftPressed = true;
							}
							Trace.WriteLine($"Включили флаг: {keyInfo.key}");
							break;
						default:
							break;
					}
				}
				if (wParam == (IntPtr)WM_KEYUP)
				{
					switch (keyInfo.key)
					{
						case Keys.LShiftKey:
						case Keys.RShiftKey:
						case Keys.Shift:
						case Keys.ShiftKey:
							lock (_locker)
							{
								_isShiftPressed = false;
							}
							Trace.WriteLine($"Выключили флаг: {keyInfo.key}");
							break;
						default:
							break;
					}
					Trace.WriteLine($"Отпущена клавиша: {keyInfo.key}");
				}

				//var shiftState = GetKeyState((int)Keys.Shift);
				//Trace.WriteLine($"Состояние Shift: {shiftState} {(shiftState < 0 ? PRESSED_MSG : NOT_PRESSED_MSG)}");

				//var lshiftState = GetKeyState((int)Keys.LShiftKey);
				//Trace.WriteLine($"Состояние LShift: {lshiftState} {(lshiftState < 0 ? PRESSED_MSG : NOT_PRESSED_MSG)}");

				//var rshiftState = GetKeyState((int)Keys.RShiftKey);
				//Trace.WriteLine($"Состояние RShift: {rshiftState} {(rshiftState < 0 ? PRESSED_MSG : NOT_PRESSED_MSG)}");

				//var numLockState = GetKeyState((int)Keys.NumLock);
				//Trace.WriteLine($"Состояние NumLock: {numLockState} {(numLockState < 0 ? PRESSED_MSG : NOT_PRESSED_MSG)}");

				//var capsLockState = GetKeyState((int)Keys.CapsLock);
				//Trace.WriteLine($"Состояние CapsLock: {capsLockState} {(capsLockState < 0 ? PRESSED_MSG : NOT_PRESSED_MSG)}");

				if (_isCapturedStarted && wParam == (IntPtr)WM_KEYDOWN)
				{
					Trace.WriteLine($"Передана в обработку клавиша: {keyStateStr} {keyInfo.key} IsShiftPressed={IsShiftPressed}");
					ProcessCapture(keyInfo, IsShiftPressed);
				}
				Trace.WriteLine($"Закончена обработка очереди  {keyStateStr} {keyInfo.key}");
			}

			lock (_locker)
			{
				_isProcessingQueue = false;
			}
		}

		/// <summary>
		/// Сюда запоминаем когда нажат шифт
		/// </summary>
		volatile bool _isShiftPressed = (GetKeyState((int)Keys.LShiftKey) < 0) || (GetKeyState((int) Keys.RShiftKey) < 0);		
		public bool IsShiftPressed => _isShiftPressed;
		public bool IsCapsLockOn => GetKeyState((int)Keys.CapsLock) == 1;

		/// <summary>
		/// Большая или маленькая буква должна быть
		/// </summary>
		public bool IsCapsLiteral(bool isShiftPressed)
		{
			return (IsCapsLockOn && !isShiftPressed) || (!IsCapsLockOn && isShiftPressed);
		}
		public bool IsNumLockOn => GetKeyState((int)Keys.NumLock) == 1;

		private enum KeyLayout
		{
			En,
			Ru,
			Unknown
		}

		private void ProcessCapture(KBDLLHOOKSTRUCT keyInfo, bool isShiftPressed)
		{
			Trace.WriteLine($"Обработка {keyInfo.key} Шифт {(isShiftPressed ? "нажата" : "отпущена")}");
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
					CursorBackSpace();
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
					SetCapturedText(" ");
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
					_cursorPosition = _capturedText?.Length ?? 0;
					break;
				case Keys.Home:
					_cursorPosition = 0;
					break;
				case Keys.Left:
					CursorBackward();
					break;
				case Keys.Up:
					break;
				case Keys.Right:
					CursorForward();
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
					CursorDelete();
					break;
				case Keys.Help:
					break;
				case Keys.D0:
					SetCapturedText(IsShiftPressed ? ")" : "0");
					break;
				case Keys.D1:
					SetCapturedText(IsShiftPressed ? "!" : "1");
					break;
				case Keys.D8:
					SetCapturedText(IsShiftPressed ? "*" : "8");
					break;
				case Keys.D9:
					SetCapturedText(IsShiftPressed ? "(" : "9");
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
					if (IsNumLockOn) SetCapturedText("0");
					break;
				case Keys.NumPad1:
					if (IsNumLockOn) SetCapturedText("1");
					break;
				case Keys.NumPad2:
					if (IsNumLockOn) SetCapturedText("2");
					break;
				case Keys.NumPad3:
					if (IsNumLockOn) SetCapturedText("3");
					break;
				case Keys.NumPad4:
					if (IsNumLockOn)
						SetCapturedText("4");
					else
						CursorBackward();
					break;
				case Keys.NumPad5:
					if (IsNumLockOn) SetCapturedText("5");
					break;
				case Keys.NumPad6:
					if (IsNumLockOn)
						SetCapturedText("6");
					else
						CursorForward();
					break;
				case Keys.NumPad7:
					if (IsNumLockOn) SetCapturedText("7");
					break;
				case Keys.NumPad8:
					if (IsNumLockOn) SetCapturedText("8");
					break;
				case Keys.NumPad9:
					if (IsNumLockOn) SetCapturedText("9");
					break;
				case Keys.Multiply:
					SetCapturedText("*");
					break;
				case Keys.Add:
					SetCapturedText("+");
					break;
				case Keys.Separator:;
					break;
				case Keys.Subtract:
					SetCapturedText("-");
					break;
				case Keys.Divide:
					SetCapturedText("/");
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
					SetCapturedText(IsShiftPressed ? "+" : "=");
					break;
				case Keys.OemMinus:
					SetCapturedText(IsShiftPressed ? "_" : "-");
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
			var keyLayout = Input.ToString() switch
			{
				EN_KL => KeyLayout.En,
				RU_KL => KeyLayout.Ru,
				_ => throw new Exception("Неподдерживаемая раскладка клавиатуры"),
			};

			Trace.WriteLine($"Раскладка: {keyLayout}");
			if (keyLayout == KeyLayout.En)
			{
				switch (keyInfo.key)
				{
					case Keys.D2:
						SetCapturedText(IsShiftPressed ? "@" : "2");
						break;
					case Keys.D3:
						SetCapturedText(IsShiftPressed ? "#" : "3");
						break;
					case Keys.D4:
						SetCapturedText(IsShiftPressed ? "$" : "4");
						break;
					case Keys.D5:
						SetCapturedText(IsShiftPressed ? "%" : "5");
						break;
					case Keys.D6:
						SetCapturedText(IsShiftPressed ? "^" : "6");
						break;
					case Keys.D7:
						SetCapturedText(IsShiftPressed ? "&" : "7");
						break;
					case Keys.A:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "A" : "a");
						break;
					case Keys.B:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "B" : "b");
						break;
					case Keys.C:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "C" : "c");
						break;
					case Keys.D:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "D" : "d");
						break;
					case Keys.E:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "E" : "e");
						break;
					case Keys.F:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "F" : "f");
						break;
					case Keys.G:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "G" : "g");
						break;
					case Keys.H:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "H" : "h");
						break;
					case Keys.I:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "I" : "i");
						break;
					case Keys.J:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "J" : "j");
						break;
					case Keys.K:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "K" : "k");
						break;
					case Keys.L:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "L" : "l");
						break;
					case Keys.M:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "M" : "m");
						break;
					case Keys.N:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "N" : "n");
						break;
					case Keys.O:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "O" : "o");
						break;
					case Keys.P:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "P" : "p");
						break;
					case Keys.Q:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "Q" : "q");
						break;
					case Keys.R:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "R" : "r");
						break;
					case Keys.S:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "S" : "s");
						break;
					case Keys.T:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "T" : "t");
						break;
					case Keys.U:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "U" : "u");
						break;
					case Keys.V:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "V" : "v");
						break;
					case Keys.W:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "W" : "w");
						break;
					case Keys.X:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "X" : "x");
						break;
					case Keys.Y:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "Y" : "y");
						break;
					case Keys.Z:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "Z" : "z");
						break;
					case Keys.OemSemicolon:
						SetCapturedText(IsShiftPressed ? ":" : ";");
						break;
					//case Keys.Oem1:
					//	break;					
					case Keys.Oemcomma:
						SetCapturedText(IsShiftPressed ? "<" : ",");
						break;
					case Keys.OemPeriod:
						SetCapturedText(IsShiftPressed ? ">" : ".");
						break;
					case Keys.OemQuestion:
						SetCapturedText(IsShiftPressed ? "?" : "/");
						break;
					//case Keys.Oem2:
					//	break;
					case Keys.Oemtilde:
						SetCapturedText(IsShiftPressed ? "~" : "`");
						break;
					//case Keys.Oem3:
					//break;
					case Keys.OemOpenBrackets:
						SetCapturedText(IsShiftPressed ? "{" : "[");
						break;
					//case Keys.Oem4:
					//	break;
					case Keys.OemPipe:
						SetCapturedText(IsShiftPressed ? "|" : "\\");
						break;
					//case Keys.Oem5:
					//	break;
					case Keys.OemCloseBrackets:
						SetCapturedText(IsShiftPressed ? "}" : "]");
						break;
					//case Keys.Oem6:
					//	break;
					case Keys.OemQuotes:
						SetCapturedText(IsShiftPressed ? "\"" : "'");
						break;
					case Keys.Decimal:
						SetCapturedText(".");
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
						SetCapturedText(IsShiftPressed ? "\"" : "2");
						break;
					case Keys.D3:
						SetCapturedText(IsShiftPressed ? "№" : "3");
						break;
					case Keys.D4:
						SetCapturedText(IsShiftPressed ? ";" : "4");
						break;
					case Keys.D5:
						SetCapturedText(IsShiftPressed ? "%" : "5");
						break;
					case Keys.D6:
						SetCapturedText(IsShiftPressed ? ":" : "6");
						break;
					case Keys.D7:
						SetCapturedText(IsShiftPressed ? "?" : "7");
						break;
					case Keys.A:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "Ф" : "ф");
						break;
					case Keys.B:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "И" : "и");
						break;
					case Keys.C:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "С" : "с");
						break;
					case Keys.D:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "В" : "в");
						break;
					case Keys.E:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "У" : "у");
						break;
					case Keys.F:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "А" : "а");
						break;
					case Keys.G:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "П" : "п");
						break;
					case Keys.H:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "Р" : "р");
						break;
					case Keys.I:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "Ш" : "ш");
						break;
					case Keys.J:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "О" : "о");
						break;
					case Keys.K:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "Л" : "л");
						break;
					case Keys.L:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "Д" : "д");
						break;
					case Keys.M:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "Ь" : "ь");
						break;
					case Keys.N:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "Т" : "т");
						break;
					case Keys.O:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "Щ" : "щ");
						break;
					case Keys.P:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "З" : "з");
						break;
					case Keys.Q:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "Й" : "й");
						break;
					case Keys.R:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "К" : "к");
						break;
					case Keys.S:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "Ы" : "ы");
						break;
					case Keys.T:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "Е" : "е");
						break;
					case Keys.U:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "Г" : "г");
						break;
					case Keys.V:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "М" : "м");
						break;
					case Keys.W:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "Ц" : "ц");
						break;
					case Keys.X:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "Ч" : "ч");
						break;
					case Keys.Y:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "Н" : "н");
						break;
					case Keys.Z:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "Я" : "я");
						break;
					case Keys.OemSemicolon:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "Ж" : "ж");
						break;
					//case Keys.Oem1:
					//	break;					
					case Keys.Oemcomma:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "Б" : "б");
						break;
					case Keys.OemPeriod:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "Ю" : "ю");
						break;
					case Keys.OemQuestion:
						SetCapturedText(IsShiftPressed ? "," : ".");
						break;
					//case Keys.Oem2:
					//	break;
					case Keys.Oemtilde:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "Ё" : "ё");
						break;
					//case Keys.Oem3:
					//break;
					case Keys.OemOpenBrackets:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "Х" : "х");
						break;
					//case Keys.Oem4:
					//	break;
					case Keys.OemPipe:
						SetCapturedText(IsShiftPressed ? "/" : "\\");
						break;
					//case Keys.Oem5:
					//	break;
					case Keys.OemCloseBrackets:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "Ъ" : "ъ");
						break;
					//case Keys.Oem6:
					//	break;
					case Keys.OemQuotes:
						SetCapturedText(IsCapsLiteral(isShiftPressed) ? "Э" : "э");
						break;
					case Keys.Decimal:
						SetCapturedText(",");
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
