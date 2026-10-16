using GHelper.AutoUpdate;
using GHelper.Battery;
using GHelper.Display;
using GHelper.Fan;
using GHelper.Gpu;
using GHelper.Helpers;
using GHelper.Input;
using GHelper.Mode;
using GHelper.Properties;
using GHelper.UI;
using System.Diagnostics;
using System.Timers;

namespace GHelper
{
    public partial class SettingsForm : RForm
    {
        ContextMenuStrip contextMenuStrip = new CustomContextMenu();
        ToolStripMenuItem menuEco, menuStandard, menuUltimate, menuOptimized;
        DonateControl donateControl;

        public GPUModeControl gpuControl;
        AutoUpdateControl updateControl;

        public static System.Timers.Timer sensorTimer = default!;
        private static readonly bool sensorsAlways = AppConfig.Is("sensors_always");
        private readonly System.Windows.Forms.Timer batteryTimer = new() { Interval = 200 };

        public Fans? fansForm;
        public Extra? extraForm;
        public Updates? updatesForm;
        public OverlayConfig? overlayForm;

        static long lastRefresh;
        static long lastBatteryRefresh;
        static long lastLostFocus;

        bool isGpuSection = true;
        bool isMuxGpu = true;

        bool batteryMouseOver = false;
        bool batteryFullMouseOver = false;

        bool sliderGammaIgnore = false;

        /// <summary>Dell-only Cool thermal pill (added; does not replace Quiet/Balanced/Turbo).</summary>
        private RButton? buttonCool;

        public SettingsForm()
        {

            InitializeComponent();
            InitTheme(true);

            gpuControl = new GPUModeControl(this);
            updateControl = new AutoUpdateControl(this);

            buttonSilent.Text = Properties.Strings.Silent;
            buttonBalanced.Text = Properties.Strings.Balanced;
            buttonTurbo.Text = Properties.Strings.Turbo;
            buttonFans.Text = Properties.Strings.FansPower;

            buttonEco.Text = Properties.Strings.EcoMode;
            buttonUltimate.Text = Properties.Strings.UltimateMode;
            buttonStandard.Text = Properties.Strings.StandardMode;
            buttonOptimized.Text = Properties.Strings.Optimized;
            buttonStopGPU.Text = Properties.Strings.StopGPUApps;

            buttonScreenAuto.Text = Properties.Strings.AutoMode;
            buttonMiniled.Text = Properties.Strings.Multizone;

            buttonKeyboardColor.Text = Properties.Strings.Color;
            buttonKeyboard.Text = Properties.Strings.Extra;

            labelPerf.Text = Properties.Strings.PerformanceMode;
            labelGPU.Text = Properties.Strings.GPUMode;
            labelSreen.Text = Properties.Strings.LaptopScreen;
            UpdateKeyboardLabel();
            labelMatrix.Text = Properties.Strings.AnimeMatrix;
            labelRearLight.Text = Properties.Strings.RearLight;
            labelBatteryTitle.Text = Properties.Strings.BatteryChargeLimit;

            checkStartup.Text = Properties.Strings.RunOnStartup;

            buttonMatrix.Text = "Matrix";
            buttonQuit.Text = Properties.Strings.Quit;
            buttonUpdates.Text = Properties.Strings.Updates;
            buttonDonate.Text = Properties.Strings.Donate;

            buttonController.Text = Properties.Strings.Controller;
            labelAlly.Text = Properties.Strings.AllyController;

            // Accessible Labels

            panelMatrix.AccessibleName = Properties.Strings.AnimeMatrix;
            sliderBattery.AccessibleName = Properties.Strings.BatteryChargeLimit;
            buttonQuit.AccessibleName = Properties.Strings.Quit;
            buttonUpdates.AccessibleName = Properties.Strings.BiosAndDriverUpdates;
            panelPerformance.AccessibleName = Properties.Strings.PerformanceMode;
            buttonSilent.AccessibleName = Properties.Strings.Silent;
            buttonBalanced.AccessibleName = Properties.Strings.Balanced;
            buttonTurbo.AccessibleName = Properties.Strings.Turbo;
            buttonFans.AccessibleName = Properties.Strings.FansAndPower;
            panelGPU.AccessibleName = Properties.Strings.GPUMode;
            buttonEco.AccessibleName = Properties.Strings.EcoMode;
            buttonStandard.AccessibleName = Properties.Strings.StandardMode;
            buttonOptimized.AccessibleName = Properties.Strings.Optimized;
            buttonUltimate.AccessibleName = Properties.Strings.UltimateMode;
            panelScreen.AccessibleName = Properties.Strings.LaptopScreen;

            buttonScreenAuto.AccessibleName = Properties.Strings.AutoMode;
            //button60Hz.AccessibleName = "60Hz Refresh Rate";
            //button120Hz.AccessibleName = "Maximum Refresh Rate";

            panelKeyboard.AccessibleName = Properties.Strings.LaptopKeyboard;
            buttonKeyboard.AccessibleName = Properties.Strings.ExtraSettings;
            buttonKeyboardColor.AccessibleName = Properties.Strings.LaptopKeyboard + " " + Properties.Strings.Color;
            comboKeyboard.AccessibleName = Properties.Strings.LaptopBacklight;

            FormClosing += SettingsForm_FormClosing;
            Deactivate += SettingsForm_LostFocus;

            buttonSilent.BorderColor = colorEco;
            buttonBalanced.BorderColor = colorStandard;
            buttonTurbo.BorderColor = colorTurbo;
            buttonFans.BorderColor = colorCustom;

            buttonEco.BorderColor = colorEco;
            buttonStandard.BorderColor = colorStandard;
            buttonUltimate.BorderColor = colorTurbo;
            buttonOptimized.BorderColor = colorEco;
            buttonXGM.BorderColor = colorTurbo;

            button60Hz.BorderColor = colorGray;
            button120Hz.BorderColor = colorGray;
            buttonScreenAuto.BorderColor = colorGray;
            buttonMiniled.BorderColor = colorTurbo;

            buttonEnergySaver.BackColor = colorEco;
            buttonEnergySaver.ForeColor = SystemColors.ControlLightLight;
            buttonEnergySaver.Click += ButtonEnergySaver_Click;

            buttonAmdOled.BackColor = colorTurbo;
            buttonAmdOled.ForeColor = SystemColors.ControlLightLight;
            buttonAmdOled.Click += ButtonAmdOled_Click;

            buttonArmoury.BackColor = colorTurbo;
            buttonArmoury.ForeColor = SystemColors.ControlLightLight;
            buttonArmoury.Click += ButtonArmoury_Click;

            buttonSilent.Click += ButtonSilent_Click;
            buttonBalanced.Click += ButtonBalanced_Click;
            buttonTurbo.Click += ButtonTurbo_Click;

            buttonEco.Click += ButtonEco_Click;
            buttonStandard.Click += ButtonStandard_Click;
            buttonUltimate.Click += ButtonUltimate_Click;
            buttonOptimized.Click += ButtonOptimized_Click;
            buttonStopGPU.Click += ButtonStopGPU_Click;
            pictureGPU.Click += PictureGPU_Click;

            VisibleChanged += SettingsForm_VisibleChanged;

            button60Hz.Click += Button60Hz_Click;
            button120Hz.Click += Button120Hz_Click;
            buttonScreenAuto.Click += ButtonScreenAuto_Click;
            buttonMiniled.Click += ButtonMiniled_Click;
            buttonFHD.Click += ButtonFHD_Click;
            buttonHDRControl.Click += ButtonHDRControl_Click;

            buttonQuit.Click += ButtonQuit_Click;

            buttonKeyboardColor.Click += ButtonKeyboardColor_Click;
            buttonKeyboardColor.Swatch2Click += ButtonKeyboardColor2_Click;

            buttonFans.Click += ButtonFans_Click;
            buttonKeyboard.Click += ButtonKeyboard_Click;

            labelCPUFan.Click += LabelCPUFan_Click;
            labelGPUFan.Click += LabelCPUFan_Click;

            comboMatrix.DropDownStyle = ComboBoxStyle.DropDownList;
            comboMatrixRunning.DropDownStyle = ComboBoxStyle.DropDownList;

            checkStartup.Checked = Startup.IsScheduled();
            checkStartup.CheckedChanged += CheckStartup_CheckedChanged;

            labelVersion.Click += LabelVersion_Click;
            labelVersion.ForeColor = Color.FromArgb(128, Color.Gray);

            buttonOptimized.MouseMove += ButtonOptimized_MouseHover;
            buttonOptimized.MouseLeave += ButtonGPU_MouseLeave;

            buttonEco.MouseMove += ButtonEco_MouseHover;
            buttonEco.MouseLeave += ButtonGPU_MouseLeave;

            buttonStandard.MouseMove += ButtonStandard_MouseHover;
            buttonStandard.MouseLeave += ButtonGPU_MouseLeave;

            buttonUltimate.MouseMove += ButtonUltimate_MouseHover;
            buttonUltimate.MouseLeave += ButtonGPU_MouseLeave;

            tableGPU.MouseLeave += ButtonGPU_MouseLeave;

            buttonScreenAuto.MouseMove += ButtonScreenAuto_MouseHover;
            buttonScreenAuto.MouseLeave += ButtonScreen_MouseLeave;

            button60Hz.MouseMove += Button60Hz_MouseHover;
            button60Hz.MouseLeave += ButtonScreen_MouseLeave;

            button120Hz.MouseMove += Button120Hz_MouseHover;
            button120Hz.MouseLeave += ButtonScreen_MouseLeave;

            buttonFHD.MouseMove += ButtonFHD_MouseHover;
            buttonFHD.MouseLeave += ButtonScreen_MouseLeave;

            buttonUpdates.Click += ButtonUpdates_Click;

            sliderBattery.MouseUp += SliderBattery_MouseUp;
            sliderBattery.KeyUp += SliderBattery_KeyUp;
            sliderBattery.ValueChanged += SliderBattery_ValueChanged;
            batteryTimer.Tick += (_, _) => { batteryTimer.Stop(); BatteryControl.SetBatteryChargeLimit(sliderBattery.Value); };
            if (AppConfig.IsChargeLimit6080()) sliderBattery.supportedValues = new() { 60, 65, 70, 75, 80, 100 };

            sensorTimer = new System.Timers.Timer(AppConfig.Get("sensor_timer", 1000));
            sensorTimer.Elapsed += OnTimedEvent;
            sensorTimer.Enabled = sensorsAlways;

            labelCharge.MouseEnter += PanelBattery_MouseEnter;
            labelCharge.MouseLeave += PanelBattery_MouseLeave;
            labelBattery.Click += LabelBattery_Click;

            buttonBatteryFull.MouseEnter += ButtonBatteryFull_MouseEnter;
            buttonBatteryFull.MouseLeave += ButtonBatteryFull_MouseLeave;
            buttonBatteryFull.Click += ButtonBatteryFull_Click;

            buttonOverlay.Click += ButtonOverlay_Click;
            buttonOverlay.MouseUp += (s, e) => { if (e.Button == MouseButtons.Right) ToggleOverlay(); };
            buttonOverlay.Text = Properties.Strings.Overlay;
            buttonOverlayAlly.Click += ButtonOverlay_Click;
            buttonOverlayAlly.BorderColor = colorStandard;
            VisualiseOverlay();
            buttonKeyboard.SizeChanged += (s, e) => AlignFnLock();
            AlignFnLock();

            Text = "D-Helper " + (ProcessHelper.IsUserAdministrator() ? "—" : "-") + " " + AppConfig.GetModelShort();
            TopMost = AppConfig.Is("topmost");

            //This will auto position the window again when it resizes. Might mess with position if people drag the window somewhere else.
            this.Resize += SettingsForm_Resize;

            VisualiseFnLock();
            buttonFnLock.Click += ButtonFnLock_Click;

            labelVisual.Click += LabelVisual_Click;
            labelCharge.Click += LabelCharge_Click;

            donateControl = new DonateControl(this, buttonDonate);
            donateControl.Init();

            labelBacklight.ForeColor = colorStandard;
            labelBacklight.Click += LabelBacklight_Click;

            panelPerformance.Focus();
            InitVisual();
        }

