using System;
using System.Runtime.InteropServices;


namespace HookLibrary
{

	#region HookEventArgs

	public class HookEventArgs : EventArgs
	{
		public int HookCode;	// Hook code
		public IntPtr wParam;	// WPARAM argument
		public IntPtr lParam;	// LPARAM argument
	}

	#endregion

	#region Hook Delegate

	public delegate int HookProc(int code, IntPtr wParam, IntPtr lParam);
	public delegate void HookEventHandler(object sender, HookEventArgs e);

	#endregion

	#region Hook Enum

	public enum HookType : int
	{
		WH_JOURNALRECORD = 0,
		WH_JOURNALPLAYBACK = 1,
		WH_KEYBOARD = 2,
		WH_GETMESSAGE = 3,
		WH_CALLWNDPROC = 4,
		WH_CBT = 5,
		WH_SYSMSGFILTER = 6,
		WH_MOUSE = 7,
		WH_HARDWARE = 8,
		WH_DEBUG = 9,
		WH_SHELL = 10,
		WH_FOREGROUNDIDLE = 11,
		WH_CALLWNDPROCRET = 12,
		WH_KEYBOARD_LL = 13,
		WH_MOUSE_LL = 14
	}


	#endregion

	#region Hook wrapper class

	public class WindowsHook : IDisposable
	{

		#region Data members

		private IntPtr m_hook;
		private HookType m_hooktype;
		private HookProc m_hookproc;

		#endregion

		#region Events

		public event HookEventHandler HookInvoked;
		
		protected void OnHookInvoked(HookEventArgs e)
		{
			if (HookInvoked != null)
				HookInvoked(this, e);
		}

		#endregion

		#region Properties

		public IntPtr hHook
		{
			get
			{
				return m_hook;
			}
		}

		#endregion

		#region Constructors

		public WindowsHook(HookType HookType)
		{
			m_hooktype = HookType;
			m_hookproc = new HookProc(HookProcedure);
		}

		#endregion

		#region Dispose

		public void Dispose()
		{
			Uninstall();
		}
	
		#endregion

		#region Install/Uninstall

		public void Install ()
		{
			m_hook = SetWindowsHookEx(m_hooktype, m_hookproc, IntPtr.Zero, (uint)AppDomain.GetCurrentThreadId());
		}

		public void Uninstall()
		{
			if (m_hook != IntPtr.Zero)
			{
				UnhookWindowsHookEx(m_hook);
			}
		}




		#endregion

		#region Hook procedure

		protected  int HookProcedure (int code, IntPtr wParam, IntPtr lParam)
		{
			HookEventArgs e = new HookEventArgs();
			e.HookCode = code;
			e.wParam = wParam;
			e.lParam = lParam;
			OnHookInvoked(e);

			return CallNextHookEx(m_hook, code, wParam, lParam);
		}


		#endregion

		#region API Calls 

		[DllImport("user32.dll")]
		static extern IntPtr SetWindowsHookEx(HookType hook, HookProc callback, IntPtr hMod, uint dwThreadId);


		[DllImport("user32.dll")]
		static extern bool UnhookWindowsHookEx(IntPtr hhk);

		[DllImport("user32.dll")]
		static extern int CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam,   IntPtr lParam);


		#endregion

	}

	#endregion
}
