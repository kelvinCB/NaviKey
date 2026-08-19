using System;
using System.ComponentModel;
using System.Drawing;
using Microsoft.Win32;
using System.Runtime.InteropServices;
using System.Windows.Forms;

sealed class ThemePalette
{
    public readonly string Name;
    public readonly string Description;
    public readonly Color Background;
    public readonly Color Rail;
    public readonly Color Surface;
    public readonly Color SurfaceRaised;
    public readonly Color Accent;
    public readonly Color TextMuted;
    public readonly Color TextPrimary;
    public readonly Color Active;
    public readonly Color Alert;
    public readonly Color Footer;
    public readonly Color NavSelected;
    public readonly Color ActiveButton;

    ThemePalette(string name, string description, Color background, Color rail, Color surface, Color surfaceRaised, Color accent, Color textMuted, Color textPrimary, Color active, Color alert, Color footer, Color navSelected, Color activeButton)
    {
        Name = name; Description = description; Background = background; Rail = rail; Surface = surface; SurfaceRaised = surfaceRaised; Accent = accent; TextMuted = textMuted; TextPrimary = textPrimary; Active = active; Alert = alert; Footer = footer; NavSelected = navSelected; ActiveButton = activeButton;
    }

    public static readonly ThemePalette CommandCenter = new ThemePalette("Command Center", "Oscuro y técnico", Color.FromArgb(5, 18, 38), Color.FromArgb(4, 14, 30), Color.FromArgb(10, 37, 64), Color.FromArgb(14, 50, 82), Color.FromArgb(73, 214, 232), Color.FromArgb(169, 194, 211), Color.White, Color.FromArgb(83, 210, 125), Color.FromArgb(244, 123, 107), Color.FromArgb(7, 28, 49), Color.FromArgb(14, 53, 84), Color.FromArgb(31, 81, 68));
    public static readonly ThemePalette Aurora = new ThemePalette("Aurora", "Verde azulado y relajado", Color.FromArgb(6, 25, 32), Color.FromArgb(5, 34, 42), Color.FromArgb(12, 51, 59), Color.FromArgb(18, 76, 83), Color.FromArgb(88, 224, 193), Color.FromArgb(163, 211, 208), Color.FromArgb(241, 255, 251), Color.FromArgb(112, 224, 140), Color.FromArgb(255, 138, 122), Color.FromArgb(10, 44, 55), Color.FromArgb(22, 78, 90), Color.FromArgb(35, 108, 87));
    public static readonly ThemePalette HighContrast = new ThemePalette("Alto contraste", "Máxima legibilidad", Color.Black, Color.FromArgb(16, 16, 16), Color.FromArgb(26, 26, 26), Color.FromArgb(42, 42, 42), Color.FromArgb(255, 212, 0), Color.FromArgb(230, 230, 230), Color.White, Color.FromArgb(0, 255, 102), Color.FromArgb(255, 90, 95), Color.FromArgb(17, 17, 17), Color.FromArgb(51, 51, 0), Color.FromArgb(61, 58, 0));
    public static readonly ThemePalette Claro = new ThemePalette("Claro", "Limpio y luminoso", Color.FromArgb(244, 247, 250), Color.FromArgb(226, 232, 240), Color.White, Color.FromArgb(238, 243, 248), Color.FromArgb(0, 112, 192), Color.FromArgb(70, 85, 105), Color.FromArgb(15, 23, 42), Color.FromArgb(22, 163, 74), Color.FromArgb(220, 38, 38), Color.FromArgb(226, 232, 240), Color.FromArgb(207, 226, 243), Color.FromArgb(22, 101, 52));
}

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
    ThemePalette theme = ThemePalette.CommandCenter;
    Color Background { get { return theme.Background; } }
    Color Rail { get { return theme.Rail; } }
    Color Surface { get { return theme.Surface; } }
    Color SurfaceRaised { get { return theme.SurfaceRaised; } }
    Color Cyan { get { return theme.Accent; } }
    Color TextMuted { get { return theme.TextMuted; } }
    Color Green { get { return theme.Active; } }
    Color Red { get { return theme.Alert; } }
    Color Footer { get { return theme.Footer; } }
    bool active, ctrl, alt, leftHeld, xHeld, moveLeft, moveRight, moveUp, moveDown, soundsEnabled = true, ensureCursorOnResume = true; int speed = 50, lastMoveKey = 0, moveRepeat = 0, lastMoveTick, selectedSection; Label state, modeHint, speedReadout, pageHeading, pageDescription; Panel statusDot, sectionOverlay; NumericUpDown speedBox; Button toggle; Button[] navButtons, themeButtons; HookProc proc; IntPtr hook; ModeAudio modeAudio;

    public MainForm()
    {
        Text = "Teclado como ratón";
        ClientSize = new Size(980, 700);
        MinimumSize = new Size(980, 700);
        StartPosition = FormStartPosition.CenterScreen;
        FormBorderStyle = FormBorderStyle.FixedSingle;
        MaximizeBox = false;
        BackColor = Background;
        ForeColor = Color.White;
        Font = new Font("Segoe UI", 9F);
        try
        {
            var applicationIcon = System.Drawing.Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            if (applicationIcon != null) Icon = applicationIcon;
        }
        catch (ArgumentException) { }

        var rail = new Panel { Dock = DockStyle.Left, Width = 214, BackColor = Rail, Padding = new Padding(22, 24, 18, 20) };
        var brand = new Label { Text = "TECLADO\r\nCOMO RATÓN", Left = 22, Top = 24, Width = 170, Height = 50, ForeColor = Color.White, Font = new Font("Segoe UI", 12F, FontStyle.Bold), BackColor = Color.Transparent };
        var brandLine = new Panel { Left = 22, Top = 92, Width = 36, Height = 3, BackColor = Cyan };
        var navTitle = new Label { Text = "ESPACIO DE CONTROL", Left = 22, Top = 124, Width = 170, ForeColor = TextMuted, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
        rail.Controls.AddRange(new Control[] { brand, brandLine, navTitle });
        string[] navItems = { "Centro", "Velocidad", "Controles", "Apariencia", "Accesibilidad", "Acerca de" };
        navButtons = new Button[navItems.Length];
        for (int i = 0; i < navItems.Length; i++)
        {
            var nav = CreateNavButton(navItems[i], 158 + i * 42, i == 0);
            int sectionIndex = i;
            nav.Click += delegate { ShowSection(sectionIndex); };
            navButtons[i] = nav;
            rail.Controls.Add(nav);
        }
        var railFooter = new Label { Text = "NATIVE WINDOWS\r\nCONTROL v1.0", Left = 22, Top = 610, Width = 170, Height = 34, ForeColor = TextMuted, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
        rail.Controls.Add(railFooter);

        var content = new Panel { Dock = DockStyle.Fill, BackColor = Background, Padding = new Padding(32, 26, 32, 24), AutoScroll = false };
        pageHeading = new Label { Text = "Control del puntero", Left = 32, Top = 26, Width = 620, Height = 38, ForeColor = Color.White, Font = new Font("Segoe UI", 24F, FontStyle.Bold) };
        pageDescription = new Label { Text = "Un centro de control claro para moverte, hacer clic y desplazarte sin tocar el mouse.", Left = 34, Top = 68, Width = 700, Height = 24, ForeColor = TextMuted, Font = new Font("Segoe UI", 10F) };
        content.Controls.AddRange(new Control[] { pageHeading, pageDescription });

        var hero = CreateSurface(Surface, 32, 108, 702, 128);
        statusDot = new Panel { Left = 22, Top = 23, Width = 10, Height = 10, BackColor = Red };
        var statusLabel = new Label { Text = "ESTADO DEL SISTEMA", Left = 42, Top = 19, Width = 190, Height = 18, ForeColor = TextMuted, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
        state = new Label { Left = 22, Top = 43, Width = 390, Height = 34, ForeColor = Color.White, Font = new Font("Segoe UI", 18F, FontStyle.Bold) };
        modeHint = new Label { Left = 24, Top = 84, Width = 370, Height = 20, ForeColor = TextMuted, Font = new Font("Segoe UI", 9F) };
        toggle = CreatePrimaryButton("Activar modo ratón", 470, 35, 202, 56);
        toggle.Click += delegate { SetActive(!active); };
        hero.Controls.AddRange(new Control[] { statusDot, statusLabel, state, modeHint, toggle });
        content.Controls.Add(hero);

        var speedPanel = CreateSurface(Surface, 32, 254, 338, 172);
        var speedTitle = new Label { Text = "Velocidad del puntero", Left = 20, Top = 18, Width = 280, Height = 28, ForeColor = Color.White, Font = new Font("Segoe UI", 13F, FontStyle.Bold) };
        var speedHelp = new Label { Text = "Ajusta el paso base por pulsación.", Left = 20, Top = 49, Width = 280, Height = 20, ForeColor = TextMuted };
        speedBox = new NumericUpDown { Left = 20, Top = 82, Width = 100, Height = 32, Minimum = 1, Maximum = 200, Value = speed, Increment = 1, BackColor = SurfaceRaised, ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 12F) };
        speedBox.ValueChanged += delegate { speed = (int)speedBox.Value; UpdateSpeedReadout(); };
        speedReadout = new Label { Text = "50 px / pulsación", Left = 136, Top = 88, Width = 175, Height = 24, ForeColor = Cyan, Font = new Font("Segoe UI", 10F, FontStyle.Bold) };
        var speedKeys = new Label { Text = "Ctrl+Alt+PageUp / PageDown", Left = 20, Top = 132, Width = 295, Height = 20, ForeColor = TextMuted, Font = new Font("Segoe UI", 8.5F) };
        speedPanel.Controls.AddRange(new Control[] { speedTitle, speedHelp, speedBox, speedReadout, speedKeys });
        content.Controls.Add(speedPanel);

        var curvePanel = CreateSurface(Surface, 390, 254, 344, 172);
        var curveTitle = new Label { Text = "Aceleración progresiva", Left = 20, Top = 18, Width = 290, Height = 28, ForeColor = Color.White, Font = new Font("Segoe UI", 13F, FontStyle.Bold) };
        var curveHelp = new Label { Text = "Mantén una flecha para ganar velocidad.", Left = 20, Top = 49, Width = 295, Height = 20, ForeColor = TextMuted };
        var curveBase = new Panel { Left = 20, Top = 134, Width = 300, Height = 1, BackColor = Color.FromArgb(72, 111, 137) };
        var curve1 = new Panel { Left = 28, Top = 122, Width = 38, Height = 12, BackColor = Color.FromArgb(68, 155, 185) };
        var curve2 = new Panel { Left = 74, Top = 114, Width = 48, Height = 20, BackColor = Color.FromArgb(68, 174, 192) };
        var curve3 = new Panel { Left = 130, Top = 100, Width = 58, Height = 34, BackColor = Color.FromArgb(73, 196, 201) };
        var curve4 = new Panel { Left = 196, Top = 80, Width = 68, Height = 54, BackColor = Color.FromArgb(83, 210, 177) };
        var curve5 = new Panel { Left = 272, Top = 59, Width = 32, Height = 75, BackColor = Green };
        curvePanel.Controls.AddRange(new Control[] { curveTitle, curveHelp, curveBase, curve1, curve2, curve3, curve4, curve5 });
        content.Controls.Add(curvePanel);

        var quickPanel = CreateSurface(Surface, 32, 444, 702, 148);
        var quickTitle = new Label { Text = "Accesos rápidos de teclado", Left = 20, Top = 17, Width = 360, Height = 26, ForeColor = Color.White, Font = new Font("Segoe UI", 13F, FontStyle.Bold) };
        quickPanel.Controls.Add(quickTitle);
        quickPanel.Controls.Add(CreateShortcutCard("FLECHAS", "Mover el puntero", 20, 57, 145));
        quickPanel.Controls.Add(CreateShortcutCard("X + FLECHA", "Scroll vertical u horizontal", 180, 57, 185));
        quickPanel.Controls.Add(CreateShortcutCard("Z / .", "Clic izquierdo / derecho", 380, 57, 150));
        quickPanel.Controls.Add(CreateShortcutCard("B / N", "Atrás / adelante", 545, 57, 135));
        content.Controls.Add(quickPanel);

        var footer = CreateSurface(Footer, 32, 610, 702, 42);
        var footerText = new Label { Text = "Ctrl+Alt+X activa o desactiva el modo. Retroceso, Supr o Escape vuelven al teclado normal.", Left = 18, Top = 12, Width = 660, Height = 20, ForeColor = TextMuted, Font = new Font("Segoe UI", 8.5F) };
        footer.Controls.Add(footerText);
        content.Controls.Add(footer);

        sectionOverlay = new Panel { Left = 32, Top = 108, Width = 702, Height = 610, BackColor = Background, Visible = false };
        content.Controls.Add(sectionOverlay);
        Controls.Add(content);
        Controls.Add(rail);
        UpdateUi();
        modeAudio = new ModeAudio();
        Shown += delegate { toggle.Focus(); };
        proc = Callback; hook = SetWindowsHookEx(WH_KEYBOARD_LL, proc, IntPtr.Zero, 0); if (hook == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
        SystemEvents.PowerModeChanged += PowerModeChanged; SystemEvents.SessionSwitch += SessionSwitch;
        FormClosing += delegate { SystemEvents.PowerModeChanged -= PowerModeChanged; SystemEvents.SessionSwitch -= SessionSwitch; UnhookWindowsHookEx(hook); modeAudio.Dispose(); };
    }
    Panel CreateSurface(Color color, int left, int top, int width, int height)
    {
        return new Panel { Left = left, Top = top, Width = width, Height = height, BackColor = color, BorderStyle = BorderStyle.FixedSingle };
    }
    Button CreateNavButton(string text, int top, bool selected)
    {
        return new Button { Text = text, Left = 16, Top = top, Width = 178, Height = 34, FlatStyle = FlatStyle.Flat, FlatAppearance = { BorderSize = 0 }, BackColor = selected ? theme.NavSelected : Rail, ForeColor = selected ? theme.TextPrimary : TextMuted, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(12, 0, 0, 0), TabStop = false };
    }
    Button CreatePrimaryButton(string text, int left, int top, int width, int height)
    {
        return new Button { Text = text, Left = left, Top = top, Width = width, Height = height, FlatStyle = FlatStyle.Flat, FlatAppearance = { BorderSize = 1, BorderColor = Cyan }, BackColor = Cyan, ForeColor = Background, Font = new Font("Segoe UI", 10F, FontStyle.Bold), TabStop = false };
    }
    Panel CreateShortcutCard(string key, string action, int left, int top, int width)
    {
        var panel = new Panel { Left = left, Top = top, Width = width, Height = 66, BackColor = SurfaceRaised, BorderStyle = BorderStyle.FixedSingle };
        var keyLabel = new Label { Text = key, Left = 10, Top = 8, Width = width - 20, Height = 18, ForeColor = Cyan, Font = new Font("Segoe UI", 8F, FontStyle.Bold) };
        var actionLabel = new Label { Text = action, Left = 10, Top = 32, Width = width - 20, Height = 25, ForeColor = Color.White, Font = new Font("Segoe UI", 8.5F) };
        panel.Controls.AddRange(new Control[] { keyLabel, actionLabel });
        return panel;
    }
    Label CreatePageLabel(string text, int left, int top, int width, int height, float size, FontStyle style, Color color)
    {
        return new Label { Text = text, Left = left, Top = top, Width = width, Height = height, ForeColor = color, Font = new Font("Segoe UI", size, style) };
    }
    void ShowSection(int index)
    {
        if (navButtons == null || sectionOverlay == null) return;
        selectedSection = index;
        for (int i = 0; i < navButtons.Length; i++)
        {
            navButtons[i].BackColor = i == index ? theme.NavSelected : Rail;
            navButtons[i].ForeColor = i == index ? theme.TextPrimary : TextMuted;
        }
        sectionOverlay.Controls.Clear();
        if (index == 0)
        {
            pageHeading.Text = "Control del puntero";
            pageDescription.Text = "Un centro de control claro para moverte, hacer clic y desplazarte sin tocar el mouse.";
            sectionOverlay.Visible = false;
            return;
        }
        sectionOverlay.Visible = true;
        sectionOverlay.BringToFront();
        if (index == 1)
        {
            pageHeading.Text = "Velocidad";
            pageDescription.Text = "Ajusta el ritmo del puntero y prueba el paso que mejor se adapta a tu pantalla.";
            BuildVelocitySection();
        }
        else if (index == 2)
        {
            pageHeading.Text = "Controles";
            pageDescription.Text = "Consulta cada tecla y activa el modo ratón desde este mismo espacio.";
            BuildControlsSection();
        }
        else if (index == 3)
        {
            pageHeading.Text = "Apariencia";
            pageDescription.Text = "Una interfaz de alto contraste, pensada para leerla de un vistazo.";
            BuildAppearanceSection();
        }
        else if (index == 4)
        {
            pageHeading.Text = "Accesibilidad";
            pageDescription.Text = "Ajustes para que el cambio de modo sea evidente y predecible.";
            BuildAccessibilitySection();
        }
        else
        {
            pageHeading.Text = "Acerca de";
            pageDescription.Text = "Información de esta versión nativa para Windows.";
            BuildAboutSection();
        }
    }
    void BuildVelocitySection()
    {
        var panel = CreateSurface(Surface, 0, 0, 702, 178);
        panel.Controls.Add(CreatePageLabel("Paso base", 22, 20, 250, 28, 15F, FontStyle.Bold, Color.White));
        panel.Controls.Add(CreatePageLabel("Cada pulsación empieza con este valor y la aceleración progresiva llega hasta el máximo.", 22, 52, 620, 24, 9F, FontStyle.Regular, TextMuted));
        var input = new NumericUpDown { Left = 22, Top = 92, Width = 112, Height = 34, Minimum = 1, Maximum = 200, Value = speed, Increment = 1, BackColor = SurfaceRaised, ForeColor = Color.White, BorderStyle = BorderStyle.FixedSingle, Font = new Font("Segoe UI", 13F) };
        var valueLabel = CreatePageLabel(speed.ToString() + " px / pulsación", 154, 99, 230, 24, 10F, FontStyle.Bold, Cyan);
        input.ValueChanged += delegate { speed = (int)input.Value; valueLabel.Text = speed.ToString() + " px / pulsación"; UpdateSpeedReadout(); };
        var plus = CreatePrimaryButton("Subir 5 px", 410, 88, 112, 38);
        var minus = new Button { Text = "Bajar 5 px", Left = 534, Top = 88, Width = 112, Height = 38, FlatStyle = FlatStyle.Flat, FlatAppearance = { BorderSize = 1, BorderColor = Color.FromArgb(85, 132, 157) }, BackColor = SurfaceRaised, ForeColor = Color.White, Font = new Font("Segoe UI", 9F, FontStyle.Bold) };
        plus.Click += delegate { speed = Math.Min(200, speed + 5); input.Value = speed; valueLabel.Text = speed.ToString() + " px / pulsación"; UpdateSpeedReadout(); };
        minus.Click += delegate { speed = Math.Max(1, speed - 5); input.Value = speed; valueLabel.Text = speed.ToString() + " px / pulsación"; UpdateSpeedReadout(); };
        panel.Controls.AddRange(new Control[] { input, valueLabel, plus, minus });
        sectionOverlay.Controls.Add(panel);
        var hint = CreateSurface(Footer, 0, 198, 702, 96);
        hint.Controls.Add(CreatePageLabel("Atajo rápido", 20, 18, 150, 20, 9F, FontStyle.Bold, Cyan));
        hint.Controls.Add(CreatePageLabel("Ctrl+Alt+PageUp sube cinco píxeles. Ctrl+Alt+PageDown baja cinco.", 20, 45, 620, 24, 10F, FontStyle.Regular, Color.White));
        sectionOverlay.Controls.Add(hint);
    }
    void BuildControlsSection()
    {
        var move = CreateSurface(Surface, 0, 0, 338, 180);
        move.Controls.Add(CreatePageLabel("Mover y desplazar", 20, 18, 280, 26, 14F, FontStyle.Bold, Color.White));
        move.Controls.Add(CreatePageLabel("Flechas / NumPad 8 4 6 2\r\nX + flechas: scroll vertical y horizontal\r\r\nLa diagonal funciona al mantener dos flechas.", 20, 56, 300, 100, 9.5F, FontStyle.Regular, TextMuted));
        var actions = CreateSurface(Surface, 364, 0, 338, 180);
        actions.Controls.Add(CreatePageLabel("Clic y navegación", 20, 18, 280, 26, 14F, FontStyle.Bold, Color.White));
        actions.Controls.Add(CreatePageLabel("Z / NumPad 1: clic izquierdo y arrastre\r\n. / NumPad 3: clic derecho\r\nB: atrás   N: adelante", 20, 56, 300, 100, 9.5F, FontStyle.Regular, TextMuted));
        sectionOverlay.Controls.AddRange(new Control[] { move, actions });
        var actionButton = CreatePrimaryButton(active ? "Desactivar modo ratón" : "Activar modo ratón", 0, 208, 240, 48);
        actionButton.Click += delegate { SetActive(!active); actionButton.Text = active ? "Desactivar modo ratón" : "Activar modo ratón"; };
        sectionOverlay.Controls.Add(actionButton);
        sectionOverlay.Controls.Add(CreatePageLabel("Retroceso, Supr o Escape desactivan sin cerrar el programa.", 260, 222, 410, 24, 9.5F, FontStyle.Regular, TextMuted));
    }
    void BuildAppearanceSection()
    {
        var panel = CreateSurface(Surface, 0, 0, 702, 322);
        panel.Controls.Add(CreatePageLabel("Elige tu apariencia", 22, 20, 420, 28, 15F, FontStyle.Bold, theme.TextPrimary));
        panel.Controls.Add(CreatePageLabel("Selecciona un diseño para cambiar colores, contraste y ambiente al instante.", 22, 54, 640, 24, 9.5F, FontStyle.Regular, TextMuted));
        ThemePalette[] options = { ThemePalette.CommandCenter, ThemePalette.Aurora, ThemePalette.HighContrast, ThemePalette.Claro };
        themeButtons = new Button[options.Length];
        for (int i = 0; i < options.Length; i++)
        {
            ThemePalette option = options[i];
            Button themeButton = new Button { Left = 22 + (i % 2) * 334, Top = 92 + (i / 2) * 102, Width = 314, Height = 84, Tag = option, FlatStyle = FlatStyle.Flat, BackColor = option.Surface, ForeColor = option.TextPrimary, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(16, 8, 10, 8), Font = new Font("Segoe UI", 10F, FontStyle.Bold), TabStop = false };
            themeButton.FlatAppearance.BorderSize = 2;
            themeButton.FlatAppearance.BorderColor = option.Accent;
            themeButton.Click += delegate { SetTheme((ThemePalette)((Button)themeButton).Tag); };
            themeButtons[i] = themeButton;
            panel.Controls.Add(themeButton);
        }
        sectionOverlay.Controls.Add(panel);
        var note = CreateSurface(Footer, 0, 342, 702, 82);
        note.Controls.Add(CreatePageLabel("Diseño activo", 20, 16, 150, 20, 9F, FontStyle.Bold, Cyan));
        note.Controls.Add(CreatePageLabel("Puedes cambiar de tema cuando quieras; el modo ratón y sus atajos no se modifican.", 20, 42, 640, 24, 9.5F, FontStyle.Regular, theme.TextPrimary));
        sectionOverlay.Controls.Add(note);
        RefreshThemeButtons();
    }
    void BuildAccessibilitySection()
    {
        var panel = CreateSurface(Surface, 0, 0, 702, 180);
        panel.Controls.Add(CreatePageLabel("Ajustes de accesibilidad", 22, 20, 400, 28, 15F, FontStyle.Bold, Color.White));
        panel.Controls.Add(CreatePageLabel("Haz que los cambios de estado sean fáciles de percibir y recuperar.", 22, 54, 620, 22, 9.5F, FontStyle.Regular, TextMuted));
        var soundBox = new CheckBox { Text = "Sonido al activar y desactivar", Left = 22, Top = 94, Width = 310, Height = 28, Checked = soundsEnabled, ForeColor = Color.White, BackColor = Surface, FlatStyle = FlatStyle.Flat };
        soundBox.CheckedChanged += delegate { soundsEnabled = soundBox.Checked; };
        var focusBox = new CheckBox { Text = "Mantener visible el puntero al reanudar", Left = 22, Top = 132, Width = 360, Height = 28, Checked = ensureCursorOnResume, ForeColor = Color.White, BackColor = Surface, FlatStyle = FlatStyle.Flat };
        focusBox.CheckedChanged += delegate { ensureCursorOnResume = focusBox.Checked; if (focusBox.Checked) EnsureCursorVisible(); };
        panel.Controls.AddRange(new Control[] { soundBox, focusBox });
        sectionOverlay.Controls.Add(panel);
        var safe = CreateSurface(Footer, 0, 202, 702, 92);
        safe.Controls.Add(CreatePageLabel("Salida segura", 20, 16, 150, 20, 9F, FontStyle.Bold, Cyan));
        safe.Controls.Add(CreatePageLabel("Retroceso, Supr y Escape siempre vuelven al teclado normal sin cerrar la aplicación.", 20, 43, 640, 24, 9.5F, FontStyle.Regular, Color.White));
        sectionOverlay.Controls.Add(safe);
    }
    void BuildAboutSection()
    {
        var panel = CreateSurface(Surface, 0, 0, 702, 190);
        panel.Controls.Add(CreatePageLabel("Teclado como ratón", 22, 22, 420, 30, 17F, FontStyle.Bold, Color.White));
        panel.Controls.Add(CreatePageLabel("Control nativo para Windows\r\nVersión de producción 1.0\r\nHook de teclado de bajo nivel + eventos de mouse nativos", 22, 66, 620, 82, 10F, FontStyle.Regular, TextMuted));
        sectionOverlay.Controls.Add(panel);
        var repo = CreateSurface(Footer, 0, 212, 702, 82);
        repo.Controls.Add(CreatePageLabel("Código fuente", 20, 16, 150, 20, 9F, FontStyle.Bold, Cyan));
        repo.Controls.Add(CreatePageLabel("github.com/kelvinCB/TecladoComoRaton", 20, 42, 640, 24, 10F, FontStyle.Regular, Color.White));
        sectionOverlay.Controls.Add(repo);
    }
    void SetTheme(ThemePalette palette)
    {
        if (palette == null) return;
        if (Object.ReferenceEquals(theme, palette)) { RefreshThemeButtons(); return; }
        ApplyTheme(palette);
    }
    void ApplyTheme(ThemePalette palette)
    {
        ThemePalette previous = theme;
        theme = palette;
        SuspendLayout();
        ApplyThemeToControl(this, previous, palette);
        if (navButtons != null)
        {
            for (int i = 0; i < navButtons.Length; i++)
            {
                navButtons[i].BackColor = i == selectedSection ? palette.NavSelected : palette.Rail;
                navButtons[i].ForeColor = i == selectedSection ? palette.TextPrimary : palette.TextMuted;
            }
        }
        RefreshThemeButtons();
        UpdateUi();
        ResumeLayout(true);
    }
    void ApplyThemeToControl(Control control, ThemePalette previous, ThemePalette next)
    {
        control.BackColor = MapThemeColor(control.BackColor, previous, next);
        control.ForeColor = MapThemeColor(control.ForeColor, previous, next);
        Button button = control as Button;
        if (button != null) button.FlatAppearance.BorderColor = MapThemeColor(button.FlatAppearance.BorderColor, previous, next);
        foreach (Control child in control.Controls) ApplyThemeToControl(child, previous, next);
    }
    Color MapThemeColor(Color value, ThemePalette previous, ThemePalette next)
    {
        if (value == previous.Background) return next.Background;
        if (value == previous.Rail) return next.Rail;
        if (value == previous.Surface) return next.Surface;
        if (value == previous.SurfaceRaised) return next.SurfaceRaised;
        if (value == previous.Accent) return next.Accent;
        if (value == previous.TextMuted) return next.TextMuted;
        if (value == previous.TextPrimary || value == Color.White) return next.TextPrimary;
        if (value == previous.Active) return next.Active;
        if (value == previous.Alert) return next.Alert;
        if (value == previous.Footer) return next.Footer;
        if (value == previous.NavSelected) return next.NavSelected;
        if (value == previous.ActiveButton) return next.ActiveButton;
        return value;
    }
    void RefreshThemeButtons()
    {
        if (themeButtons == null) return;
        for (int i = 0; i < themeButtons.Length; i++)
        {
            Button themeButton = themeButtons[i];
            ThemePalette option = (ThemePalette)themeButton.Tag;
            themeButton.Text = (Object.ReferenceEquals(theme, option) ? "✓ " : "   ") + option.Name + "\r\n" + option.Description;
            themeButton.BackColor = option.Surface;
            themeButton.ForeColor = option.TextPrimary;
            themeButton.FlatAppearance.BorderColor = option.Accent;
            themeButton.FlatAppearance.BorderSize = Object.ReferenceEquals(theme, option) ? 3 : 1;
        }
    }
    void UpdateSpeedReadout() { if (speedReadout != null) speedReadout.Text = speed.ToString() + " px / pulsación"; }
    void PowerModeChanged(object sender, PowerModeChangedEventArgs e) { if (e.Mode == PowerModes.Resume) BeginInvoke((Action)ReinstallHookAfterResume); }
    void SessionSwitch(object sender, SessionSwitchEventArgs e) { if (e.Reason == SessionSwitchReason.SessionUnlock) BeginInvoke((Action)ReinstallHookAfterResume); }
    void ReinstallHookAfterResume()
    {
        if (hook != IntPtr.Zero) { UnhookWindowsHookEx(hook); hook = IntPtr.Zero; }
        ctrl = false; alt = false; if (leftHeld) mouse_event(LEFTUP, 0, 0, 0, UIntPtr.Zero); leftHeld = false; xHeld = false; ResetMovementState();
        active = false; if (ensureCursorOnResume) EnsureCursorVisible(); UpdateUi();
        hook = SetWindowsHookEx(WH_KEYBOARD_LL, proc, IntPtr.Zero, 0);
    }
    void SetActive(bool value) { if (active == value) { if (value) EnsureCursorVisible(); UpdateUi(); return; } active = value; if (value) EnsureCursorVisible(); else ResetMovementState(); PlayModeSound(value); UpdateUi(); }
    void PlayModeSound(bool enabled)
    {
        if (!soundsEnabled || modeAudio == null) return;
        Action play = delegate { modeAudio.Enabled = soundsEnabled; modeAudio.Play(enabled); };
        if (IsHandleCreated && InvokeRequired)
        {
            try { BeginInvoke(play); } catch (InvalidOperationException) { }
        }
        else play();
    }
    void UpdateUi()
    {
        if (state == null) return;
        state.Text = active ? "MODO RATÓN ACTIVO" : "TECLADO NORMAL";
        state.ForeColor = active ? theme.Active : theme.TextPrimary;
        modeHint.Text = active ? "Flechas listas para mover el puntero" : "Ctrl+Alt+X para activar el modo ratón";
        statusDot.BackColor = active ? theme.Active : theme.Alert;
        toggle.Text = active ? "Desactivar modo ratón" : "Activar modo ratón";
        toggle.BackColor = active ? theme.ActiveButton : theme.Accent;
        toggle.ForeColor = active ? theme.TextPrimary : theme.Background;
        UpdateSpeedReadout();
    }
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