        private void ButtonArmoury_Click(object? sender, EventArgs e)
        {
            // Armoury Crate path removed (Dell-only)
        }


        private void ButtonAmdOled_Click(object? sender, EventArgs e)
        {
            if (VisualControl.DisableOledPowerOptimization()) VisualiseAmdOled(false);
        }

        private void LabelBattery_Click(object? sender, EventArgs e)
        {
            HardwareControl.chargeWatt = !HardwareControl.chargeWatt;
            RefreshSensors(true);
        }

        private void ButtonEnergySaver_Click(object? sender, EventArgs e)
        {
            PowerNative.SetEnergySaver(false);
        }

        private void LabelBacklight_Click(object? sender, EventArgs e)
        {
            if (AppConfig.IsDynamicLighting() && DynamicLightingHelper.IsEnabled()) DynamicLightingHelper.OpenSettings();
        }

        private void ButtonFHD_Click(object? sender, EventArgs e)
        {
            ScreenControl.ToogleFHD();
        }

        private void ButtonHDRControl_Click(object? sender, EventArgs e)
        {
            ScreenControl.ToogleHDRControl();
        }

        private void SliderBattery_ValueChanged(object? sender, EventArgs e)
        {
            VisualiseBatteryTitle(sliderBattery.Value);
        }

        private void SliderBattery_KeyUp(object? sender, KeyEventArgs e)
        {
            batteryTimer.Stop();
            batteryTimer.Start();
        }

        private void SliderBattery_MouseUp(object? sender, MouseEventArgs e)
        {
            batteryTimer.Stop();
            batteryTimer.Start();
        }

        private void LabelCharge_Click(object? sender, EventArgs e)
        {
            BatteryControl.BatteryReport();
        }

        private void LabelVisual_Click(object? sender, EventArgs e)
        {
            labelVisual.Visible = false;
            VisualControl.forceVisual = true;
        }

        public void InitVisual()
        {

            if (AppConfig.Is("hide_visual")) return;

            if (AppConfig.IsOLED())
            {
                panelGamma.Visible = true;
                sliderGamma.Visible = true;
                labelGammaTitle.Text = Properties.Strings.FlickerFreeDimming + " / " + Properties.Strings.VisualMode;

                VisualiseBrightness();

                sliderGamma.ValueChanged += SliderGamma_ValueChanged;
                sliderGamma.MouseUp += SliderGamma_ValueChanged;

            }
            else
            {
                labelGammaTitle.Text = Properties.Strings.VisualMode;
            }

            var gamuts = VisualControl.GetGamutModes();

            // Color profiles exist
            if (gamuts.Count > 0)
            {
                tableVisual.ColumnCount = 3;
                buttonInstallColor.Visible = false;
            }
            else
            {
                // If it's possible to retrieve color profiles
                if (ColorProfileHelper.ProfileExists())
                {
                    tableVisual.ColumnCount = 2;

                    buttonInstallColor.Text = Properties.Strings.DownloadColorProfiles;
                    buttonInstallColor.Visible = true;
                    buttonInstallColor.Click += ButtonInstallColorProfile_Click;

                    panelGamma.Visible = true;
                    tableVisual.Visible = true;
                }

                return;
            }

            panelGamma.Visible = true;
            tableVisual.Visible = true;

            var visualValue = (SplendidCommand)AppConfig.Get("visual", (int)VisualControl.GetDefaultVisualMode());
            var colorTempValue = AppConfig.Get("color_temp", VisualControl.DefaultColorTemp);

            comboVisual.DropDownStyle = ComboBoxStyle.DropDownList;
            comboVisual.DataSource = new BindingSource(VisualControl.GetVisualModes(), null);
            comboVisual.DisplayMember = "Value";
            comboVisual.ValueMember = "Key";
            comboVisual.SelectedValue = visualValue;

            comboColorTemp.DropDownStyle = ComboBoxStyle.DropDownList;
            comboColorTemp.DataSource = new BindingSource(VisualControl.GetTemperatures(), null);
            comboColorTemp.DisplayMember = "Value";
            comboColorTemp.ValueMember = "Key";
            comboColorTemp.SelectedValue = colorTempValue;

            VisualControl.SetVisual(visualValue, colorTempValue, true);

            comboVisual.SelectedValueChanged += ComboVisual_SelectedValueChanged;
            comboVisual.Visible = true;
            VisualiseDisabled();

            comboColorTemp.SelectedValueChanged += ComboVisual_SelectedValueChanged;
            comboColorTemp.Visible = true;

            if (gamuts.Count <= 1) return;

            comboGamut.DropDownStyle = ComboBoxStyle.DropDownList;
            comboGamut.DataSource = new BindingSource(gamuts, null);
            comboGamut.DisplayMember = "Value";
            comboGamut.ValueMember = "Key";
            comboGamut.SelectedValue = (SplendidGamut)AppConfig.Get("gamut", (int)VisualControl.GetDefaultGamut());

            comboGamut.SelectedValueChanged += ComboGamut_SelectedValueChanged;
            comboGamut.Visible = true;

        }

