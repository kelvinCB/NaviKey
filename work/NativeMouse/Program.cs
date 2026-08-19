using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Media;
using System.Windows.Forms;

static class Program
{
    [STAThread]
    static void Main()
    {
        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }
}

sealed class MainForm : Form
{
    const int WH_KEYBOARD_LL = 13, WM_KEYDOWN = 0x100, WM_KEYUP = 0x101, WM_SYSKEYDOWN = 0x104, WM_SYSKEYUP = 0x105;
    const int VK_CONTROL = 0x11, VK_LCONTROL = 0xA2, VK_RCONTROL = 0xA3, VK_MENU = 0x12, VK_LMENU = 0xA4, VK_RMENU = 0xA5, VK_X = 0x58, VK_BACK = 8, VK_DELETE = 0x2E, VK_ESCAPE = 0x1B, VK_Z = 0x5A, VK_B = 0x42, VK_N = 0x4E, VK_OEM_PERIOD = 0xBE, VK_BROWSER_BACK = 0xA6, VK_BROWSER_FORWARD = 0xA7;
    const int VK_LEFT = 0x25, VK_UP = 0x26, VK_RIGHT = 0x27, VK_DOWN = 0x28;
    const int VK_NUMPAD1 = 0x61, VK_NUMPAD2 = 0x62, VK_NUMPAD3 = 0x63, VK_NUMPAD4 = 0x64, VK_NUMPAD6 = 0x66, VK_NUMPAD8 = 0x68, VK_PRIOR = 0x21, VK_NEXT = 0x22;
    const uint LEFTDOWN = 2, LEFTUP = 4, RIGHTDOWN = 8, RIGHTUP = 16, MOUSEEVENTF_WHEEL = 0x0800, MOUSEEVENTF_HWHEEL = 0x01000;
    bool active, ctrl, alt, leftHeld, xHeld, moveLeft, moveRight, moveUp, moveDown; int speed = 50, lastMoveKey = 0, moveRepeat = 0, lastMoveTick; Label state; Button toggle; HookProc proc; IntPtr hook; SoundPlayer onSound, offSound; MemoryStream onStream, offStream;

