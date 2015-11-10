using System;
using System.Collections;
using System.Collections.Specialized;
using System.Runtime.InteropServices;
using System.Windows.Forms;


namespace HookLibrary
{
	#region KeyboardHookEventArgs

	public class KeyboardHookEventArgs : EventArgs
	{
		public KeyStroke KeyStroke;

		public KeyboardHookEventArgs (KeyStroke Key)
		{
			this.KeyStroke = Key;
		}

	}


	#endregion

	#region Keystroke class 

	/// <summary>
	/// This class encpasulates a keystroke - either one captured by the hook or defined as a filter to capture.
	/// </summary>
	public class KeyStroke
	{
		/// <summary>
		/// The Key pressed.
		/// </summary>
		public System.Windows.Forms.Keys KeyCode;

		/// <summary>
		/// The state of the Control key.
		/// </summary>
		public bool Ctrl;

		/// <summary>
		/// The state of the Alt key.
		/// </summary>
		public bool Alt;

		/// <summary>
		/// The state of the Shift key.
		/// </summary>
		public bool Shift;

		public KeyStroke() : this(Keys.None, false, false, false)
		{	}

		public KeyStroke (Keys KeyCode, bool Ctrl, bool Alt, bool Shift)
		{
			this.KeyCode = KeyCode;
			this.Ctrl = Ctrl;
			this.Alt = Alt;
			this.Shift = Shift;
		}

		/// <summary>
		/// Outputs the key combination in the format of (Ctrl)-(Alt)-(Shift)-(Key), where applicable.
		/// </summary>
		/// <returns></returns>
		public override string ToString()
		{
			string Ctrl = this.Ctrl ? "Ctrl-" : "";
			string Alt = this.Alt ? "Alt-" : "";
			string Shift = this.Shift ? "Shift-" : "";
			
			return Ctrl + Alt + Shift +  this.KeyCode.ToString();
		}

		/// <summary>
		/// Checks if the given keystroke matches the filter.
		/// Matching 
		/// </summary>
		/// <param name="keystroke"></param>
		/// <returns></returns>
		public bool IsMatch (KeyStroke keystroke)
		{
			return 
				this.KeyCode == Keys.None ? true : keystroke.KeyCode == this.KeyCode &&
				keystroke.Ctrl == this.Ctrl &&
				keystroke.Alt == this.Alt &&
				keystroke.Shift == this.Shift;
		}



	}

	#endregion

	#region KeyboardHook Delegate

	public delegate void KeyboardHookEventHandler(object sender, KeyboardHookEventArgs e);

	#endregion

	#region Keyboard Message Flags class

	class KeyboardMessageFlags
	{
		private BitVector32 flags;
		private BitVector32.Section repeatCount;
		private BitVector32.Section scanCode;
		private BitVector32.Section extendedKey;
		private BitVector32.Section reserved;
		private BitVector32.Section contextCode;
		private BitVector32.Section previousKeyState;
		private BitVector32.Section transitionState;

		public override string ToString()
		{
			return this.flags.ToString();
		}


		public KeyboardMessageFlags(int lParam)
		{
			this.flags = new BitVector32(lParam);
			repeatCount = BitVector32.CreateSection(short.MaxValue);
			scanCode = BitVector32.CreateSection(byte.MaxValue, repeatCount);
			extendedKey = BitVector32.CreateSection(1, scanCode);
			reserved = BitVector32.CreateSection(8, extendedKey);
			contextCode = BitVector32.CreateSection(1, reserved);
			previousKeyState = BitVector32.CreateSection(1, contextCode);
			transitionState = BitVector32.CreateSection(1, previousKeyState);
		}

		public bool IsBeingReleased
		{
			get
			{
				return flags[transitionState] == 1;
			}
		}

		public bool IsRepeatedKey
		{
			get {
				
				return flags[previousKeyState] == 1; }
		}

		public bool AltPressed
		{
			get { return flags[contextCode] == 1; }
		}

		public short RepeatCount
		{
			get {
				return (short)flags[repeatCount]; }
		}

		public byte ScanCode 
		{
			get { return (byte)flags[scanCode]; }
		}

	}

	#endregion

	public class KeyboardHook : WindowsHook
	{

		#region Data members

		ArrayList m_Filters = new ArrayList();
		
		#endregion

		#region Constructor

		public KeyboardHook() : base(HookType.WH_KEYBOARD)
		{
			this.HookInvoked += new HookEventHandler(KeyboardHook_HookInvoked);
		}

		#endregion

		#region Filter methods

		public void AddFilter(Keys KeyCode, bool Ctrl, bool Alt, bool Shift)
		{
			
			KeyStroke newFilter = new KeyStroke();
			newFilter.KeyCode = KeyCode;
			newFilter.Shift = Shift;
			newFilter.Alt = Alt;
			newFilter.Ctrl = Ctrl;

			m_Filters.Add(newFilter);
		}

		private bool CheckFilters(KeyStroke state)
		{
			if (m_Filters.Count == 0)
				return true;

			foreach (KeyStroke filter in m_Filters)
			{
				if (filter.IsMatch(state))
					return true;
			}

			return false;
		}

		#endregion

		#region Events
        
		public event KeyboardHookEventHandler KeyPressed;
		protected void OnKeyPressed(KeyboardHookEventArgs e)
		{
			if (KeyPressed != null)
				KeyPressed(this, e);
		}

		#endregion

		#region Event handlers

		private void KeyboardHook_HookInvoked(object sender, HookEventArgs e)
		{
		    try
		    {
			if (e.HookCode != 0)
				return;
			
			KeyStroke key = new KeyStroke();
			key.KeyCode = (Keys)e.wParam.ToInt32();
			key.Shift = GetKeyState(VirtualKeys.VK_SHIFT)  <= -127;
			key.Alt = GetKeyState(VirtualKeys.VK_MENU) <= -127;
			key.Ctrl = GetKeyState(VirtualKeys.VK_CONTROL) <= -127;

			KeyboardHookEventArgs keyboardEventArgs  = new KeyboardHookEventArgs(key);
			
			KeyboardMessageFlags kmf = new KeyboardMessageFlags(e.lParam.ToInt32());
			
			if (CheckFilters(key)  && !kmf.IsBeingReleased)
			{
					OnKeyPressed (keyboardEventArgs);
			}
            }
            catch (Exception)
            {
            }
		}

		#region API Calls

		// Key states

		public enum VirtualKeys
		{
			VK_SHIFT	=	 0x10,
			VK_CONTROL	=	 0x11,		
			VK_MENU	=	 0x12	//ALT
	}

		[DllImport("user32.dll")]
		static extern short GetKeyState(VirtualKeys nVirtKey);

		#endregion

		#endregion
	}
}