        public void CycleVisualMode(int delta)
        {

            if (comboVisual.Items.Count < 1) return;

            if (delta > 0)
            {
                if (comboVisual.SelectedIndex < comboVisual.Items.Count - 1)
                    comboVisual.SelectedIndex += 1;
                else
                    comboVisual.SelectedIndex = 0;
            }
            else
            {
                if (comboVisual.SelectedIndex > 0)
                    comboVisual.SelectedIndex -= 1;
                else
                    comboVisual.SelectedIndex = comboVisual.Items.Count - 1;
            }

            Program.toast.RunToast(comboVisual.GetItemText(comboVisual.SelectedItem), ToastIcon.BrightnessUp);
        }

        private async void ButtonInstallColorProfile_Click(object? sender, EventArgs e)
        {
            await ColorProfileHelper.InstallProfile();
            InitVisual();
        }

        private void ComboGamut_SelectedValueChanged(object? sender, EventArgs e)
        {
            VisualControl.SetGamut((int)comboGamut.SelectedValue);
        }

        private void ComboVisual_SelectedValueChanged(object? sender, EventArgs e)
        {
            VisualControl.SetVisual((SplendidCommand)comboVisual.SelectedValue, (int)comboColorTemp.SelectedValue);
            VisualiseDisabled();
        }

        public void VisualiseBrightness()
        {
            if (InvokeRequired) { Invoke(VisualiseBrightness); return; }
            sliderGammaIgnore = true;
            sliderGamma.Value = VisualControl.GetBrightness();
            labelGamma.Text = sliderGamma.Value + "%";
            sliderGammaIgnore = false;
        }

        public void VisualiseAmdOled(bool status = false)
        {
            if (InvokeRequired) { Invoke(() => VisualiseAmdOled(status)); return; }
            buttonAmdOled.Visible = status;
        }

        public void VisualiseArmoury(bool status = false)
        {
            if (InvokeRequired) { Invoke(() => VisualiseArmoury(status)); return; }
            buttonArmoury.Visible = false; // always hidden on Dell-only
        }

        public void VisualiseDisabled()
        {
            comboGamut.Enabled = comboColorTemp.Enabled = (SplendidCommand)AppConfig.Get("visual") != SplendidCommand.Disabled;
        }

        public void VisualiseGamut()
        {
            if (InvokeRequired) { Invoke(VisualiseGamut); return; }
            if (comboGamut.Items.Count > 0) comboGamut.SelectedIndex = 0;
        }

        private void SliderGamma_ValueChanged(object? sender, EventArgs e)
        {
            if (sliderGammaIgnore) return;
            VisualControl.SetBrightness(sliderGamma.Value);
        }

        private void ButtonOverlay_Click(object? sender, EventArgs e)
        {
            if (overlayForm == null || overlayForm.Text == "")
            {
                overlayForm = new OverlayConfig();
                AddOwnedForm(overlayForm);
            }

            if (overlayForm.Visible)
                overlayForm.Close();
            else
                overlayForm.Show();
        }

        // Ally / Matrix / Peripherals stubs — keep call sites compiling until those folders are removed
        public void VisualiseAlly(bool visible = false) { }

        public void VisualiseController(int mode)
        {
            buttonControllerMode.Text = mode switch
            {
                1 => "Gamepad",
                3 => "Mouse",
                -1 => "Skip",
                _ => "Auto",
            };
        }

        public void VisualiseBacklight(int backlight)
        {
            if (InvokeRequired) { Invoke(() => VisualiseBacklight(backlight)); return; }
            buttonBacklight.Text = Math.Round((double)backlight * 33.33).ToString() + "%";
        }

        public void VisualiseFPSLimit(int limit)
        {
            if (InvokeRequired) { Invoke(() => VisualiseFPSLimit(limit)); return; }
            buttonFPS.Text = "FPS Limit " + ((limit > 0 && limit <= 120) ? limit : "OFF");
        }

        public void VisualiseAutoTDP(bool status)
        {
            Logger.WriteLine($"Auto TDP: {status}");
            buttonAutoTDP.Activated = status;
        }

        private void SettingsForm_LostFocus(object? sender, EventArgs e)
        {
            lastLostFocus = DateTimeOffset.Now.ToUnixTimeMilliseconds();
        }

        private void ButtonBatteryFull_Click(object? sender, EventArgs e)
        {
            BatteryControl.ToggleBatteryLimitFull();
        }

        private void ButtonBatteryFull_MouseLeave(object? sender, EventArgs e)
        {
            batteryFullMouseOver = false;
            RefreshSensors(true);
        }

        private void ButtonBatteryFull_MouseEnter(object? sender, EventArgs e)
        {
            batteryFullMouseOver = true;
            labelCharge.Text = Properties.Strings.BatteryLimitFull;
        }

        private void SettingsForm_Resize(object? sender, EventArgs e)
        {
            if (WindowState != FormWindowState.Normal)
            {
                WindowState = FormWindowState.Normal;
                return;
            }

            var screen = Screen.FromControl(this);
            Left = screen.WorkingArea.Right - 10 - Width;
            Top = screen.WorkingArea.Top > screen.Bounds.Top ? screen.WorkingArea.Top + 10 : screen.WorkingArea.Bottom - 10 - Height;
        }

        private void PanelBattery_MouseEnter(object? sender, EventArgs e)
        {
            batteryMouseOver = true;
            ShowBatteryWear();
        }

        private void PanelBattery_MouseLeave(object? sender, EventArgs e)
        {
            batteryMouseOver = false;
            RefreshSensors(true);
        }

        private async void ShowBatteryWear()
        {
            //Refresh again only after 15 Minutes since the last refresh
            if (lastBatteryRefresh == 0 || Math.Abs(DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastBatteryRefresh) > 15 * 60_000)
            {
                lastBatteryRefresh = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                await Task.Run(HardwareControl.RefreshBatteryHealth);
            }

            if (batteryMouseOver && HardwareControl.batteryHealth != -1)
            {
                labelCharge.Text = Properties.Strings.BatteryHealth + ": " + Math.Round(HardwareControl.batteryHealth, 1) + "%";
            }
        }

        private void SettingsForm_VisibleChanged(object? sender, EventArgs e)
        {
            // Dell: only poll sensors while the window is open (lighter idle tray)
            if (Program.acpi.IsDellPlatform)
            {
                sensorTimer.Interval = this.Visible ? AppConfig.Get("sensor_timer", 2000) : 30000;
                sensorTimer.Enabled = this.Visible;
                if (this.Visible) RefreshSensors(true);
                return;
            }

            sensorTimer.Enabled = this.Visible || sensorsAlways;
            if (this.Visible)
                updateControl.CheckForUpdates();
        }

        private void ButtonUpdates_Click(object? sender, EventArgs e)
        {
            if (updatesForm == null || updatesForm.Text == "")
            {
                updatesForm = new Updates();
                AddOwnedForm(updatesForm);
            }

            if (updatesForm.Visible)
            {
                updatesForm.Close();
            }
            else
            {
                updatesForm.Show();
            }
        }

        public void VisualiseMatrixPicture(string image) { }

