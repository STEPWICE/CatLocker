using System.Drawing.Drawing2D;

namespace CatLocker;

internal sealed class SettingsForm : Form
{
    private readonly SoftTrackBar opacitySlider = new();
    private readonly SoftTrackBar positionSlider = new();
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
        ClientSize = new Size(520, 706);
        DoubleBuffered = true;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        MaximizeBox = false;
        MinimizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterScreen;
        Text = "CatLocker Settings";
        Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);

        // Hero.
        Panel heroPanel = CreateHeroPanel(new Point(20, 16), new Size(480, 100));
        badgeLabel = new Label
        {
            BackColor = Color.Transparent,
            Dock = DockStyle.Fill,
            Font = new Font("Segoe UI Semibold", 8.25F, FontStyle.Bold, GraphicsUnit.Point),
            ForeColor = ModernTheme.BadgeText,
            TextAlign = ContentAlignment.MiddleCenter,
            UseMnemonic = false
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
            using SolidBrush brush = new(ModernTheme.BadgeBackground);
            using Pen pen = new(Color.FromArgb(60, ModernTheme.BadgeText), 1F);
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
            Text = "CatLocker",
            UseMnemonic = false
        };

        Label descriptionLabel = new()
        {
            AutoSize = false,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 9.5F, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = Color.FromArgb(228, 232, 250),
            Location = new Point(80, 54),
            Size = new Size(360, 36),
            Text = "Soft lock for keyboard and mouse with a calm overlay.",
            UseMnemonic = false
        };

        heroPanel.Controls.Add(iconPanel);
        heroPanel.Controls.Add(titleLabel);
        heroPanel.Controls.Add(descriptionLabel);

        // Cards.
        Panel settingsCard = CreateCard(new Point(20, 128), new Size(480, 392));
        Panel previewCard = CreatePreviewCard(new Point(20, 532), new Size(480, 76));

        Label settingsCaption = CreateSectionCaption("Lock settings", new Point(24, 16));

        Label modeLabel = CreateFieldLabel("Lock mode", new Point(24, 48));
        StyleComboBox(lockModeComboBox, dropDownHeight: 48);
        lockModeComboBox.Items.Add(LockMode.KeyboardOnly.ToDisplayText());
        lockModeComboBox.Items.Add(LockMode.KeyboardAndMouse.ToDisplayText());
        lockModeComboBox.Location = new Point(176, 44);
        lockModeComboBox.Size = new Size(252, 24);
        lockModeComboBox.SelectedIndex = Settings.LockMode == LockMode.KeyboardOnly ? 0 : 1;
        lockModeComboBox.SelectedIndexChanged += (_, _) => UpdatePreview();

        modeHintLabel.AutoSize = false;
        modeHintLabel.BackColor = Color.Transparent;
        modeHintLabel.ForeColor = ModernTheme.TextSecondary;
        modeHintLabel.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point);
        modeHintLabel.Location = new Point(177, 70);
        modeHintLabel.Size = new Size(252, 16);
        modeHintLabel.UseMnemonic = false;

        Label hotkeyLabel = CreateFieldLabel("Unlock hotkey", new Point(24, 98));
        ConfigureHotkeyCheck(ctrlCheckBox, "Ctrl", new Point(176, 96), chord.Ctrl);
        ConfigureHotkeyCheck(shiftCheckBox, "Shift", new Point(240, 96), chord.Shift);
        ConfigureHotkeyCheck(altCheckBox, "Alt", new Point(304, 96), chord.Alt);
        StyleComboBox(hotkeyComboBox, dropDownHeight: 176);
        hotkeyComboBox.Location = new Point(360, 94);
        hotkeyComboBox.Size = new Size(68, 24);
        FillHotkeyKeys(chord.KeyCode);

        hotkeyHintLabel.AutoSize = false;
        hotkeyHintLabel.BackColor = Color.Transparent;
        hotkeyHintLabel.ForeColor = ModernTheme.TextSecondary;
        hotkeyHintLabel.Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point);
        hotkeyHintLabel.Location = new Point(177, 120);
        hotkeyHintLabel.Size = new Size(252, 16);
        hotkeyHintLabel.UseMnemonic = false;

        Label autoLockLabel = CreateFieldLabel("Auto-lock", new Point(24, 150));
        Panel autoLockWrap = WrapNumeric(autoLockNumeric, new Point(104, 147), Settings.AutoLockMinutes);
        Label autoLockUnit = CreateFieldLabel("min idle", new Point(172, 150));
        Label autoUnlockLabel = CreateFieldLabel("Auto-unlock", new Point(238, 150));
        Panel autoUnlockWrap = WrapNumeric(autoUnlockNumeric, new Point(326, 147), Settings.AutoUnlockMinutes);
        Label autoUnlockUnit = CreateFieldLabel("min", new Point(394, 150));

        Label timersHintLabel = new()
        {
            AutoSize = false,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 8.25F, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = ModernTheme.TextSecondary,
            Location = new Point(177, 172),
            Size = new Size(252, 16),
            Text = "0 = off for both.",
            UseMnemonic = false
        };

        Label opacityLabel = CreateFieldLabel("Overlay opacity", new Point(24, 200));
        opacitySlider.Location = new Point(172, 192);
        opacitySlider.Size = new Size(224, 28);
        opacitySlider.Minimum = 20;
        opacitySlider.Maximum = 100;
        opacitySlider.TickFrequency = 10;
        opacitySlider.Value = Settings.OverlayOpacity;
        opacitySlider.ValueChanged += (_, _) => UpdatePreview();

        ConfigureValueLabel(opacityValueLabel, new Point(406, 200));

        Label positionLabel = CreateFieldLabel("Vertical position", new Point(24, 242));
        positionSlider.Location = new Point(172, 234);
        positionSlider.Size = new Size(224, 28);
        positionSlider.Minimum = 5;
        positionSlider.Maximum = 95;
        positionSlider.TickFrequency = 10;
        positionSlider.Value = Settings.OverlayVerticalPosition;
        positionSlider.ValueChanged += (_, _) => UpdatePreview();

        ConfigureValueLabel(positionValueLabel, new Point(406, 242));

        ConfigureCheck(startupCheckBox, "Start CatLocker with Windows", new Point(24, 284), Settings.StartWithWindows);
        ConfigureCheck(soundsCheckBox, "Play soft sounds on lock and unlock", new Point(24, 308), Settings.EnableSounds);
        ConfigureCheck(touchCheckBox, "Block touch and pen (fullscreen barrier)", new Point(24, 332), Settings.BlockTouch);
        ConfigureCheck(darkCheckBox, "Dark theme", new Point(24, 356), Settings.DarkMode);

        previewPanel.Location = new Point(16, 18);
        previewPanel.Size = new Size(448, 40);
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
        previewLabel.UseMnemonic = false;
        previewPanel.Controls.Add(previewLabel);

        Label limitsLabel = new()
        {
            AutoSize = false,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 8F, FontStyle.Regular, GraphicsUnit.Point),
            ForeColor = ModernTheme.TextSecondary,
            Location = new Point(24, 614),
            Size = new Size(472, 32),
            Text = "Press your unlock hotkey to lock and unlock. Windows always allows Ctrl+Alt+Del and Win+L — a system limit, not a bug.",
            UseMnemonic = false
        };

        ModernButton saveButton = new()
        {
            DialogResult = DialogResult.OK,
            Location = new Point(282, 660),
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
            Location = new Point(396, 660),
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
        settingsCard.Controls.Add(autoLockWrap);
        settingsCard.Controls.Add(autoLockUnit);
        settingsCard.Controls.Add(autoUnlockLabel);
        settingsCard.Controls.Add(autoUnlockWrap);
        settingsCard.Controls.Add(autoUnlockUnit);
        settingsCard.Controls.Add(timersHintLabel);
        settingsCard.Controls.Add(opacityLabel);
        settingsCard.Controls.Add(opacitySlider);
        settingsCard.Controls.Add(opacityValueLabel);
        settingsCard.Controls.Add(positionLabel);
        settingsCard.Controls.Add(positionSlider);
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

    private static void StyleComboBox(ComboBox combo, int dropDownHeight)
    {
        combo.DrawMode = DrawMode.OwnerDrawFixed;
        combo.FlatStyle = FlatStyle.Flat;
        combo.DropDownStyle = ComboBoxStyle.DropDownList;
        combo.BackColor = ModernTheme.FieldBackground;
        combo.ForeColor = ModernTheme.TextPrimary;
        combo.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        combo.ItemHeight = 20;
        combo.DropDownHeight = dropDownHeight;
        combo.IntegralHeight = false;
        combo.DrawItem += ComboDrawItem;
        combo.DropDownClosed += (_, _) => combo.Invalidate();
        combo.SelectedIndexChanged += (_, _) => combo.Invalidate();
    }

    private static void ComboDrawItem(object? sender, DrawItemEventArgs e)
    {
        if (sender is not ComboBox combo)
        {
            return;
        }

        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        int index = e.Index < 0 ? combo.SelectedIndex : e.Index;
        bool selected = e.Index >= 0 && (e.State & DrawItemState.Selected) == DrawItemState.Selected;

        Color back = selected ? ModernTheme.MenuSelected : ModernTheme.FieldBackground;
        using (SolidBrush brush = new(back))
        {
            e.Graphics.FillRectangle(brush, e.Bounds);
        }

        string text = index >= 0 && index < combo.Items.Count
            ? combo.GetItemText(combo.Items[index]) ?? string.Empty
            : combo.Text;

        Rectangle textRect = new(e.Bounds.X + 8, e.Bounds.Y, e.Bounds.Width - 26, e.Bounds.Height);
        TextRenderer.DrawText(
            e.Graphics,
            text,
            combo.Font,
            textRect,
            ModernTheme.TextPrimary,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.EndEllipsis);

        if (e.Index < 0)
        {
            // Closed combo: themed border + chevron.
            using Pen border = new(ModernTheme.FieldBorder, 1F);
            e.Graphics.DrawRectangle(border, new Rectangle(e.Bounds.X, e.Bounds.Y, e.Bounds.Width - 1, e.Bounds.Height - 1));
            using Pen chevron = new(ModernTheme.TextSecondary, 1.6F);
            int cx = e.Bounds.Right - 14;
            int cy = e.Bounds.Top + e.Bounds.Height / 2 - 1;
            e.Graphics.DrawLines(chevron, new[] { new Point(cx - 4, cy - 2), new Point(cx, cy + 2), new Point(cx + 4, cy - 2) });
        }
        else if ((e.State & DrawItemState.Focus) == DrawItemState.Focus)
        {
            e.DrawFocusRectangle();
        }
    }

    private static Panel WrapNumeric(NumericUpDown numeric, Point location, int value)
    {
        numeric.BorderStyle = BorderStyle.None;
        numeric.BackColor = ModernTheme.FieldBackground;
        numeric.ForeColor = ModernTheme.TextPrimary;
        numeric.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point);
        numeric.Minimum = 0;
        numeric.Maximum = 240;
        numeric.Value = Math.Clamp(value, 0, 240);
        numeric.TextAlign = HorizontalAlignment.Left;
        numeric.Location = new Point(7, 4);
        numeric.Size = new Size(46, 18);

        Panel wrapper = new()
        {
            BackColor = ModernTheme.FieldBackground,
            Location = location,
            Size = new Size(60, 26)
        };
        wrapper.Controls.Add(numeric);
        wrapper.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rectangle = new(0, 0, wrapper.Width - 1, wrapper.Height - 1);
            using GraphicsPath path = ModernTheme.CreateRoundedRectangle(rectangle, ModernTheme.RadiusSmall);
            using Pen pen = new(ModernTheme.FieldBorder, 1F);
            e.Graphics.DrawPath(pen, path);
        };
        wrapper.Resize += (_, _) => ApplyRoundedRegion(wrapper, ModernTheme.RadiusSmall);
        ApplyRoundedRegion(wrapper, ModernTheme.RadiusSmall);
        return wrapper;
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
        checkBox.UseMnemonic = false;
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

    private static void ConfigureCheck(CheckBox checkBox, string text, Point location, bool isChecked)
    {
        checkBox.AutoSize = true;
        checkBox.BackColor = Color.Transparent;
        checkBox.FlatStyle = FlatStyle.System;
        checkBox.ForeColor = ModernTheme.TextPrimary;
        checkBox.Location = location;
        checkBox.Text = text;
        checkBox.Checked = isChecked;
        checkBox.UseMnemonic = false;
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

        Settings.OverlayOpacity = opacitySlider.Value;
        Settings.OverlayVerticalPosition = positionSlider.Value;
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
        int opacity = opacitySlider.Value;
        LockMode mode = lockModeComboBox.SelectedIndex == 0 ? LockMode.KeyboardOnly : LockMode.KeyboardAndMouse;
        UnlockHotkey chord = CurrentUiChord();

        previewPanel.BackColor = Blend(Color.White, ModernTheme.OverlayBackground, opacity / 100d);
        previewLabel.Text = mode.ToOverlayText(chord.DisplayText);
        opacityValueLabel.Text = $"{opacity}%";
        positionValueLabel.Text = $"{positionSlider.Value}%";
        modeHintLabel.Text = mode == LockMode.KeyboardOnly
            ? "Only keyboard input will be blocked."
            : "Keyboard and mouse input will be blocked.";

        bool chordValid = chord.Ctrl || chord.Shift || chord.Alt;
        hotkeyHintLabel.ForeColor = chordValid ? ModernTheme.TextSecondary : ModernTheme.Danger;
        hotkeyHintLabel.Text = chordValid
            ? "Swallowed while locked — apps won't see it."
            : "Pick at least one modifier.";

        badgeLabel.Text = chord.DisplayText;
        int badgeWidth = Math.Clamp(chord.DisplayText.Length * 7 + 32, 110, 190);
        badgePanel.Size = new Size(badgeWidth, 28);
        badgePanel.Location = new Point(478 - badgeWidth, 36);
        ApplyRoundedRegion(badgePanel, ModernTheme.RadiusMedium);

        previewPanel.Invalidate();
    }

    private static Panel CreateHeroPanel(Point location, Size size)
    {
        Panel panel = new()
        {
            BackColor = ModernTheme.HeroStart,
            Location = location,
            Size = size
        };

        panel.Resize += (_, _) => ApplyRoundedRegion(panel, ModernTheme.RadiusHero);
        panel.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rectangle = new(0, 0, panel.Width - 1, panel.Height - 1);
            using GraphicsPath path = ModernTheme.CreateRoundedRectangle(rectangle, ModernTheme.RadiusHero);
            using LinearGradientBrush brush = new(rectangle, ModernTheme.HeroStart, ModernTheme.HeroEnd, LinearGradientMode.ForwardDiagonal);
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

    private static Panel CreatePreviewCard(Point location, Size size)
    {
        // Solid contrasting zone so the dark overlay pill reads in both themes.
        Panel panel = new()
        {
            BackColor = ModernTheme.PreviewBackdrop,
            Location = location,
            Size = size
        };

        panel.Resize += (_, _) => ApplyRoundedRegion(panel, ModernTheme.RadiusLarge);
        panel.Paint += (_, e) =>
        {
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
            Rectangle rectangle = new(0, 0, panel.Width - 1, panel.Height - 1);
            using GraphicsPath path = ModernTheme.CreateRoundedRectangle(rectangle, ModernTheme.RadiusLarge);
            using SolidBrush brush = new(ModernTheme.PreviewBackdrop);
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
            TextAlign = ContentAlignment.MiddleCenter,
            UseMnemonic = false
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
            Text = text,
            UseMnemonic = false
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
            Text = text,
            UseMnemonic = false
        };
    }

    private static void ConfigureValueLabel(Label label, Point location)
    {
        label.AutoSize = false;
        label.BackColor = Color.Transparent;
        label.Font = new Font("Segoe UI Semibold", 8.25F, FontStyle.Bold, GraphicsUnit.Point);
        label.ForeColor = ModernTheme.AccentStrong;
        label.Location = location;
        label.Size = new Size(48, 18);
        label.TextAlign = ContentAlignment.MiddleRight;
        label.UseMnemonic = false;
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
