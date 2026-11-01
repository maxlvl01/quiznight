// The monitor's real resolutions (Windows): what it supports, and switching it for fullscreen like other games do.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Godot;

namespace QuizNight.Storage;

/// <summary>
/// Fullscreen at another resolution than the desktop's. Godot can't draw the game at fewer pixels than the screen without
/// laying it out at that size too (everything would shrink), so like other games we ask Windows to switch the monitor
/// itself (CDS_FULLSCREEN: a temporary change that never touches the desktop's saved resolution) and switch it back when
/// the game leaves fullscreen, loses focus or closes. Godot numbers screens in EnumDisplayMonitors' order, so screen i is
/// the i-th monitor Windows lists. Other systems: no list, nothing switched.
/// </summary>
public static class DisplayModes
{
	public static bool Supported => OperatingSystem.IsWindows() && DisplayServer.GetName() == "Windows";

	/// <summary>The monitor whose resolution we changed ("\\.\DISPLAY2"), to switch it back; null while none is changed.</summary>
	static string? _changed;
	public static bool Changed => _changed != null;

	/// <summary>The monitor's own resolution as set in Windows (not a switched one), or the screen's size elsewhere.</summary>
	public static Vector2I Desktop(int screen)
	{
		if (Supported && Device(screen) is { } dev && Read(dev, EnumRegistrySettings) is { } m) return new((int)m.dmPelsWidth, (int)m.dmPelsHeight);
		return DisplayServer.ScreenGetSize(screen);
	}

	/// <summary>Every resolution the monitor supports (at least 1024 × 720, as the game's layout needs), smallest first.</summary>
	public static List<Vector2I> For(int screen)
	{
		var list = new List<Vector2I>();
		if (!Supported || Device(screen) is not { } dev) return list;
		for (var i = 0; Read(dev, i) is { } m; i++)
			if (m.dmPelsWidth >= 1024 && m.dmPelsHeight >= 720) list.Add(new((int)m.dmPelsWidth, (int)m.dmPelsHeight));
		return list.Distinct().OrderBy(s => s.X * s.Y).ThenBy(s => s.X).ToList();
	}

	/// <summary>Switches the monitor to this resolution at its highest refresh rate there; false if Windows refuses.</summary>
	public static bool Use(int screen, Vector2I size)
	{
		if (!Supported || Device(screen) is not { } dev) return false;
		if (size == Desktop(screen)) return Restore();
		DevMode? best = null;
		for (var i = 0; Read(dev, i) is { } m; i++)
			if (m.dmPelsWidth == size.X && m.dmPelsHeight == size.Y && (best == null || m.dmDisplayFrequency > best.Value.dmDisplayFrequency || m.dmDisplayFrequency == best.Value.dmDisplayFrequency && m.dmBitsPerPel > best.Value.dmBitsPerPel))
				best = m;
		if (best is not { } mode) return false;
		// (another monitor switched before goes back first: only one at a time)
		if (_changed != null && _changed != dev) Restore();
		mode.dmFields = DmPelsWidth | DmPelsHeight | DmDisplayFrequency | DmBitsPerPel;
		var result = ChangeDisplaySettingsExW(dev, ref mode, IntPtr.Zero, CdsFullscreen, IntPtr.Zero);
		if (result != 0)
		{
			GD.PushWarning($"Display mode {size.X}x{size.Y} on {dev} refused ({result})");
			return false;
		}
		_changed = dev;
		return true;
	}

	/// <summary>The switched monitor back at the desktop's resolution (nothing if none was switched).</summary>
	public static bool Restore()
	{
		if (_changed == null) return true;
		var ok = ChangeDisplaySettingsExW(_changed, IntPtr.Zero, IntPtr.Zero, 0, IntPtr.Zero) == 0;
		_changed = null;
		return ok;
	}

	// ---------- Windows ----------

	const int EnumRegistrySettings = -2;
	const uint DmBitsPerPel = 0x40000, DmPelsWidth = 0x80000, DmPelsHeight = 0x100000, DmDisplayFrequency = 0x400000;
	const uint CdsFullscreen = 0x4;

	static DevMode? Read(string device, int index)
	{
		var m = new DevMode { dmSize = (ushort)Marshal.SizeOf<DevMode>() };
		return EnumDisplaySettingsW(device, index, ref m) ? m : null;
	}

	/// <summary>The monitor's device name for Godot's screen number.</summary>
	static string? Device(int screen)
	{
		var names = new List<string>();
		EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (mon, _, _, _) =>
		{
			var info = new MonitorInfoEx { cbSize = Marshal.SizeOf<MonitorInfoEx>() };
			names.Add(GetMonitorInfoW(mon, ref info) ? info.szDevice : "");
			return true;
		}, IntPtr.Zero);
		return screen >= 0 && screen < names.Count && names[screen] != "" ? names[screen] : null;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	struct DevMode
	{
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmDeviceName;
		public ushort dmSpecVersion, dmDriverVersion, dmSize, dmDriverExtra;
		public uint dmFields;
		public int dmPositionX, dmPositionY;
		public uint dmDisplayOrientation, dmDisplayFixedOutput;
		public short dmColor, dmDuplex, dmYResolution, dmTTOption, dmCollate;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string dmFormName;
		public ushort dmLogPixels;
		public uint dmBitsPerPel, dmPelsWidth, dmPelsHeight, dmDisplayFlags, dmDisplayFrequency;
		public uint dmICMMethod, dmICMIntent, dmMediaType, dmDitherType, dmReserved1, dmReserved2, dmPanningWidth, dmPanningHeight;
	}

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
	struct MonitorInfoEx
	{
		public int cbSize;
		public int monitorLeft, monitorTop, monitorRight, monitorBottom;
		public int workLeft, workTop, workRight, workBottom;
		public uint dwFlags;
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)] public string szDevice;
	}

	delegate bool MonitorEnumProc(IntPtr monitor, IntPtr hdc, IntPtr rect, IntPtr data);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	static extern bool EnumDisplaySettingsW(string device, int modeNum, ref DevMode mode);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	static extern int ChangeDisplaySettingsExW(string device, ref DevMode mode, IntPtr hwnd, uint flags, IntPtr param);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	static extern int ChangeDisplaySettingsExW(string device, IntPtr mode, IntPtr hwnd, uint flags, IntPtr param);

	[DllImport("user32.dll")]
	static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr clip, MonitorEnumProc callback, IntPtr data);

	[DllImport("user32.dll", CharSet = CharSet.Unicode)]
	static extern bool GetMonitorInfoW(IntPtr monitor, ref MonitorInfoEx info);
}