        protected override void WndProc(ref Message m)
        {

            if (m.Msg == NativeMethods.WM_POWERBROADCAST && m.WParam == (IntPtr)NativeMethods.PBT_APMSUSPEND)
            {
                Logger.WriteLine("System Suspend");
                GPUModeControl.suspended = true;
                Program.modeControl.SleepReset();
                m.Result = (IntPtr)1;
            }

            if (m.Msg == NativeMethods.WM_POWERBROADCAST && m.WParam == (IntPtr)NativeMethods.PBT_APMRESUMEAUTOMATIC)
            {
                Logger.WriteLine("System Resume");
                GPUModeControl.suspended = false;
                // Dell: SetAutoModes on monitor-on applies battery once (debounced). Avoid a second CCTK here.
                if (!Program.acpi.IsDellPlatform)
                    BatteryControl.AutoBattery();
                m.Result = (IntPtr)1;
            }

            if (m.Msg == NativeMethods.WM_POWERBROADCAST && m.WParam == (IntPtr)NativeMethods.PBT_POWERSETTINGCHANGE)
            {
                var settings = (NativeMethods.POWERBROADCAST_SETTING)m.GetLParam(typeof(NativeMethods.POWERBROADCAST_SETTING));
                if (settings.PowerSetting == NativeMethods.PowerSettingGuid.LIDSWITCH_STATE_CHANGE)
                {
                    switch (settings.Data)
                    {
                        case 0:
                            Logger.WriteLine("Lid Closed");
                            BatteryControl.AutoBattery();
                            InputDispatcher.lidClose = true;
                            break;
                        case 1:
                            Logger.WriteLine("Lid Open");
                            InputDispatcher.InitFNLock();
                            InputDispatcher.lidClose = false;
                            break;
                    }

                }
                else if (settings.PowerSetting == NativeMethods.PowerSettingGuid.EnergySaverStatus)
                {
                    Logger.WriteLine("Battery Saver: " + settings.Data);
                    buttonEnergySaver.Visible = settings.Data != 0;
                }
                else
                {
                    switch (settings.Data)
                    {
                        case 0:
                            Logger.WriteLine("Monitor Power Off");
                            Program.hardwareOverlay?.SuspendForDisplayOff();
                            break;
                        case 1:
                            Logger.WriteLine("Monitor Power On");
                            GPUModeControl.suspended = false;
                            // If SetAutoModes was recently run (debounce), skip fallback AutoBattery on Dell
                            if (!Program.SetAutoModes(wakeup: true) && !Program.acpi.IsDellPlatform)
                                BatteryControl.AutoBattery();
                            Program.hardwareOverlay?.ResumeForDisplayOn();
                            break;
                        case 2:
                            Logger.WriteLine("Monitor Dimmed");
                            break;
                    }
                }
                m.Result = (IntPtr)1;
            }

            if (m.Msg == Program.WM_TASKBARCREATED)
            {
                Logger.WriteLine("Taskbar created, re-creating tray icon");
                if (Program.trayIcon is not null) Program.trayIcon.Visible = true;
            }

            try
            {
                base.WndProc(ref m);
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
            }
        }

        public void SetContextMenu()
        {
            var currentMode = Modes.GetCurrent();

            foreach (ToolStripItem item in contextMenuStrip.Items.Cast<ToolStripItem>().ToList())
            {
                if (item is ToolStripMenuItem menuItem) menuItem.Dispose();
            }
            contextMenuStrip.Items.Clear();
            contextMenuStrip.ShowCheckMargin = true;
            contextMenuStrip.ImageScalingSize = new Size(16, 16);
            contextMenuStrip.ShowImageMargin = false;
            Padding padding = new Padding(5, 5, 5, 5);

            var title = new ToolStripMenuItem(Properties.Strings.PerformanceMode);
            title.Margin = padding;
            title.Enabled = false;
            contextMenuStrip.Items.Add(title);

            foreach (var mode in Modes.GetDictonary())
            {
                var menuMode = new ToolStripMenuItem(mode.Value);
                menuMode.Tag = mode.Key;
                menuMode.Click += (sender, args) => { Program.modeControl.SetPerformanceMode(mode.Key, notify: true); };
                menuMode.Margin = padding;
                menuMode.Checked = (mode.Key == currentMode);
                contextMenuStrip.Items.Add(menuMode);
            }

            contextMenuStrip.Items.Add("-");

            if (isGpuSection)
            {
                var titleGPU = new ToolStripMenuItem(Properties.Strings.GPUMode);
                titleGPU.Margin = padding;
                titleGPU.Enabled = false;
                contextMenuStrip.Items.Add(titleGPU);

                menuEco = new ToolStripMenuItem(Properties.Strings.EcoMode);
                menuEco.Click += ButtonEco_Click;
                menuEco.Margin = padding;
                menuEco.Checked = buttonEco.Activated;
                contextMenuStrip.Items.Add(menuEco);

                menuStandard = new ToolStripMenuItem(Properties.Strings.StandardMode);
                menuStandard.Click += ButtonStandard_Click;
                menuStandard.Margin = padding;
                menuStandard.Checked = buttonStandard.Activated;
                contextMenuStrip.Items.Add(menuStandard);

                menuUltimate = new ToolStripMenuItem(Properties.Strings.UltimateMode);
                menuUltimate.Click += ButtonUltimate_Click;
                menuUltimate.Margin = padding;
                menuUltimate.Checked = buttonUltimate.Activated;
                menuUltimate.Visible = isMuxGpu;
                contextMenuStrip.Items.Add(menuUltimate);

                menuOptimized = new ToolStripMenuItem(Properties.Strings.Optimized);
                menuOptimized.Click += ButtonOptimized_Click;
                menuOptimized.Margin = padding;
                menuOptimized.Checked = buttonOptimized.Activated;
                contextMenuStrip.Items.Add(menuOptimized);

                contextMenuStrip.Items.Add("-");
            }

            var bwIcon = new ToolStripMenuItem(Properties.Strings.BWTrayIcon);
            bwIcon.Margin = padding;
            bwIcon.Checked = AppConfig.IsBWIcon();
            bwIcon.Click += (sender, args) =>
            {
                bwIcon.Checked = !bwIcon.Checked;
                AppConfig.Set("bw_icon", bwIcon.Checked ? 1 : 0);
                VisualiseIcon();
            };
            contextMenuStrip.Items.Add(bwIcon);

            contextMenuStrip.Items.Add("-");

            var menuOverlay = new ToolStripMenuItem(Properties.Strings.Overlay);
            menuOverlay.Click += (sender, args) => ToggleOverlay();
            menuOverlay.Margin = padding;
            menuOverlay.Checked = AppConfig.IsOverlay();
            contextMenuStrip.Items.Add(menuOverlay);

            var menuOverlayGameOnly = new ToolStripMenuItem(Properties.Strings.OverlayOnlyInGames);
            menuOverlayGameOnly.Click += (sender, args) => ToggleOverlayGameOnly();
            menuOverlayGameOnly.Margin = padding;
            menuOverlayGameOnly.Checked = AppConfig.IsOverlayGameOnly();
            menuOverlayGameOnly.Enabled = AppConfig.IsOverlay();
            contextMenuStrip.Items.Add(menuOverlayGameOnly);

            var quit = new ToolStripMenuItem(Properties.Strings.Quit);
            quit.Click += ButtonQuit_Click;
            quit.Margin = padding;
            contextMenuStrip.Items.Add(quit);

            //contextMenuStrip.ShowCheckMargin = true;
            contextMenuStrip.Renderer = new CustomMenuRenderer();

            InitContextMenuTheme();

            if (Program.trayIcon is not null) Program.trayIcon.ContextMenuStrip = contextMenuStrip;


        }

        public void InitContextMenuTheme()
        {
            if (contextMenuStrip is not null)
            {
                contextMenuStrip.BackColor = this.BackColor;
                contextMenuStrip.ForeColor = this.ForeColor;
            }

            donateControl?.ApplyTheme();
        }

        public void SetVersionLabel(string label, bool update = false)
        {
            if (InvokeRequired)
                Invoke(delegate
                {
                    labelVersion.Text = label;
                    if (update) labelVersion.ForeColor = colorTurbo;
                });
            else
            {
                labelVersion.Text = label;
                if (update) labelVersion.ForeColor = colorTurbo;
            }
        }


        private void LabelVersion_Click(object? sender, EventArgs e)
        {
            updateControl.Update();
        }


        private static void OnTimedEvent(Object? source, ElapsedEventArgs? e)
        {
            Program.settingsForm.RefreshSensors();
        }

        private void ButtonFHD_MouseHover(object? sender, EventArgs e)
        {
            labelTipScreen.Text = "Switch to " + ((buttonFHD.Text == "FHD") ? "UHD" : "FHD") + " Mode";
        }

        private void Button120Hz_MouseHover(object? sender, EventArgs e)
        {
            labelTipScreen.Text = Properties.Strings.MaxRefreshTooltip;
        }

        private void Button60Hz_MouseHover(object? sender, EventArgs e)
        {
            labelTipScreen.Text = Properties.Strings.MinRefreshTooltip.Replace("60", ScreenControl.MIN_RATE.ToString());
        }

        private void ButtonScreen_MouseLeave(object? sender, EventArgs e)
        {
            labelTipScreen.Text = "";
        }

        private void ButtonScreenAuto_MouseHover(object? sender, EventArgs e)
        {
            labelTipScreen.Text = Properties.Strings.AutoRefreshTooltip.Replace("60", ScreenControl.MIN_RATE.ToString());
        }

        private void ButtonUltimate_MouseHover(object? sender, EventArgs e)
        {
            labelTipGPU.Text = Properties.Strings.UltimateGPUTooltip;
        }

        private void ButtonStandard_MouseHover(object? sender, EventArgs e)
        {
            labelTipGPU.Text = Properties.Strings.StandardGPUTooltip;
        }

