using System.Drawing.Drawing2D;

namespace CatLocker;

internal sealed class SettingsForm : Form
{
    private readonly TrackBar opacityTrackBar = new();
    private readonly TrackBar positionTrackBar = new();
    private readonly CheckBox startupCheckBox = new();
    private readonly CheckBox soundsCheckBox = new();
    private readonly CheckBox touchCheckBox = new();
    private readonly CheckBox darkCheckBox = new();
    private readonly CheckBox ctrlCheckBox = new();
    private readonly CheckBox shiftCheckBox = new();
    private readonly CheckBox altCheckBox = new();
    private readonly ComboBox lockModeComboBox = new();
    private readonly ComboBox hotkeyComboBox = new();
    private readonly List<uint> hotkeyValues = new();
    private readonly NumericUpDown autoLockNumeric = new();
    private readonly NumericUpDown autoUnlockNumeric = new();
    private readonly Panel previewPanel = new();
    private readonly Label previewLabel = new();
    private readonly Label opacityValueLabel = new();
    private readonly Label positionValueLabel = new();
    private readonly Label modeHintLabel = new();
    private readonly Label hotkeyHintLabel = new();
    private readonly Panel badgePanel;
    private readonly Label badgeLabel;
    private readonly AppSettings originalSettings;

    public SettingsForm(AppSettings settings)
    {
        originalSettings = settings.Clone().Normalize();
        Settings = settings.Clone().Normalize();
        UnlockHotkey chord = Settings.GetChord();

        AutoScaleMode = AutoScaleMode.Dpi;
        BackColor = ModernTheme.WindowBackground;
        ClientSize = new Size(520, 690);
        DoubleBuffered = true;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        Text = "CatLocker Settings";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

        Panel heroPanel = CreateHeroPanel(new Point(20, 16), new Size(480, 100));
        badgeLabel = new Label
        {
            BackColor = Color.Transparent,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 8.25F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.White,
            TextAlign = ContentAlignment.MiddleCenter
        };
        badgePanel = new Panel
        {
            BackColor = Color.Transparent,
            Size = new Size(130, 28)
        };
        badgePanel.Controls.Add(badgeLabel);
        badgePanel.Resize += (_, _) => ApplyRoundedRegion(badgePanel, ModernTheme.RadiusMedium);
        badgePanel.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rectangle = new(0, 0, badgePanel.Width - 1, badgePanel.Height - 1);
            using GraphicsPath path = ModernTheme.CreateRoundedRectangle(rectangle, ModernTheme.RadiusMedium);
            using SolidBrush brush = new(Color.FromArgb(42, Color.White));
            using Pen pen = new(Color.FromArgb(72, Color.White), 1F);
            e.Graphics.FillPath(brush, path);
            e.Graphics.DrawPath(pen, path);
        };
        heroPanel.Controls.Add(badgePanel);

        Panel iconPanel = CreateIconPanel(new Point(20, 22));

        Label titleLabel = new()
        {
            AutoSize = true,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI Semibold", 17F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = Color.White,
            Location = new Point(78, 18),
            Text = "CatLocker"
        };

        Label descriptionLabel = new()
        {
            AutoSize = false,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(224, 238, 255),
            Location = new Point(80, 54),
            Size = new Size(360, 36),
            Text = "Soft lock for keyboard & mouse with a calm overlay."
        };

        heroPanel.Controls.Add(iconPanel);
        heroPanel.Controls.Add(titleLabel);
        heroPanel.Controls.Add(descriptionLabel);

        Panel settingsCard = CreateCard(new Point(20, 128), new Size(480, 390));
        Panel previewCard = CreateCard(new Point(20, 530), new Size(480, 60));

        Label settingsCaption = CreateSectionCaption("Lock settings", new Point(22, 16));

        Label modeLabel = CreateFieldLabel("Lock mode", new Point(22, 48));
        lockModeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        lockModeComboBox.FlatStyle = FlatStyle.Flat;
        lockModeComboBox.Font = Font;
        lockModeComboBox.Items.Add(LockMode.KeyboardOnly.ToDisplayText());
        lockModeComboBox.Items.Add(LockMode.KeyboardAndMouse.ToDisplayText());
        lockModeComboBox.Location = new Point(176, 44);
        lockModeComboBox.Size = new Size(260, 23);
        lockModeComboBox.SelectedIndex = Settings.LockMode == LockMode.KeyboardOnly ? 0 : 1;
        lockModeComboBox.SelectedIndexChanged += (_, _) => UpdatePreview();