    public MainForm()
    {
        Text = "Teclado como ratón"; ClientSize = new Size(500, 300); FormBorderStyle = FormBorderStyle.FixedDialog; MaximizeBox = false;
        var title = new Label { Text = "Controla el puntero con el teclado", Left = 18, Top = 16, AutoSize = true, Font = new Font(Font, FontStyle.Bold) };
        var help = new Label { Left = 18, Top = 52, Width = 465, Height = 105, Text = "Ctrl+Alt+X activa/desactiva el modo ratón.\r\n\r\nFlechas o teclado numérico 8/4/6/2 mueven; mantén X y usa ↑/↓ para scroll vertical y ←/→ para scroll horizontal; Z o NumPad 1 hacen clic izquierdo (mantén la tecla para arrastrar); . o NumPad 3 hacen clic derecho; B vuelve atrás y N avanza en el navegador; Retroceso vuelve al teclado normal.\r\n\r\nEl botón también cambia el modo." };
        var speedLabel = new Label { Left = 18, Top = 170, Width = 155, Text = "Píxeles por pulsación:" };
        var speedBox = new NumericUpDown { Left = 175, Top = 166, Width = 75, Minimum = 1, Maximum = 200, Value = speed, Increment = 1 };
        speedBox.ValueChanged += delegate { speed = (int)speedBox.Value; };
        var speedHelp = new Label { Left = 260, Top = 170, AutoSize = true, Text = "Ctrl+Alt+PageUp/PageDown también lo ajusta" };
        toggle = new Button { Left = 18, Top = 220, Width = 190, Height = 32, Text = "Activar modo ratón" }; toggle.Click += delegate { SetActive(!active); };
        state = new Label { Left = 230, Top = 228, AutoSize = true, Font = new Font(Font, FontStyle.Bold) };
        Controls.AddRange(new Control[] { title, help, speedLabel, speedBox, speedHelp, toggle, state }); UpdateUi();
        onStream = new MemoryStream(CreateTone(880, 1320, 140)); offStream = new MemoryStream(CreateTone(440, 220, 180));
        onSound = new SoundPlayer(onStream); offSound = new SoundPlayer(offStream); onSound.Load(); offSound.Load();
        Shown += delegate { toggle.Focus(); };
        proc = Callback; hook = SetWindowsHookEx(WH_KEYBOARD_LL, proc, IntPtr.Zero, 0); if (hook == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
        SystemEvents.PowerModeChanged += PowerModeChanged; SystemEvents.SessionSwitch += SessionSwitch;
        FormClosing += delegate { SystemEvents.PowerModeChanged -= PowerModeChanged; SystemEvents.SessionSwitch -= SessionSwitch; UnhookWindowsHookEx(hook); onSound.Dispose(); offSound.Dispose(); onStream.Dispose(); offStream.Dispose(); };
    }
    void PowerModeChanged(object sender, PowerModeChangedEventArgs e) { if (e.Mode == PowerModes.Resume) BeginInvoke((Action)ReinstallHookAfterResume); }
    void SessionSwitch(object sender, SessionSwitchEventArgs e) { if (e.Reason == SessionSwitchReason.SessionUnlock) BeginInvoke((Action)ReinstallHookAfterResume); }
    void ReinstallHookAfterResume()
    {
        if (hook != IntPtr.Zero) { UnhookWindowsHookEx(hook); hook = IntPtr.Zero; }
        ctrl = false; alt = false; if (leftHeld) mouse_event(LEFTUP, 0, 0, 0, UIntPtr.Zero); leftHeld = false; xHeld = false; ResetMovementState();
        active = false; EnsureCursorVisible(); UpdateUi();
        hook = SetWindowsHookEx(WH_KEYBOARD_LL, proc, IntPtr.Zero, 0);
    }
    void SetActive(bool value) { if (active == value) { if (value) EnsureCursorVisible(); UpdateUi(); return; } active = value; if (value) EnsureCursorVisible(); else ResetMovementState(); PlayModeSound(value); UpdateUi(); }
    void PlayModeSound(bool enabled) { (enabled ? onSound : offSound).Play(); }
    static byte[] CreateTone(int firstFrequency, int secondFrequency, int durationMs)
    {
        const int sampleRate = 44100, bits = 16, channels = 1;
        int samples = sampleRate * durationMs / 1000, dataSize = samples * 2;
        using (MemoryStream stream = new MemoryStream(44 + dataSize))
        using (BinaryWriter writer = new BinaryWriter(stream))
        {
            writer.Write(new byte[] { 0x52, 0x49, 0x46, 0x46 }); writer.Write(36 + dataSize); writer.Write(new byte[] { 0x57, 0x41, 0x56, 0x45 });
            writer.Write(new byte[] { 0x66, 0x6D, 0x74, 0x20 }); writer.Write(16); writer.Write((short)1); writer.Write((short)channels); writer.Write(sampleRate); writer.Write(sampleRate * channels * bits / 8); writer.Write((short)(channels * bits / 8)); writer.Write((short)bits);
            writer.Write(new byte[] { 0x64, 0x61, 0x74, 0x61 }); writer.Write(dataSize);
            for (int i = 0; i < samples; i++) { double phase = (double)i / samples; int frequency = phase < 0.5 ? firstFrequency : secondFrequency; short sample = (short)(Math.Sin(2 * Math.PI * frequency * i / sampleRate) * 9000); writer.Write(sample); }
            return stream.ToArray();
        }
    }
    void UpdateUi() { state.Text = active ? "● MODO RATÓN ACTIVO" : "○ Teclado normal"; state.ForeColor = active ? Color.ForestGreen : SystemColors.ControlText; toggle.Text = active ? "Desactivar modo ratón" : "Activar modo ratón"; }
    IntPtr Callback(int code, IntPtr msg, IntPtr data)
    {
        if (code >= 0)
        {
            int key = Marshal.ReadInt32(data); bool down = msg == (IntPtr)WM_KEYDOWN || msg == (IntPtr)WM_SYSKEYDOWN;
            bool up = msg == (IntPtr)WM_KEYUP || msg == (IntPtr)WM_SYSKEYUP;
            if (key == VK_CONTROL || key == VK_LCONTROL || key == VK_RCONTROL) ctrl = down ? true : (up ? false : ctrl);
            if (key == VK_MENU || key == VK_LMENU || key == VK_RMENU) alt = down ? true : (up ? false : alt);
            if (key == VK_X && ctrl && alt && down) { BeginInvoke((Action)delegate { SetActive(!active); }); return (IntPtr)1; }
            if (ctrl && alt && down && (key == VK_PRIOR || key == VK_NEXT)) { speed = Math.Max(1, Math.Min(200, speed + (key == VK_PRIOR ? 5 : -5))); return (IntPtr)1; }
            if (!active) return CallNextHookEx(hook, code, msg, data);
            if ((key == VK_BACK || key == VK_DELETE || key == VK_ESCAPE) && down) { if (leftHeld) { mouse_event(LEFTUP, 0, 0, 0, UIntPtr.Zero); leftHeld = false; } if (active) { active = false; ResetMovementState(); PlayModeSound(false); } BeginInvoke((Action)UpdateUi); return (IntPtr)1; }
            if (key == VK_X) { xHeld = down ? true : (up ? false : xHeld); return (IntPtr)1; }
            if (down && key == VK_B) { keybd_event((byte)VK_BROWSER_BACK, 0, 0, UIntPtr.Zero); keybd_event((byte)VK_BROWSER_BACK, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); return (IntPtr)1; }
            if (down && key == VK_N) { keybd_event((byte)VK_BROWSER_FORWARD, 0, 0, UIntPtr.Zero); keybd_event((byte)VK_BROWSER_FORWARD, 0, KEYEVENTF_KEYUP, UIntPtr.Zero); return (IntPtr)1; }
            if (down && (key == VK_Z || key == VK_NUMPAD1)) { if (!leftHeld) { mouse_event(LEFTDOWN, 0, 0, 0, UIntPtr.Zero); leftHeld = true; } return (IntPtr)1; }
            if (!down && (key == VK_Z || key == VK_NUMPAD1)) { if (leftHeld) { mouse_event(LEFTUP, 0, 0, 0, UIntPtr.Zero); leftHeld = false; } return (IntPtr)1; }
            if (!down && (key == VK_OEM_PERIOD || key == VK_NUMPAD3)) { mouse_event(RIGHTDOWN, 0, 0, 0, UIntPtr.Zero); mouse_event(RIGHTUP, 0, 0, 0, UIntPtr.Zero); return (IntPtr)1; }
            if (up && IsMovementKey(key)) { SetMovementKey(key, false); if (key == lastMoveKey) { lastMoveKey = 0; moveRepeat = 0; } return CallNextHookEx(hook, code, msg, data); }
            if (down)
            {
                if (xHeld && (key == VK_UP || key == VK_NUMPAD8 || key == VK_DOWN || key == VK_NUMPAD2)) { mouse_event(MOUSEEVENTF_WHEEL, 0, 0, (uint)((key == VK_UP || key == VK_NUMPAD8) ? 120 : -120), UIntPtr.Zero); return (IntPtr)1; }
                if (xHeld && (key == VK_LEFT || key == VK_NUMPAD4 || key == VK_RIGHT || key == VK_NUMPAD6)) { mouse_event(MOUSEEVENTF_HWHEEL, 0, 0, (uint)((key == VK_RIGHT || key == VK_NUMPAD6) ? 120 : -120), UIntPtr.Zero); return (IntPtr)1; }
                if (IsMovementKey(key)) SetMovementKey(key, true);
                int dx = 0, dy = 0;
                if (IsMovementKey(key))
                {
                    int now = Environment.TickCount;
                    moveRepeat = (key == lastMoveKey && unchecked(now - lastMoveTick) < 350) ? Math.Min(moveRepeat + 1, 5) : 1;
                    lastMoveKey = key; lastMoveTick = now;
                    int baseStep = Math.Max(1, speed / 5), maxStep = Math.Min(200, Math.Max(speed, speed * 4));
                    int step = Math.Min(maxStep, baseStep << Math.Min(moveRepeat - 1, 8));
                    if (moveLeft) dx = -step; else if (moveRight) dx = step;
                    if (moveUp) dy = -step; else if (moveDown) dy = step;
                }
                if (dx != 0 || dy != 0) { Point p; GetCursorPos(out p); SetCursorPos(p.X + dx, p.Y + dy); return (IntPtr)1; }
                if (key == VK_Z || key == VK_NUMPAD1 || key == VK_OEM_PERIOD || key == VK_NUMPAD3) return (IntPtr)1;
                return CallNextHookEx(hook, code, msg, data);
            }
            return CallNextHookEx(hook, code, msg, data);
        }
        return CallNextHookEx(hook, code, msg, data);
    }
    static bool IsMovementKey(int key) { return key == VK_LEFT || key == VK_NUMPAD4 || key == VK_RIGHT || key == VK_NUMPAD6 || key == VK_UP || key == VK_NUMPAD8 || key == VK_DOWN || key == VK_NUMPAD2; }
    void SetMovementKey(int key, bool pressed)
    {
        if (key == VK_LEFT || key == VK_NUMPAD4) moveLeft = pressed;
        else if (key == VK_RIGHT || key == VK_NUMPAD6) moveRight = pressed;
        else if (key == VK_UP || key == VK_NUMPAD8) moveUp = pressed;
        else if (key == VK_DOWN || key == VK_NUMPAD2) moveDown = pressed;
    }
    void ResetMovementState() { moveLeft = false; moveRight = false; moveUp = false; moveDown = false; lastMoveKey = 0; moveRepeat = 0; }
    void EnsureCursorVisible()
    {
        CURSORINFO info = new CURSORINFO(); info.cbSize = Marshal.SizeOf(typeof(CURSORINFO));
        if (GetCursorInfo(ref info) && (info.flags & CURSOR_SHOWING) == 0)
        {
            for (int i = 0; i < 16 && ShowCursor(true) < 0; i++) { }
        }
    }
    delegate IntPtr HookProc(int code, IntPtr msg, IntPtr data);
    [DllImport("user32.dll", SetLastError = true)] static extern IntPtr SetWindowsHookEx(int id, HookProc proc, IntPtr mod, uint thread);
    [DllImport("user32.dll")] static extern bool UnhookWindowsHookEx(IntPtr hook);
    [DllImport("user32.dll")] static extern IntPtr CallNextHookEx(IntPtr hook, int code, IntPtr msg, IntPtr data);
    [DllImport("user32.dll")] static extern bool GetCursorPos(out Point p);
    [DllImport("user32.dll")] static extern bool SetCursorPos(int x, int y);
    const int CURSOR_SHOWING = 0x00000001;
    [StructLayout(LayoutKind.Sequential)] struct CURSORINFO { public int cbSize; public int flags; public IntPtr hCursor; public Point ptScreenPos; }
    [DllImport("user32.dll")] static extern bool GetCursorInfo(ref CURSORINFO info);
    [DllImport("user32.dll")] static extern int ShowCursor(bool show);
    const uint KEYEVENTF_KEYUP = 0x0002;
    [DllImport("user32.dll")] static extern void mouse_event(uint flags, uint dx, uint dy, uint data, UIntPtr extra);
    [DllImport("user32.dll")] static extern void keybd_event(byte key, byte scan, uint flags, UIntPtr extra);
}