        private void ButtonEco_MouseHover(object? sender, EventArgs e)
        {
            labelTipGPU.Text = Properties.Strings.EcoGPUTooltip;
        }

        private void ButtonOptimized_MouseHover(object? sender, EventArgs e)
        {
            labelTipGPU.Text = Properties.Strings.OptimizedGPUTooltip;
        }

        private void ButtonGPU_MouseLeave(object? sender, EventArgs e)
        {
            labelTipGPU.Text = "";
        }

        private void ButtonScreenAuto_Click(object? sender, EventArgs e)
        {
            ScreenControl.SetAutoRefresh(1);
            ScreenControl.AutoScreen();
        }


        private void CheckStartup_CheckedChanged(object? sender, EventArgs e)
        {
            if (sender is null) return;
            CheckBox chk = (CheckBox)sender;

            if (chk.Checked)
                Startup.Schedule();
            else
                Startup.UnSchedule();
        }

        public void VisualiseMatrixRunning(int mode)
        {
            if (InvokeRequired) { Invoke(() => VisualiseMatrixRunning(mode)); return; }
            if (mode >= 0 && mode < comboMatrixRunning.Items.Count)
                comboMatrixRunning.SelectedIndex = mode;
        }

        public void SetMatrixRunning(int mode)
        {
            VisualiseMatrixRunning(mode);
            AppConfig.Set("matrix_running", mode);
        }

        private void LabelCPUFan_Click(object? sender, EventArgs e)
        {
            FanSensorControl.fanRpm = !FanSensorControl.fanRpm;
            RefreshSensors(true);
        }

        private void ButtonKeyboardColor2_Click(object? sender, EventArgs e)
        {
            // Aura color picker removed (Dell-only)
        }

        private void ButtonKeyboard_Click(object? sender, EventArgs e)
        {
            if (extraForm == null || extraForm.IsDisposed)
            {
                extraForm = new Extra();
                AddOwnedForm(extraForm);
                extraForm.FormClosed += (_, _) => extraForm = null;
            }

            if (extraForm.Visible)
            {
                extraForm.Hide();
            }
            else
            {
                extraForm.Show();
                extraForm.BringToFront();
            }
        }

        public void FansInit()
        {
            if (fansForm == null || fansForm.Text == "") return;
            Invoke(fansForm.InitAll);
        }

        public void GPUInit()
        {
            if (fansForm == null || fansForm.Text == "") return;
            Invoke(fansForm.InitGPU);
        }

        public void FansToggle(int index = 0)
        {
            if (fansForm == null || fansForm.Text == "")
            {
                fansForm = new Fans();
                AddOwnedForm(fansForm);
            }

            if (fansForm.Visible)
            {
                fansForm.Close();
            }
            else
            {
                fansForm.FormPosition();
                fansForm.Show();
                fansForm.ToggleNavigation(index);
            }

        }

        private void ButtonFans_Click(object? sender, EventArgs e)
        {
            FansToggle();
        }

        private void ButtonKeyboardColor_Click(object? sender, EventArgs e)
        {
            // Aura color picker removed (Dell-only)
        }

        public void InitRearLight() { }

        /// <summary>Hide ASUS-only panels on Dell Pro; keep Extra / Fn-Lock / Overlay / screen / battery.</summary>
        public void InitDellPlatform()
        {
            Text = "D-Helper";
            ShowIcon = true;
            ShowInTaskbar = true;
            IconHelper.ApplyFormIcon(this, Properties.Resources.standard);
            isGpuSection = false;
            panelGPU.Visible = false;
            panelMatrix.Visible = false;
            panelAlly.Visible = false;
            panelRearLight.Visible = false;
            panelPeripherals.Visible = false;
            panelGamma.Visible = false;
            buttonArmoury.Visible = false;
            buttonDonate.Visible = false;

            // Keep panelKeyboard — it hosts Extra, Fn-Lock, Overlay. Hide Aura-only chrome.
            panelKeyboard.Visible = true;
            comboKeyboard.Visible = false;
            buttonKeyboardColor.Visible = false;
            labelBacklight.Visible = false;
            labelKeyboard.Text = "Settings";
            buttonKeyboard.Text = Properties.Strings.Extra;

            // Dell names: Silent → Quiet; add Cool as an extra pill (keep Quiet/Balanced/Turbo/Fans)
            buttonSilent.Text = "Quiet";
            buttonSilent.AccessibleName = "Quiet";
            InitDellCoolPill();
            InitDellModeTooltips();

            // Sync battery slider/label from config (designer default is often 100%)
            int limit = AppConfig.Get("charge_limit", 80);
            if (limit < 40 || limit > 100) limit = 80;
            VisualiseBattery(limit);

            // Warm Extra CCTK reads in background so Extra opens with real values, not stuck Loading
            Program.acpi.DellHal?.PrefetchExtraSettingsAsync();

            // Ensure screen refresh controls stay available on Dell
            panelScreen.Visible = true;
            tableScreen.Visible = true;
            buttonFHD.Visible = false;
            buttonMiniled.Visible = false;

            // Lighter idle: don't keep sensors running while tray-only
            if (sensorTimer != null)
            {
                sensorTimer.Interval = 2000;
                sensorTimer.Enabled = Visible;
            }

            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
            UpdateStyles();

            SetContextMenu();
            VisualiseIcon(true);
            Logger.WriteLine("Dell platform UI: Extra/FnLock/Overlay kept; Aura/GPU/Matrix hidden; Cool pill added");

            // Hide Fn-Lock when this BIOS does not expose CCTK FnLock
            Task.Run(() =>
            {
                bool? supported = Program.acpi.DellHal?.GetFnLock();
                if (IsDisposed) return;
                BeginInvoke(() =>
                {
                    if (IsDisposed) return;
                    if (supported == null)
                    {
                        buttonFnLock.Visible = false;
                        buttonOverlay.Width = buttonKeyboard.Width;
                        buttonOverlay.Left = buttonKeyboard.Left;
                        Logger.WriteLine("Dell FnLock: not supported on this BIOS — button hidden");
                    }
                    else
                    {
                        buttonFnLock.Visible = true;
                        AlignFnLock();
                        // Sync config from BIOS once so UI matches reality
                        AppConfig.Set("fn_lock", supported.Value ? 1 : 0);
                        VisualiseFnLock();
                    }
                });
            });
        }

        private void InitDellCoolPill()
        {
            if (buttonCool != null) return;

            buttonCool = new RButton
            {
                Activated = false,
                BackColor = buttonBalanced.BackColor,
                BorderColor = Color.DodgerBlue,
                BorderRadius = 5,
                Dock = DockStyle.Fill,
                FlatStyle = FlatStyle.Flat,
                ForeColor = buttonBalanced.ForeColor,
                Image = buttonSilent.Image,
                ImageAlign = ContentAlignment.BottomCenter,
                Margin = new Padding(4),
                Name = "buttonCool",
                Secondary = false,
                Text = "Cool",
                AccessibleName = "Cool",
                TextImageRelation = TextImageRelation.ImageAboveText,
                UseVisualStyleBackColor = false
            };
            buttonCool.FlatAppearance.BorderSize = 0;
            buttonCool.Click += ButtonCool_Click;

            // Quiet | Balanced | Cool | Turbo | Fans
            tablePerf.SuspendLayout();
            tablePerf.Controls.Clear();
            tablePerf.ColumnCount = 5;
            tablePerf.ColumnStyles.Clear();
            for (int i = 0; i < 5; i++)
                tablePerf.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
            tablePerf.Controls.Add(buttonSilent, 0, 0);
            tablePerf.Controls.Add(buttonBalanced, 1, 0);
            tablePerf.Controls.Add(buttonCool, 2, 0);
            tablePerf.Controls.Add(buttonTurbo, 3, 0);
            tablePerf.Controls.Add(buttonFans, 4, 0);
            tablePerf.ResumeLayout(true);
        }

        private void InitDellModeTooltips()
        {
            const string quietTip =
                "Quiet — Dell Quiet thermal: lower fan noise and power. Best for office work and battery life.";
            const string balancedTip =
                "Balanced — Dell Optimized: everyday mix of performance and noise (default).";
            const string coolTip =
                "Cool — Dell Cool thermal: prioritizes lower temperatures with more aggressive cooling.";
            const string turboTip =
                "Turbo — Dell Ultra Performance: maximum CPU power for demanding workloads (louder/hotter).";

            toolTip.SetToolTip(buttonSilent, quietTip);
            toolTip.SetToolTip(buttonBalanced, balancedTip);
            if (buttonCool != null) toolTip.SetToolTip(buttonCool, coolTip);
            toolTip.SetToolTip(buttonTurbo, turboTip);
            toolTip.SetToolTip(buttonFans, "Fans & power — open detailed fan/power settings for the current mode.");
        }