        modeHintLabel.AutoSize = false;
        modeHintLabel.BackColor = Color.Transparent;
        modeHintLabel.ForeColor = ModernTheme.TextSecondary;
        modeHintLabel.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point);
        modeHintLabel.Location = new Point(177, 70);
        modeHintLabel.Size = new Size(260, 16);

        Label hotkeyLabel = CreateFieldLabel("Unlock hotkey", new Point(22, 98));
        ConfigureHotkeyCheck(ctrlCheckBox, "Ctrl", new Point(176, 96), chord.Ctrl);
        ConfigureHotkeyCheck(shiftCheckBox, "Shift", new Point(236, 96), chord.Shift);
        ConfigureHotkeyCheck(altCheckBox, "Alt", new Point(296, 96), chord.Alt);
        hotkeyComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        hotkeyComboBox.FlatStyle = FlatStyle.Flat;
        hotkeyComboBox.Font = Font;
        hotkeyComboBox.Location = new Point(352, 94);
        hotkeyComboBox.Size = new Size(84, 23);
        FillHotkeyKeys(chord.KeyCode);

        hotkeyHintLabel.AutoSize = false;
        hotkeyHintLabel.BackColor = Color.Transparent;
        hotkeyHintLabel.ForeColor = ModernTheme.TextSecondary;
        hotkeyHintLabel.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point);
        hotkeyHintLabel.Location = new Point(177, 120);
        hotkeyHintLabel.Size = new Size(260, 16);

        Label autoLockLabel = CreateFieldLabel("Auto-lock", new Point(22, 150));
        ConfigureTimerNumeric(autoLockNumeric, new Point(110, 148), Settings.AutoLockMinutes);
        Label autoLockUnit = CreateFieldLabel("min idle", new Point(168, 150));
        Label autoUnlockLabel = CreateFieldLabel("Auto-unlock", new Point(236, 150));
        ConfigureTimerNumeric(autoUnlockNumeric, new Point(322, 148), Settings.AutoUnlockMinutes);
        Label autoUnlockUnit = CreateFieldLabel("min", new Point(380, 150));

        Label timersHintLabel = new()
        {
            AutoSize = false,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = ModernTheme.TextSecondary,
            Location = new Point(177, 172),
            Size = new Size(260, 16),
            Text = "0 = off for both."
        };

        Label opacityLabel = CreateFieldLabel("Overlay opacity", new Point(22, 198));
        opacityTrackBar.BackColor = ModernTheme.CardBackground;
        opacityTrackBar.Location = new Point(172, 186);
        opacityTrackBar.Size = new Size(224, 42);
        opacityTrackBar.Minimum = 20;
        opacityTrackBar.Maximum = 100;
        opacityTrackBar.TickFrequency = 10;
        opacityTrackBar.Value = Settings.OverlayOpacity;
        opacityTrackBar.ValueChanged += (_, _) => UpdatePreview();

        ConfigureValueLabel(opacityValueLabel, new Point(410, 198));

        Label positionLabel = CreateFieldLabel("Vertical position", new Point(22, 242));
        positionTrackBar.BackColor = ModernTheme.CardBackground;
        positionTrackBar.Location = new Point(172, 230);
        positionTrackBar.Size = new Size(224, 42);
        positionTrackBar.Minimum = 5;
        positionTrackBar.Maximum = 95;
        positionTrackBar.TickFrequency = 10;
        positionTrackBar.Value = Settings.OverlayVerticalPosition;
        positionTrackBar.ValueChanged += (_, _) => UpdatePreview();

        ConfigureValueLabel(positionValueLabel, new Point(410, 242));

        ConfigureCheck(startupCheckBox, "Start CatLocker with Windows", new Point(22, 282), Settings.StartWithWindows);
        ConfigureCheck(soundsCheckBox, "Play soft sounds on lock / unlock", new Point(22, 306), Settings.EnableSounds);
        ConfigureCheck(touchCheckBox, "Block touch & pen (fullscreen barrier)", new Point(22, 330), Settings.BlockTouch);
        ConfigureCheck(darkCheckBox, "Dark theme", new Point(22, 354), Settings.DarkMode);

        previewPanel.Location = new Point(14, 10);
        previewPanel.Size = new Size(452, 40);
        previewPanel.BackColor = ModernTheme.OverlayBackground;
        previewPanel.Paint += PaintPreviewBorder;
        previewPanel.Resize += (_, _) => ApplyRoundedRegion(previewPanel, ModernTheme.RadiusMedium);
        ApplyRoundedRegion(previewPanel, ModernTheme.RadiusMedium);

        previewLabel.Dock = DockStyle.Fill;
        previewLabel.BackColor = Color.Transparent;
        previewLabel.ForeColor = Color.White;
        previewLabel.Font = new Font("Segoe UI Semibold", 9F, FontStyle.Bold, GraphicsUnit.Point);
        previewLabel.Padding = new Padding(48, 0, 18, 0);
        previewLabel.TextAlign = ContentAlignment.MiddleCenter;
        previewPanel.Controls.Add(previewLabel);

        Label limitsLabel = new()
        {
            AutoSize = false,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = ModernTheme.TextSecondary,
            Location = new Point(24, 596),
            Size = new Size(472, 32),
            Text = "Press your unlock hotkey to lock / unlock. Windows always allows Ctrl+Alt+Del and Win+L — a system limit, not a bug."
        };

        ModernButton saveButton = new()
        {
            DialogResult = DialogResult.OK,
            Location = new Point(282, 644),
            Primary = true,
            Text = "Save"
        };
        saveButton.Click += (_, _) =>
        {
            if (!TrySaveValues())
            {
                DialogResult = DialogResult.None;
            }
        };

        ModernButton cancelButton = new()
        {
            DialogResult = DialogResult.Cancel,
            Location = new Point(396, 644),
            Text = "Cancel"
        };

        settingsCard.Controls.Add(settingsCaption);
        settingsCard.Controls.Add(modeLabel);
        settingsCard.Controls.Add(lockModeComboBox);
        settingsCard.Controls.Add(modeHintLabel);
        settingsCard.Controls.Add(hotkeyLabel);
        settingsCard.Controls.Add(ctrlCheckBox);
        settingsCard.Controls.Add(shiftCheckBox);
        settingsCard.Controls.Add(altCheckBox);
        settingsCard.Controls.Add(hotkeyComboBox);
        settingsCard.Controls.Add(hotkeyHintLabel);
        settingsCard.Controls.Add(autoLockLabel);
        settingsCard.Controls.Add(autoLockNumeric);
        settingsCard.Controls.Add(autoLockUnit);
        settingsCard.Controls.Add(autoUnlockLabel);
        settingsCard.Controls.Add(autoUnlockNumeric);
        settingsCard.Controls.Add(autoUnlockUnit);
        settingsCard.Controls.Add(timersHintLabel);
        settingsCard.Controls.Add(opacityLabel);
        settingsCard.Controls.Add(opacityTrackBar);
        settingsCard.Controls.Add(opacityValueLabel);
        settingsCard.Controls.Add(positionLabel);
        settingsCard.Controls.Add(positionTrackBar);
        settingsCard.Controls.Add(positionValueLabel);
        settingsCard.Controls.Add(startupCheckBox);
        settingsCard.Controls.Add(soundsCheckBox);
        settingsCard.Controls.Add(touchCheckBox);
        settingsCard.Controls.Add(darkCheckBox);
        previewCard.Controls.Add(previewPanel);

        AcceptButton = saveButton;
        CancelButton = cancelButton;

        Controls.Add(heroPanel);
        Controls.Add(settingsCard);
        Controls.Add(previewCard);
        Controls.Add(limitsLabel);
        Controls.Add(saveButton);
        Controls.Add(cancelButton);

        UpdatePreview();
    }

    public AppSettings Settings { get; private set; }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        if (DialogResult != DialogResult.OK)
        {
            Settings = originalSettings;
        }

        base.OnFormClosed(e);
    }

    private void ConfigureHotkeyCheck(CheckBox checkBox, string text, Point location, bool isChecked)
    {
        checkBox.AutoSize = true;
        checkBox.BackColor = Color.Transparent;
        checkBox.FlatStyle = FlatStyle.System;
        checkBox.ForeColor = ModernTheme.TextPrimary;
        checkBox.Location = location;
        checkBox.Text = text;
        checkBox.Checked = isChecked;
        checkBox.CheckedChanged += (_, _) => UpdatePreview();
    }

    private void FillHotkeyKeys(uint selectedVk)
    {
        hotkeyValues.Clear();
        hotkeyComboBox.Items.Clear();

        int selectedIndex = 0;
        void Add(uint vk)
        {
            hotkeyValues.Add(vk);
            hotkeyComboBox.Items.Add(UnlockHotkey.KeyName(vk));
            if (vk == selectedVk)
            {
                selectedIndex = hotkeyValues.Count - 1;
            }
        }

        for (uint vk = 0x41; vk <= 0x5A; vk++)
        {
            Add(vk);
        }

        for (uint vk = 0x30; vk <= 0x39; vk++)
        {
            Add(vk);
        }

        for (uint vk = 0x70; vk <= 0x7B; vk++)
        {
            Add(vk);
        }

        hotkeyComboBox.SelectedIndex = selectedIndex;
        hotkeyComboBox.SelectedIndexChanged += (_, _) => UpdatePreview();
    }

    private static void ConfigureTimerNumeric(NumericUpDown numeric, Point location, int value)
    {
        numeric.BackColor = Color.White;
        numeric.Location = location;
        numeric.Size = new Size(52, 23);
        numeric.Minimum = 0;
        numeric.Maximum = 240;
        numeric.Value = Math.Clamp(value, 0, 240);
    }

    private static void ConfigureCheck(CheckBox checkBox, string text, Point location, bool isChecked)
    {
        checkBox.AutoSize = true;
        checkBox.BackColor = Color.Transparent;
        checkBox.FlatStyle = FlatStyle.System;
        checkBox.ForeColor = ModernTheme.TextPrimary;
        checkBox.Location = location;
        checkBox.Text = text;
        checkBox.Checked = isChecked;
    }

    private UnlockHotkey CurrentUiChord()
    {
        uint vk = hotkeyValues.Count > 0 && hotkeyComboBox.SelectedIndex >= 0
            ? hotkeyValues[hotkeyComboBox.SelectedIndex]
            : NativeMethods.VkL;
        return new UnlockHotkey(ctrlCheckBox.Checked, shiftCheckBox.Checked, altCheckBox.Checked, vk);
    }

    private bool TrySaveValues()
    {
        UnlockHotkey chord = CurrentUiChord();
        if (!chord.Ctrl && !chord.Shift && !chord.Alt)
        {
            UpdatePreview();
            MessageBox.Show(
                "Choose at least one modifier (Ctrl / Shift / Alt) for the unlock hotkey.",
                "CatLocker",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            return false;
        }

        Settings.OverlayOpacity = opacityTrackBar.Value;
        Settings.OverlayVerticalPosition = positionTrackBar.Value;
        Settings.LockMode = lockModeComboBox.SelectedIndex == 0 ? LockMode.KeyboardOnly : LockMode.KeyboardAndMouse;
        Settings.StartWithWindows = startupCheckBox.Checked;
        Settings.EnableSounds = soundsCheckBox.Checked;
        Settings.BlockTouch = touchCheckBox.Checked;
        Settings.DarkMode = darkCheckBox.Checked;
        Settings.UnlockHotkey = chord.PersistText;
        Settings.AutoLockMinutes = (int)autoLockNumeric.Value;
        Settings.AutoUnlockMinutes = (int)autoUnlockNumeric.Value;
        Settings.Normalize();
        return true;
    }

    private void UpdatePreview()
    {
        int opacity = opacityTrackBar.Value;
        LockMode mode = lockModeComboBox.SelectedIndex == 0 ? LockMode.KeyboardOnly : LockMode.KeyboardAndMouse;
        UnlockHotkey chord = CurrentUiChord();

        previewPanel.BackColor = Blend(Color.White, ModernTheme.OverlayBackground, opacity / 100d);
        previewLabel.Text = mode.ToOverlayText(chord.DisplayText);
        opacityValueLabel.Text = $"{opacity}%";
        positionValueLabel.Text = $"{positionTrackBar.Value}%";
        modeHintLabel.Text = mode == LockMode.KeyboardOnly
            ? "Only keyboard input will be blocked."
            : "Keyboard and mouse input will be blocked.";

        bool chordValid = chord.Ctrl || chord.Shift || chord.Alt;
        hotkeyHintLabel.ForeColor = chordValid ? ModernTheme.TextSecondary : ModernTheme.Danger;
        hotkeyHintLabel.Text = chordValid
            ? "Swallowed while locked — apps won't see it."
            : "Pick at least one modifier.";

        badgeLabel.Text = chord.DisplayText;
        int badgeWidth = Math.Clamp(chord.DisplayText.Length * 7 + 30, 106, 190);
        badgePanel.Size = new Size(badgeWidth, 28);
        badgePanel.Location = new Point(478 - badgeWidth, 20);
        ApplyRoundedRegion(badgePanel, ModernTheme.RadiusMedium);

        previewPanel.Invalidate();
    }

    private static Panel CreateHeroPanel(Point location, Size size)
    {
        Panel panel = new()
        {
            BackColor = ModernTheme.Accent,
            Location = location,
            Size = size
        };

        panel.Resize += (_, _) => ApplyRoundedRegion(panel, ModernTheme.RadiusHero);
        panel.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rectangle = new(0, 0, panel.Width - 1, panel.Height - 1);
            using GraphicsPath path = ModernTheme.CreateRoundedRectangle(rectangle, ModernTheme.RadiusHero);
            using LinearGradientBrush brush = new(rectangle, ModernTheme.Accent, ModernTheme.AccentDeep, LinearGradientMode.ForwardDiagonal);
            using Pen pen = new(Color.FromArgb(70, Color.White), 1F);
            e.Graphics.FillPath(brush, path);
            e.Graphics.DrawPath(pen, path);
        };

        ApplyRoundedRegion(panel, ModernTheme.RadiusHero);
        return panel;
    }

    private static Panel CreateCard(Point location, Size size)
    {
        Panel panel = new()
        {
            BackColor = ModernTheme.WindowBackground,
            Location = location,
            Size = size
        };

        panel.Resize += (_, _) => ApplyRoundedRegion(panel, ModernTheme.RadiusLarge);
        panel.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rectangle = new(0, 0, panel.Width - 1, panel.Height - 1);
            using GraphicsPath path = ModernTheme.CreateRoundedRectangle(rectangle, ModernTheme.RadiusLarge);
            using LinearGradientBrush brush = new(rectangle, ModernTheme.CardBackground, ModernTheme.CardElevated, LinearGradientMode.Vertical);
            using Pen pen = new(ModernTheme.SoftBorder, 1F);
            e.Graphics.FillPath(brush, path);
            e.Graphics.DrawPath(pen, path);
        };

        ApplyRoundedRegion(panel, ModernTheme.RadiusLarge);
        return panel;
    }

    private static Panel CreateIconPanel(Point location)
    {
        Panel panel = new()
        {
            BackColor = Color.Transparent,
            Location = location,
            Size = new Size(44, 44)
        };

        Label iconLabel = new()
        {
            BackColor = Color.Transparent,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Emoji", 17F, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = Color.White,
            Text = "🐱",
            TextAlign = ContentAlignment.MiddleCenter
        };

        panel.Controls.Add(iconLabel);
        panel.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            using SolidBrush brush = new(Color.FromArgb(45, Color.White));
            e.Graphics.FillEllipse(brush, 0, 0, panel.Width - 1, panel.Height - 1);
        };

        return panel;
    }

    private static Label CreateSectionCaption(string text, Point location)
    {
        return new Label
        {
            AutoSize = true,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI Semibold", 10F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = ModernTheme.TextPrimary,
            Location = location,
            Text = text
        };
    }

    private static Label CreateFieldLabel(string text, Point location)
    {
        return new Label
        {
            AutoSize = true,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = ModernTheme.TextPrimary,
            Location = location,
            Text = text
        };
    }

    private static void ConfigureValueLabel(Label label, Point location)
    {
        label.AutoSize = false;
        label.BackColor = Color.Transparent;
        label.Font = new Font("Segoe UI Semibold", 8.25F, FontStyle.Bold, GraphicsUnit.Point);
        label.ForeColor = ModernTheme.Accent;
        label.Location = location;
        label.Size = new Size(44, 18);
        label.TextAlign = ContentAlignment.MiddleRight;
    }

    private static Color Blend(Color background, Color foreground, double opacity)
    {
        opacity = Math.Clamp(opacity, 0d, 1d);
        int r = (int)((foreground.R * opacity) + (background.R * (1d - opacity)));
        int g = (int)((foreground.G * opacity) + (background.G * (1d - opacity)));
        int b = (int)((foreground.B * opacity) + (background.B * (1d - opacity)));
        return Color.FromArgb(r, g, b);
    }

    private static void PaintPreviewBorder(object? sender, PaintEventArgs e)
    {
        if (sender is not Panel panel)
        {
            return;
        }

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        using SolidBrush glowBrush = new(Color.FromArgb(42, Color.White));
        e.Graphics.FillEllipse(glowBrush, 18, 12, 16, 16);
        using Pen iconPen = new(Color.FromArgb(190, Color.White), 1.5F);
        e.Graphics.DrawLine(iconPen, 23, 19, 26, 22);
        e.Graphics.DrawLine(iconPen, 26, 22, 32, 16);
        using Pen borderPen = new(Color.FromArgb(120, Color.White), 1F);
        using GraphicsPath path = ModernTheme.CreateRoundedRectangle(new Rectangle(0, 0, panel.Width - 1, panel.Height - 1), ModernTheme.RadiusMedium);
        e.Graphics.DrawPath(borderPen, path);
    }

    private static void ApplyRoundedRegion(Control control, int radius)
    {
        if (control.Width <= 0 || control.Height <= 0)
        {
            return;
        }

        using GraphicsPath path = ModernTheme.CreateRoundedRectangle(new Rectangle(0, 0, control.Width, control.Height), radius);
        Region? old = control.Region;
        control.Region = new Region(path);
        old?.Dispose();
    }
}