        private void ButtonCool_Click(object? sender, EventArgs e)
        {
            int coolId = Modes.FindByBase(AsusACPI.PerformanceDellCool);
            if (coolId < 0)
            {
                Modes.InitDellCool();
                coolId = Modes.FindByBase(AsusACPI.PerformanceDellCool);
            }
            if (coolId >= 0)
                Program.modeControl.SetPerformanceMode(coolId, notify: true);
        }

        public void InitAura() { }

        public void SetAura() { }

        public void VisualiseAura() { }

        public void InitMatrix()
        {
            panelMatrix.Visible = false;
        }

        public void CycleMatrix(int delta) { }

        public void CycleAuraMode(int delta) { }

        private void Button120Hz_Click(object? sender, EventArgs e)
        {
            ScreenControl.SetAutoRefresh(0);
            ScreenControl.SetScreen(ScreenControl.MAX_REFRESH, 1);
        }

        private void Button60Hz_Click(object? sender, EventArgs e)
        {
            ScreenControl.SetAutoRefresh(0);
            ScreenControl.SetScreen(ScreenControl.MIN_RATE, 0);
        }


        private void ButtonMiniled_Click(object? sender, EventArgs e)
        {
            ScreenControl.ToogleMiniled();
        }



        public void VisualiseScreen(bool screenEnabled, bool screenAuto, int frequency, int maxFrequency, int overdrive, bool overdriveSetting, int miniled1, int miniled2, bool hdr, bool acm, int fhd, int hdrControl)
        {
            bool advancedColor = hdr || acm;

            ButtonEnabled(button60Hz, screenEnabled);
            ButtonEnabled(button120Hz, screenEnabled);
            ButtonEnabled(buttonScreenAuto, screenEnabled);
            ButtonEnabled(buttonMiniled, screenEnabled);

            labelSreen.Text = screenEnabled
                ? Properties.Strings.LaptopScreen + ": " + frequency + "Hz" + ((overdrive == 1) ? " + " + Properties.Strings.Overdrive : "")
                : Properties.Strings.LaptopScreen + ": " + Properties.Strings.TurnedOff;

            panelScreen.AccessibleName = labelSreen.Text;

            button60Hz.Activated = false;
            button120Hz.Activated = false;
            buttonScreenAuto.Activated = false;

            if (screenAuto)
            {
                buttonScreenAuto.Activated = true;
            }
            else if (frequency == ScreenControl.MIN_RATE)
            {
                button60Hz.Activated = true;
            }
            else if (frequency > ScreenControl.MIN_RATE)
            {
                button120Hz.Activated = true;
            }

            button60Hz.Text = ScreenControl.MIN_RATE + "Hz";

            if (maxFrequency > ScreenControl.MIN_RATE)
            {
                button120Hz.Text = maxFrequency.ToString() + "Hz" + (overdriveSetting ? " + OD" : "");
                panelScreen.Visible = true;
                tableScreen.Visible = true;
            }
            else if (maxFrequency > 0)
            {
                tableScreen.Visible = false;
                panelScreen.Visible = AppConfig.NoGpu();
            }

            if (fhd >= 0)
            {
                buttonFHD.Visible = true;
                buttonFHD.Text = fhd > 0 ? "FHD" : "UHD";
            }

            bool hdrControlVisible = (hdr && hdrControl >= 0);

            if (miniled1 >= 0)
            {
                buttonMiniled.Visible = !hdrControlVisible;
                buttonMiniled.Enabled = !hdr;
                buttonMiniled.Activated = miniled1 == 1 || hdr;
            }
            else if (miniled2 >= 0)
            {
                buttonMiniled.Visible = !hdrControlVisible;
                buttonMiniled.Enabled = !hdr;
                if (hdr) miniled2 = 1; // Show HDR as Multizone Strong

                switch (miniled2)
                {
                    // Multizone On
                    case 0:
                        buttonMiniled.Text = Properties.Strings.Multizone;
                        buttonMiniled.BorderColor = colorStandard;
                        buttonMiniled.Activated = true;
                        break;
                    // Multizone Strong
                    case 1:
                        buttonMiniled.Text = Properties.Strings.MultizoneStrong;
                        buttonMiniled.BorderColor = colorTurbo;
                        buttonMiniled.Activated = true;
                        break;
                    // Multizone Off
                    case 2:
                        buttonMiniled.Text = Properties.Strings.OneZone;
                        buttonMiniled.BorderColor = colorStandard;
                        buttonMiniled.Activated = false;
                        break;
                }
            }
            else
            {
                buttonMiniled.Visible = false;
            }

            if (hdrControlVisible)
            {
                buttonHDRControl.Visible = true;
                buttonHDRControl.Activated = hdrControl > 0;
                buttonHDRControl.BorderColor = colorTurbo;
            } else
            {
                buttonHDRControl.Visible = false;
            }

            if (advancedColor) labelVisual.Text = Properties.Strings.VisualModesHDR;
            if (!screenEnabled) labelVisual.Text = Properties.Strings.VisualModesScreen;

            if (!screenEnabled || advancedColor)
            {
                labelVisual.Location = tableVisual.Location;
                labelVisual.Width = tableVisual.Width;
                labelVisual.Height = tableVisual.Height;
                labelVisual.Visible = true;
            }
            else
            {
                labelVisual.Visible = false;
            }


        }

        private void ButtonQuit_Click(object? sender, EventArgs e)
        {
            Close();
            Program.trayIcon.Visible = false;
            Application.Exit();
        }

        /// <summary>
        /// Closes all forms except the settings. Hides the settings
        /// </summary>
        public void HideAll()
        {
            this.Hide();
            if (fansForm != null && fansForm.Text != "") fansForm.Close();
            if (extraForm != null && extraForm.Text != "") extraForm.Close();
            if (updatesForm != null && updatesForm.Text != "") updatesForm.Close();
            if (overlayForm != null && overlayForm.Text != "") overlayForm.Close();
            MemoryHelper.TrimAfter();
        }

        /// <summary>
        /// Brings all visible windows to the top, with settings being the focus
        /// </summary>
        public void ShowAll()
        {
            this.Activate();
            this.TopMost = true;
            this.TopMost = AppConfig.Is("topmost");
            if (TopMost) User32.SetWindowPos(Handle, -1, 0, 0, 0, 0, 0x0001 | 0x0002 | 0x0010);
        }

        public DialogResult ShowMessage(string text, string title = "", MessageBoxButtons buttons = MessageBoxButtons.OK)
        {
            DialogResult result = DialogResult.None;
            Invoke((MethodInvoker)delegate
            {
                result = MessageBox.Show(this, text, title, buttons);
            });
            return result;
        }

        /// <summary>
        /// Check if any of fans, keyboard, update, or itself has focus
        /// </summary>
        /// <returns>Focus state</returns>
        public bool HasAnyFocus(bool lostFocusCheck = false)
        {
            return (fansForm != null && fansForm.ContainsFocus) ||
                   (extraForm != null && extraForm.ContainsFocus) ||
                   (updatesForm != null && updatesForm.ContainsFocus) ||
                   (overlayForm != null && overlayForm.ContainsFocus) ||
                   this.ContainsFocus ||
                   (lostFocusCheck && Math.Abs(DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastLostFocus) < 300);
        }

        private void SettingsForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                HideAll();
            }
        }

        private void ButtonUltimate_Click(object? sender, EventArgs e)
        {
            gpuControl.SetGPUMode(AsusACPI.GPUModeUltimate);
        }

        private void ButtonStandard_Click(object? sender, EventArgs e)
        {
            gpuControl.SetGPUMode(AsusACPI.GPUModeStandard);
        }

        private void ButtonEco_Click(object? sender, EventArgs e)
        {
            gpuControl.SetGPUMode(AsusACPI.GPUModeEco);
        }


        private void ButtonOptimized_Click(object? sender, EventArgs e)
        {
            AppConfig.Set("gpu_auto", (AppConfig.Get("gpu_auto") == 1) ? 0 : 1);
            VisualiseGPUMode();
            gpuControl.AutoGPUMode(true);
        }

        private void ButtonStopGPU_Click(object? sender, EventArgs e)
        {
            gpuControl.KillGPUApps();
        }

        public async void RefreshSensors(bool force = false)
        {
            int throttle = (!Visible && sensorsAlways) ? 6000 : 2000;
            if (!force && Math.Abs(DateTimeOffset.Now.ToUnixTimeMilliseconds() - lastRefresh) < throttle) return;
            lastRefresh = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            string cpuTemp = "";
            string gpuTemp = "";

            string cpuFan = "";
            string gpuFan = "";
            string midFan = "";

            string battery = "";
            string charge = "";

            await Task.Run(() => HardwareControl.ReadSensors());

            if (HardwareControl.cpuTemp > 0)
                cpuTemp = ": " + TempHelper.FormatTemp((double)HardwareControl.cpuTemp);

            if (HardwareControl.batteryCapacity > 0)
            {
                charge = Properties.Strings.BatteryCharge + ": " + HardwareControl.batteryCharge;
            }

            if (HardwareControl.batteryRate < 0)
                battery = Properties.Strings.Discharging + ": " + Math.Round(-(decimal)HardwareControl.batteryRate, 1).ToString() + "W";
            else if (HardwareControl.batteryRate > 0)
                battery = Properties.Strings.Charging + ": " + Math.Round((decimal)HardwareControl.batteryRate, 1).ToString() + "W";


            if (HardwareControl.gpuTemp > 0)
            {
                gpuTemp = ": " + TempHelper.FormatTemp((double)HardwareControl.gpuTemp);
            }

            if (HardwareControl.cpuFan is not null) cpuFan = Strings.FanSpeed + ": " + HardwareControl.cpuFan;
            if (HardwareControl.gpuFan is not null) gpuFan = Strings.FanSpeed + ": " + HardwareControl.gpuFan;
            if (HardwareControl.midFan is not null) midFan = Strings.FanSpeed + ": " + HardwareControl.midFan;

            string trayTip = "CPU" + cpuTemp + " " + cpuFan;
            if (gpuTemp.Length > 0) trayTip += "\nGPU" + gpuTemp + " " + gpuFan;
            if (battery.Length > 0) trayTip += "\n" + battery;
            
            if (Program.settingsForm.IsHandleCreated)
                Program.settingsForm.BeginInvoke(delegate
                {
                    labelCPUFan.Text = "CPU" + cpuTemp + "  " + cpuFan;
                    labelGPUFan.Text = "GPU" + gpuTemp + "  " + gpuFan;

                    if (HardwareControl.gpuFan is not null && AppConfig.NoGpu())
                        labelMidFan.Text = "GPU" + gpuTemp + " " + gpuFan;

                    if (HardwareControl.midFan is not null) 
                        labelMidFan.Text = "Mid " + midFan;
                    
                    labelBattery.Text = battery;
                    if (!batteryMouseOver && !batteryFullMouseOver) labelCharge.Text = charge;
                });

            if (Program.trayIcon is not null) Program.trayIcon.Text = trayTip;
        }

        public void LabelFansResult(string text)
        {
            if (fansForm != null && !fansForm.IsDisposed && fansForm.Text != "")
                fansForm.LabelFansResult(text);
        }

        public void ToggleOverlay(bool fromHotkey = false)
        {
            bool enable = !AppConfig.IsOverlay();
            AppConfig.Set("overlay", enable ? 1 : 0);
            Logger.WriteLine("Overlay " + (enable ? "On" : "Off") + (AppConfig.IsOverlayGameOnly() ? " (game only)" : ""));
            if (enable)
                Program.hardwareOverlay?.StartOverlay();
            else
                Program.hardwareOverlay?.StopOverlay();

            VisualiseOverlay();

            if (fromHotkey && AppConfig.IsOverlayGameOnly())
                Program.toast.RunToast(Properties.Strings.Overlay + " " + (enable ? Properties.Strings.On : Properties.Strings.Off));

            SetContextMenu();
        }

        public void ToggleOverlayGameOnly()
        {
            AppConfig.Set("overlay_game_only", AppConfig.IsOverlayGameOnly() ? 0 : 1);
            if (AppConfig.IsOverlay())
            {
                Program.hardwareOverlay?.StopOverlay();
                Program.hardwareOverlay?.StartOverlay();
            }
            SetContextMenu();
        }

        public void ShowMode(int mode)
        {
            if (InvokeRequired)
                Invoke(delegate
                {
                    VisualiseMode(mode);
                });
            else
                VisualiseMode(mode);
        }

        protected void VisualiseMode(int mode)
        {
            buttonSilent.Activated = false;
            buttonBalanced.Activated = false;
            buttonTurbo.Activated = false;
            buttonFans.Activated = false;
            if (buttonCool != null) buttonCool.Activated = false;

            int modeBase = Modes.GetBase(mode);
            if (buttonCool != null && modeBase == AsusACPI.PerformanceDellCool)
            {
                buttonCool.Activated = true;
            }
            else switch (mode)
            {
                case AsusACPI.PerformanceSilent:
                    buttonSilent.Activated = true;
                    break;
                case AsusACPI.PerformanceTurbo:
                    buttonTurbo.Activated = true;
                    break;
                case AsusACPI.PerformanceBalanced:
                    buttonBalanced.Activated = true;
                    break;
                default:
                    buttonFans.Activated = true;
                    buttonFans.BorderColor = modeBase switch
                    {
                        AsusACPI.PerformanceSilent => colorEco,
                        AsusACPI.PerformanceTurbo => colorTurbo,
                        AsusACPI.PerformanceFullSpeed => Color.Orange,
                        AsusACPI.PerformanceDellCool => Color.DodgerBlue,
                        _ => colorStandard,
                    };
                    break;
            }

            foreach (var item in contextMenuStrip.Items)
            {
                if (item is ToolStripMenuItem menuItem && menuItem.Tag is not null)
                {
                    menuItem.Checked = ((int)menuItem.Tag == mode);
                }
            }
        }


        public void SetModeLabel(string modeText)
        {
            if (InvokeRequired)
            {
                Invoke(delegate
                {
                    labelPerf.Text = modeText;
                    panelPerformance.AccessibleName = labelPerf.Text;
                });
            }
            else
            {
                labelPerf.Text = modeText;
                panelPerformance.AccessibleName = labelPerf.Text;
            }

        }



        public void VisualizeXGM(int GPUMode = -1)
        {
            // XG Mobile UI removed (Dell-only); keep overload for GPUModeControl call sites
            if (InvokeRequired) { Invoke(() => VisualizeXGM(GPUMode)); return; }
            buttonXGM.Visible = false;
            buttonXGM.Enabled = false;
        }

        void VisualizeXGM(bool connected, int activated, int GPUMode)
        {
            buttonXGM.Visible = false;
            buttonXGM.Enabled = false;
        }

        public void VisualiseGPUButtons(bool eco = true, bool ultimate = true)
        {
            if (InvokeRequired) { Invoke(() => VisualiseGPUButtons(eco, ultimate)); return; }
            isMuxGpu = ultimate;

            if (!eco)
            {
                menuEco.Visible = buttonEco.Visible = false;
                menuOptimized.Visible = buttonOptimized.Visible = false;
                buttonStopGPU.Visible = true;
                tableGPU.ColumnCount = 3;
                tableScreen.ColumnCount = 3;
            }
            else
            {
                buttonStopGPU.Visible = false;
            }

            if (!ultimate)
            {
                menuUltimate.Visible = buttonUltimate.Visible = false;
                tableGPU.ColumnCount = 3;
                tableScreen.ColumnCount = 3;
            }
        }

        public void HideGPUModes(bool gpuExists)
        {
            isGpuSection = false;

            buttonEco.Visible = false;
            buttonStandard.Visible = false;
            buttonUltimate.Visible = false;
            buttonOptimized.Visible = false;
            buttonStopGPU.Visible = true;

            tableGPU.ColumnCount = 0;

            SetContextMenu();

            panelGPU.Visible = gpuExists;

        }


        public void LockGPUModes(string text = null)
        {
            if (InvokeRequired) { Invoke(() => LockGPUModes(text)); return; }
            if (text is null) text = Properties.Strings.GPUMode + ": " + Properties.Strings.GPUChanging + " ...";

            ButtonEnabled(buttonOptimized, false);
            ButtonEnabled(buttonEco, false);
            ButtonEnabled(buttonStandard, false);
            ButtonEnabled(buttonUltimate, false);
            ButtonEnabled(buttonXGM, false);

            labelGPU.Text = text;
        }

        public void VisualiseGPUMode(int GPUMode = -1)
        {
            if (InvokeRequired) { Invoke(() => VisualiseGPUMode(GPUMode)); return; }

            if (toolTip.GetToolTip(pictureGPU) != (GPUModeControl.gpuError ?? ""))
            {
                pictureGPU.BackgroundImage = GPUModeControl.gpuError is null ? Properties.Resources.icons8_video_card_32 : SystemIcons.Warning.ToBitmap();
                pictureGPU.Cursor = GPUModeControl.gpuError is null ? Cursors.Default : Cursors.Hand;
                toolTip.SetToolTip(pictureGPU, GPUModeControl.gpuError);
            }

            ButtonEnabled(buttonOptimized, true);
            ButtonEnabled(buttonEco, true);
            ButtonEnabled(buttonStandard, true);
            ButtonEnabled(buttonUltimate, true);

            if (GPUMode == -1)
                GPUMode = AppConfig.Get("gpu_mode");

            bool GPUAuto = AppConfig.Is("gpu_auto");

            buttonEco.Activated = false;
            buttonStandard.Activated = false;
            buttonUltimate.Activated = false;
            buttonOptimized.Activated = false;

            switch (GPUMode)
            {
                case AsusACPI.GPUModeEco:
                    buttonOptimized.BorderColor = colorEco;
                    buttonEco.Activated = !GPUAuto;
                    buttonOptimized.Activated = GPUAuto;
                    labelGPU.Text = Properties.Strings.GPUMode + ": " + Properties.Strings.GPUModeEco;
                    panelGPU.AccessibleName = Properties.Strings.GPUMode + " - " + (GPUAuto ? Properties.Strings.Optimized : Properties.Strings.EcoMode);
                    break;
                case AsusACPI.GPUModeUltimate:
                    buttonUltimate.Activated = true;
                    labelGPU.Text = Properties.Strings.GPUMode + ": " + Properties.Strings.GPUModeUltimate;
                    panelGPU.AccessibleName = Properties.Strings.GPUMode + " - " + Properties.Strings.UltimateMode;
                    break;
                default:
                    buttonOptimized.BorderColor = colorStandard;
                    buttonStandard.Activated = !GPUAuto;
                    buttonOptimized.Activated = GPUAuto;
                    labelGPU.Text = Properties.Strings.GPUMode + ": " + (AppConfig.IsAlwaysUltimate() ? Properties.Strings.GPUModeUltimate : Properties.Strings.GPUModeStandard);
                    panelGPU.AccessibleName = Properties.Strings.GPUMode + " - " + (GPUAuto ? Properties.Strings.Optimized : Properties.Strings.StandardMode);
                    break;
            }

            VisualiseIcon();
            VisualizeXGM(GPUMode);

            if (isGpuSection)
            {
                menuEco.Checked = buttonEco.Activated;
                menuStandard.Checked = buttonStandard.Activated;
                menuUltimate.Checked = buttonUltimate.Activated;
                menuOptimized.Checked = buttonOptimized.Activated;
            }

            // UI Fix for small screeens
            if (Top < 0)
            {
                labelTipGPU.Visible = false;
                labelTipScreen.Visible = false;
                Top = 5;
            }

        }


        private (int, bool, bool)? lastIcon;
        private bool isDark = CheckSystemDarkModeStatus();

        public void VisualiseIcon(bool themeChange = false)
        {
            if (Program.trayIcon is null) return;
            if (themeChange) isDark = CheckSystemDarkModeStatus();

            int GPUMode = Program.acpi.IsDellPlatform ? AsusACPI.GPUModeStandard : AppConfig.Get("gpu_mode");
            bool bw = AppConfig.IsBWIcon();

            if (lastIcon == (GPUMode, isDark, bw)) return;
            lastIcon = (GPUMode, isDark, bw);

            // Always clone — Resources.* icons are shared; disposing them blanks Form/taskbar icons.
            Icon source = Program.acpi.IsDellPlatform
                ? (bw ? (isDark ? Properties.Resources.light_standard : Properties.Resources.dark_standard) : Properties.Resources.standard)
                : GPUMode switch
                {
                    AsusACPI.GPUModeEco => bw ? (isDark ? Properties.Resources.light_eco : Properties.Resources.dark_eco) : Properties.Resources.eco,
                    AsusACPI.GPUModeUltimate => bw ? (isDark ? Properties.Resources.light_standard : Properties.Resources.dark_standard) : Properties.Resources.ultimate,
                    _ => bw ? (isDark ? Properties.Resources.light_standard : Properties.Resources.dark_standard) : Properties.Resources.standard,
                };

            // Independent copy for tray — never dispose previous (WinForms may still reference GDI handles)
            Program.trayIcon.Icon = IconHelper.CloneIcon(source);

            // Keep window taskbar icon in sync
            if (Program.acpi.IsDellPlatform)
                IconHelper.ApplyFormIcon(this, Properties.Resources.standard);
        }

        private void PictureGPU_Click(object? sender, EventArgs e)
        {
            if (GPUModeControl.gpuError is null) return;
            GPUModeControl.CheckGpuError();
            Process.Start(new ProcessStartInfo("devmgmt.msc") { UseShellExecute = true });
        }

        private void ButtonSilent_Click(object? sender, EventArgs e)
        {
            Program.modeControl.SetPerformanceMode(AsusACPI.PerformanceSilent, notify: true);
        }

        private void ButtonBalanced_Click(object? sender, EventArgs e)
        {
            Program.modeControl.SetPerformanceMode(AsusACPI.PerformanceBalanced, notify: true);
        }

        private void ButtonTurbo_Click(object? sender, EventArgs e)
        {
            Program.modeControl.SetPerformanceMode(AsusACPI.PerformanceTurbo, notify: true);
        }


        public void ButtonEnabled(RButton but, bool enabled)
        {
            but.Enabled = enabled;
            but.BackColor = but.Enabled ? Color.FromArgb(255, but.BackColor) : Color.FromArgb(100, but.BackColor);
        }

        public void VisualiseBatteryTitle(int limit)
        {
            labelBatteryTitle.Text = Properties.Strings.BatteryChargeLimit + ": " + limit.ToString() + "%";
        }

        public void VisualiseBattery(int limit)
        {
            if (InvokeRequired) { BeginInvoke(() => VisualiseBattery(limit)); return; }
            limit = Math.Clamp(limit, 40, 100);
            VisualiseBatteryTitle(limit);
            if (sliderBattery.Value != limit)
                sliderBattery.Value = limit;

            sliderBattery.AccessibleName = Properties.Strings.BatteryChargeLimit + ": " + limit.ToString() + "%";
            //sliderBattery.AccessibilityObject.Select(AccessibleSelection.TakeFocus);

            VisualiseBatteryFull();
        }

        public void VisualiseBatteryFull()
        {
            if (InvokeRequired) { Invoke(VisualiseBatteryFull); return; }
            if (BatteryControl.chargeFull)
            {
                buttonBatteryFull.BackColor = colorStandard;
                buttonBatteryFull.ForeColor = SystemColors.ControlLightLight;
                buttonBatteryFull.AccessibleName = Properties.Strings.BatteryChargeLimit + "100% on";
            }
            else
            {
                buttonBatteryFull.BackColor = buttonSecond;
                buttonBatteryFull.ForeColor = SystemColors.ControlDark;
                buttonBatteryFull.AccessibleName = Properties.Strings.BatteryChargeLimit + "100% off";
            }

        }


        public void UpdateKeyboardLabel()
        {
            labelKeyboard.Text = Properties.Strings.LaptopKeyboard;
        }

        public void VisualizePeripherals()
        {
            panelPeripherals.Visible = false;
        }

        public void VisualiseAudio(double level) { }

        private void AlignFnLock()
        {
            buttonFnLock.Width = buttonOverlay.Width = (buttonKeyboard.Width - 8) / 2;
            buttonOverlay.Left = buttonFnLock.Left - 8 - buttonOverlay.Width;
        }

        public void VisualiseOverlay()
        {
            bool enabled = AppConfig.IsOverlay();
            buttonOverlay.BackColor = enabled ? colorEco : buttonSecond;
            buttonOverlay.ForeColor = enabled ? SystemColors.ControlLightLight : SystemColors.ControlDark;
            buttonOverlayAlly.Activated = enabled;
        }

        public void VisualiseFnLock()
        {

            if (AppConfig.Is("fn_lock"))
            {
                buttonFnLock.BackColor = colorStandard;
                buttonFnLock.ForeColor = SystemColors.ControlLightLight;
                buttonFnLock.AccessibleName = "Fn-Lock on";
            }
            else
            {
                buttonFnLock.BackColor = buttonSecond;
                buttonFnLock.ForeColor = SystemColors.ControlDark;
                buttonFnLock.AccessibleName = "Fn-Lock off";
            }
        }


        private void ButtonFnLock_Click(object? sender, EventArgs e)
        {
            InputDispatcher.ToggleFnLock();
        }

    }


}
