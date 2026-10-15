using GHelper.Dell;
using GHelper.Helpers;
using GHelper.UI;

namespace GHelper
{
    public partial class Extra
    {
        private bool _dellLoading;
        private int _dellLoadGen;
        private Label? _dellStatus;
        private Panel? _dellHeader;
        private Panel? _dellBody;
        private RComboBox? _dellIllum;
        private RComboBox? _dellCharge;
        private RComboBox? _dellWol;
        private CheckBox? _dellUsb, _dellLid, _dellCam, _dellAutoOn, _dellBlock;
        private Button? _dellAdminBtn;
        private Label? _dellLabIllum, _dellLabCharge, _dellLabWol;

        private RComboBox MakeDellCombo(params string[] items)
        {
            var c = new RComboBox
            {
                DropDownStyle = ComboBoxStyle.DropDownList,
                Dock = DockStyle.Top,
                Height = 42,
                NativeHeight = true,
                Enabled = true,
                BackColor = buttonMain,
                ForeColor = foreMain,
                BorderColor = borderMain,
                ButtonColor = buttonMain,
                ArrowColor = foreMain,
                Margin = new Padding(5, 2, 5, 6)
            };
            c.Items.AddRange(items);
            if (c.Items.Count > 0) c.SelectedIndex = 0;
            return c;
        }

        private CheckBox MakeDellCheck(string text)
        {
            return new CheckBox
            {
                AutoSize = true,
                Text = text,
                Dock = DockStyle.Top,
                Enabled = true,
                ForeColor = foreMain,
                BackColor = formBack,
                FlatStyle = FlatStyle.Standard,
                Padding = new Padding(3),
                Margin = new Padding(5, 3, 5, 3)
            };
        }

        private Label MakeDellLabel(string text)
        {
            return new Label
            {
                Text = text,
                AutoSize = true,
                Dock = DockStyle.Top,
                ForeColor = foreMain,
                BackColor = formBack,
                Padding = new Padding(3, 10, 3, 0),
                Margin = new Padding(5, 4, 5, 0)
            };
        }

        private void ThemeDellControls()
        {
            if (_dellHeader != null)
            {
                _dellHeader.BackColor = buttonSecond;
                foreach (Control c in _dellHeader.Controls)
                {
                    c.ForeColor = foreMain;
                    c.BackColor = buttonSecond;
                }
            }
            if (_dellBody != null) _dellBody.BackColor = formBack;

            foreach (var lab in new[] { _dellLabIllum, _dellLabCharge, _dellLabWol })
            {
                if (lab == null) continue;
                lab.ForeColor = foreMain;
                lab.BackColor = formBack;
            }
            foreach (var combo in new[] { _dellIllum, _dellCharge, _dellWol })
            {
                if (combo == null) continue;
                combo.BackColor = buttonMain;
                combo.ForeColor = foreMain;
                combo.BorderColor = borderMain;
                combo.ButtonColor = buttonMain;
                combo.ArrowColor = foreMain;
            }
            foreach (var cb in new[] { _dellUsb, _dellLid, _dellCam, _dellAutoOn, _dellBlock })
            {
                if (cb == null) continue;
                cb.ForeColor = foreMain;
                cb.BackColor = formBack;
            }
            if (_dellStatus != null)
            {
                _dellStatus.BackColor = formBack;
                if (!_dellStatus.Text.StartsWith("Error", StringComparison.OrdinalIgnoreCase) &&
                    !_dellStatus.Text.StartsWith("Run D-Helper", StringComparison.OrdinalIgnoreCase) &&
                    !_dellStatus.Text.StartsWith("Could not", StringComparison.OrdinalIgnoreCase) &&
                    !_dellStatus.Text.StartsWith("Timed out", StringComparison.OrdinalIgnoreCase))
                    _dellStatus.ForeColor = Color.FromArgb(200, foreMain);
            }
            if (_dellAdminBtn != null)
            {
                _dellAdminBtn.BackColor = buttonMain;
                _dellAdminBtn.ForeColor = foreMain;
            }
        }

        /// <summary>
        /// Dell CCTK panel — hosted like Extra "Other" (Dock=Top), with a fixed body height
        /// so AutoSize does not collapse docked rows to zero.
        /// </summary>
        private void InitDellCctkExtras()
        {
            var hal = Program.acpi.DellHal;

            _dellHeader = new Panel
            {
                Name = "panelDellHeader",
                AutoSize = true,
                Dock = DockStyle.Top,
                BackColor = buttonSecond,
                Padding = new Padding(11, 8, 11, 8)
            };
            _dellHeader.Controls.Add(new Label
            {
                AutoSize = true,
                Dock = DockStyle.Fill,
                Text = "Dell laptop features",
                Font = new Font(Font.FontFamily, Font.Size, FontStyle.Bold),
                ForeColor = foreMain,
                BackColor = buttonSecond
            });

            // Fixed height: AutoSize ignores Dock=Top children and previously collapsed this panel
            _dellBody = new Panel
            {
                Name = "panelDellBody",
                AutoSize = false,
                Dock = DockStyle.Top,
                Height = 520,
                MinimumSize = new Size(200, 520),
                BackColor = formBack,
                Padding = new Padding(21, 8, 11, 12)
            };

            _dellAdminBtn = new Button
            {
                Text = "Restart as Administrator",
                AutoSize = true,
                Dock = DockStyle.Top,
                Visible = false,
                Margin = new Padding(5, 8, 5, 4),
                Padding = new Padding(8, 6, 8, 6),
                BackColor = buttonMain,
                ForeColor = foreMain,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _dellAdminBtn.FlatAppearance.BorderColor = borderMain;
            _dellAdminBtn.Click += (_, _) => ProcessHelper.RunAsAdmin();

            _dellStatus = new Label
            {
                AutoSize = true,
                Dock = DockStyle.Top,
                MaximumSize = new Size(900, 0),
                ForeColor = Color.FromArgb(200, foreMain),
                BackColor = formBack,
                Padding = new Padding(3, 10, 3, 4),
                Text = "Loading Dell settings…"
            };

            _dellBlock = MakeDellCheck("Block Sleep");
            _dellAutoOn = MakeDellCheck("Auto On (AC / schedule)");
            _dellCam = MakeDellCheck("Camera enabled");
            _dellLid = MakeDellCheck("Lid switch enabled");
            _dellUsb = MakeDellCheck("USB PowerShare (charge devices while off)");

            _dellWol = MakeDellCombo("Disabled", "LanOnly", "LanWlan", "LanWlanSlic", "Enabled");
            _dellLabWol = MakeDellLabel("Wake on LAN");

            _dellCharge = MakeDellCombo("Standard", "Primarily AC", "Adaptive", "Express Charge", "Custom (main slider)");
            _dellLabCharge = MakeDellLabel("Charge mode");

            _dellIllum = MakeDellCombo("Disabled", "Dim", "Bright", "Auto");
            _dellLabIllum = MakeDellLabel("Keyboard light");

            // Dock Top: add in reverse visual order
            _dellBody.Controls.Add(_dellAdminBtn);
            _dellBody.Controls.Add(_dellStatus);
            _dellBody.Controls.Add(_dellBlock);
            _dellBody.Controls.Add(_dellAutoOn);
            _dellBody.Controls.Add(_dellCam);
            _dellBody.Controls.Add(_dellLid);
            _dellBody.Controls.Add(_dellUsb);
            _dellBody.Controls.Add(_dellWol);
            _dellBody.Controls.Add(_dellLabWol);
            _dellBody.Controls.Add(_dellCharge);
            _dellBody.Controls.Add(_dellLabCharge);
            _dellBody.Controls.Add(_dellIllum);
            _dellBody.Controls.Add(_dellLabIllum);

            Controls.Add(_dellBody);
            Controls.Add(_dellHeader);
            try
            {
                // Place Dell block under Hotkeys (bindings), above Other
                int bindIdx = Controls.GetChildIndex(panelBindings);
                Controls.SetChildIndex(_dellBody, bindIdx);
                Controls.SetChildIndex(_dellHeader, bindIdx);
            }
            catch { /* layout fallback */ }

            toolTip.SetToolTip(_dellLabIllum, "BIOS keyboard illumination (Disabled / Dim / Bright / Auto). May need a reboot if the LED does not update immediately.");
            toolTip.SetToolTip(_dellIllum, "BIOS keyboard illumination (Disabled / Dim / Bright / Auto). May need a reboot if the LED does not update immediately.");
            toolTip.SetToolTip(_dellLabCharge, "BIOS battery charge policy: Standard, Primarily AC, Adaptive, Express Charge, or Custom (uses the main charge-limit slider).");
            toolTip.SetToolTip(_dellCharge, "BIOS battery charge policy: Standard, Primarily AC, Adaptive, Express Charge, or Custom (uses the main charge-limit slider).");
            toolTip.SetToolTip(_dellLabWol, "Wake the PC from sleep/off when the network adapter receives a magic packet (or related wake event).");
            toolTip.SetToolTip(_dellWol, "Wake the PC from sleep/off when the network adapter receives a magic packet (or related wake event).");
            toolTip.SetToolTip(_dellUsb, "Charge phones/devices from USB even when the laptop is off or sleeping (uses battery).");
            toolTip.SetToolTip(_dellLid, "When enabled, closing the lid triggers the Windows lid action (sleep/hibernate/do nothing).");
            toolTip.SetToolTip(_dellCam, "Enable or disable the built-in camera in BIOS (may need reboot to take effect).");
            toolTip.SetToolTip(_dellAutoOn, "Allow the laptop to power on automatically from AC or a BIOS schedule.");
            toolTip.SetToolTip(_dellBlock, "Prevent the system from entering Sleep (BIOS). Useful for always-on docked use.");

            ThemeDellControls();

            if (hal == null || !hal.Capabilities.CctkAvailable)
            {
                SetDellStatus("Install Dell Command | Configure (cctk) to manage these settings.", true);
                return;
            }

            _dellIllum.SelectedIndexChanged += async (_, _) =>
            {
                if (_dellLoading) return;
                string? val = _dellIllum.SelectedItem?.ToString();
                if (string.IsNullOrEmpty(val)) return;
                await ApplyDellOption("KeyboardIllumination", val, _dellIllum, biosNote: true);
            };
            _dellCharge.SelectedIndexChanged += async (_, _) =>
            {
                if (_dellLoading) return;
                await ApplyDellCharge();
            };
            _dellWol.SelectedIndexChanged += async (_, _) =>
            {
                if (_dellLoading) return;
                string? val = _dellWol.SelectedItem?.ToString();
                if (string.IsNullOrEmpty(val)) return;
                await ApplyDellOption("WakeOnLan", val, _dellWol);
            };
            _dellUsb.CheckedChanged += async (_, _) => { if (!_dellLoading) await ApplyDellToggle(_dellUsb!, "UsbPowerShare"); };
            _dellLid.CheckedChanged += async (_, _) => { if (!_dellLoading) await ApplyDellToggle(_dellLid!, "LidSwitch"); };
            _dellCam.CheckedChanged += async (_, _) => { if (!_dellLoading) await ApplyDellToggle(_dellCam!, "Camera"); };
            _dellAutoOn.CheckedChanged += async (_, _) => { if (!_dellLoading) await ApplyDellToggle(_dellAutoOn!, "AutoOn"); };
            _dellBlock.CheckedChanged += async (_, _) => { if (!_dellLoading) await ApplyDellToggle(_dellBlock!, "BlockSleep"); };

            Shown += async (_, _) =>
            {
                ThemeDellControls();
                await LoadDellSettingsAsync(forceRefresh: false);
            };
        }

        private void SetDellStatus(string msg, bool error = false)
        {
            if (_dellStatus == null || IsDisposed) return;
            if (InvokeRequired) { BeginInvoke(() => SetDellStatus(msg, error)); return; }
            _dellStatus.Text = msg;
            _dellStatus.ForeColor = error
                ? Color.FromArgb(255, 220, 80, 80)
                : Color.FromArgb(200, foreMain);
            Logger.WriteLine("Dell Extra: " + msg);
        }

        private static void SelectCombo(RComboBox combo, string? value)
        {
            if (string.IsNullOrWhiteSpace(value) || combo == null) return;
            if (value.Equals("Off", StringComparison.OrdinalIgnoreCase)) value = "Disabled";
            for (int i = 0; i < combo.Items.Count; i++)
            {
                if (string.Equals(combo.Items[i]?.ToString(), value, StringComparison.OrdinalIgnoreCase))
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }
            combo.Items.Add(value);
            combo.SelectedIndex = combo.Items.Count - 1;
        }

        private bool EnsureDellAdmin(string action)
        {
            if (ProcessHelper.IsUserAdministrator()) return true;
            SetDellStatus($"Run D-Helper as Administrator to {action}.", true);
            if (_dellAdminBtn != null) _dellAdminBtn.Visible = true;
            try { Program.toast.RunToast("Run D-Helper as Administrator"); } catch { /* toast optional */ }
            return false;
        }

        private void ApplyDellSnapshot(DellExtraSnapshot snap)
        {
            if (_dellIllum != null && snap.Illum != null)
                SelectCombo(_dellIllum, snap.Illum);
            if (_dellCharge != null && snap.Charge != null)
            {
                _dellCharge.SelectedIndex = snap.Charge switch
                {
                    ChargeMode.PrimarilyAC => 1,
                    ChargeMode.Adaptive => 2,
                    ChargeMode.ExpressCharge => 3,
                    ChargeMode.Custom => 4,
                    _ => 0
                };
            }
            if (_dellWol != null && snap.Wol != null)
                SelectCombo(_dellWol, snap.Wol);

            void BindCheck(CheckBox? cb, bool? v)
            {
                if (cb == null || v == null) return;
                cb.Checked = v.Value;
            }
            BindCheck(_dellUsb, snap.Usb);
            BindCheck(_dellLid, snap.Lid);
            BindCheck(_dellCam, snap.Cam);
            BindCheck(_dellAutoOn, snap.AutoOn);
            BindCheck(_dellBlock, snap.Block);
        }

        private static int CountMissing(DellExtraSnapshot snap)
        {
            int n = 0;
            if (snap.Illum == null) n++;
            if (snap.Charge == null) n++;
            if (snap.Wol == null) n++;
            if (snap.Usb == null) n++;
            if (snap.Lid == null) n++;
            if (snap.Cam == null) n++;
            if (snap.AutoOn == null) n++;
            if (snap.Block == null) n++;
            return n;
        }

        private async Task LoadDellSettingsAsync(bool forceRefresh = false)
        {
            var hal = Program.acpi.DellHal;
            if (hal == null || !hal.Capabilities.CctkAvailable || IsDisposed) return;

            ThemeDellControls();

            foreach (var c in new Control?[] { _dellIllum, _dellCharge, _dellWol, _dellUsb, _dellLid, _dellCam, _dellAutoOn, _dellBlock })
                if (c != null) { c.Visible = true; c.Enabled = true; }
            foreach (var lab in new Control?[] { _dellLabIllum, _dellLabCharge, _dellLabWol })
                if (lab != null) { lab.Visible = true; lab.Enabled = true; }

            if (!ProcessHelper.IsUserAdministrator())
            {
                _dellLoading = false;
                SetDellStatus("Run D-Helper as Administrator to read/change Dell BIOS settings.", true);
                if (_dellAdminBtn != null) _dellAdminBtn.Visible = true;
                return;
            }
            if (_dellAdminBtn != null) _dellAdminBtn.Visible = false;

            // Instant UI from cache — never leave the user staring at defaults + "Loading…"
            var cached = forceRefresh ? null : hal.TryGetExtraSnapshot(TimeSpan.FromMinutes(10));
            if (cached != null && !cached.NeedAdmin)
            {
                _dellLoading = true;
                ApplyDellSnapshot(cached);
                _dellLoading = false;
                int miss = CountMissing(cached);
                SetDellStatus(miss == 0
                    ? "Dell features ready — change a control to apply."
                    : $"Dell features ready ({miss} unknown — you can still change them).");
                return;
            }

            // Single-flight: stacked Shown events were queuing duplicate CCTK batches and
            // abandoning loads without clearing _dellLoading (stuck "Loading…").
            if (_dellLoading)
            {
                SetDellStatus("Still loading Dell settings…");
                return;
            }

            _dellLoading = true;
            int gen = ++_dellLoadGen;
            SetDellStatus("Loading Dell settings…");

            try
            {
                var snap = await hal.RefreshExtraSnapshotAsync(force: forceRefresh).ConfigureAwait(true);
                if (IsDisposed) return;
                if (gen != _dellLoadGen) return; // superseded; finally clears only if we still own the gen

                if (snap.NeedAdmin)
                {
                    SetDellStatus("Could not read Dell settings — run as Administrator.", true);
                    if (_dellAdminBtn != null) _dellAdminBtn.Visible = true;
                    return;
                }

                ApplyDellSnapshot(snap);
                ThemeDellControls();
                int missing = CountMissing(snap);
                SetDellStatus(missing == 0
                    ? "Dell features ready — change a control to apply."
                    : $"Dell features ready ({missing} could not be read — you can still try changing them).");
            }
            catch (Exception ex)
            {
                SetDellStatus("Error loading Dell settings: " + ex.Message, true);
                Logger.WriteLine("Dell Extra load: " + ex);
            }
            finally
            {
                if (gen == _dellLoadGen)
                    _dellLoading = false;
            }
        }

        private async Task ApplyDellOption(string option, string value, RComboBox combo, bool biosNote = false)
        {
            var h = Program.acpi.DellHal;
            if (h == null) return;
            if (!EnsureDellAdmin($"change {option}")) return;

            combo.Enabled = false;
            SetDellStatus($"Applying {option}={value}…");
            var (ok, err, verified) = await Task.Run(() =>
            {
                bool success = h.SetCctkOption(option, value, out string error, out string? got);
                return (success, error, got);
            }).ConfigureAwait(true);
            if (IsDisposed) return;
            combo.Enabled = true;

            if (!ok)
            {
                SetDellStatus($"Error: {err}", true);
                try { Program.toast.RunToast($"Failed: {option}"); } catch { }
                await LoadDellSettingsAsync(forceRefresh: true);
                return;
            }

            _dellLoading = true;
            if (verified != null)
                SelectCombo(combo, verified);
            _dellLoading = false;

            string msg = verified != null
                ? $"Saved {option}={verified}."
                : $"Saved {option}={value}.";
            if (biosNote)
                msg += " If the keyboard LED did not change, reboot once.";
            if (verified != null && !verified.Equals(value, StringComparison.OrdinalIgnoreCase))
                msg += $" (BIOS reports {verified}.)";
            SetDellStatus(msg);
            try { Program.toast.RunToast(verified ?? value); } catch { }
        }

        private async Task ApplyDellToggle(CheckBox cb, string option)
        {
            var h = Program.acpi.DellHal;
            if (h == null) return;
            if (!EnsureDellAdmin($"change {option}"))
            {
                _dellLoading = true;
                cb.Checked = !cb.Checked;
                _dellLoading = false;
                return;
            }

            bool want = cb.Checked;
            cb.Enabled = false;
            SetDellStatus($"Applying {option}={(want ? "Enabled" : "Disabled")}…");
            var (ok, err, verified) = await Task.Run(() =>
            {
                bool success = h.SetCctkEnabled(option, want, out string error, out string? got);
                return (success, error, got);
            }).ConfigureAwait(true);
            if (IsDisposed) return;
            cb.Enabled = true;

            bool? got = verified == null ? null
                : verified.Equals("Enabled", StringComparison.OrdinalIgnoreCase) ? true
                : verified.Equals("Disabled", StringComparison.OrdinalIgnoreCase) ? false
                : null;

            if (!ok || (got != null && got != want))
            {
                _dellLoading = true;
                cb.Checked = got ?? !want;
                _dellLoading = false;
                SetDellStatus(ok ? $"Warning: {option} read back differently." : $"Error: {err}", true);
                try { Program.toast.RunToast($"Failed: {option}"); } catch { }
            }
            else
            {
                SetDellStatus($"Saved {option}={(want ? "Enabled" : "Disabled")}.");
                try { Program.toast.RunToast(option); } catch { }
            }
        }

        private async Task ApplyDellCharge()
        {
            var h = Program.acpi.DellHal;
            if (h == null || _dellCharge == null) return;
            if (!EnsureDellAdmin("change charge mode")) return;

            ChargeMode mode = _dellCharge.SelectedIndex switch
            {
                1 => ChargeMode.PrimarilyAC,
                2 => ChargeMode.Adaptive,
                3 => ChargeMode.ExpressCharge,
                4 => ChargeMode.Custom,
                _ => ChargeMode.Standard
            };
            int? start = null, stop = null;
            if (mode == ChargeMode.Custom)
            {
                var bounds = h.GetCustomChargeBounds();
                stop = bounds.Stop ?? AppConfig.Get("charge_limit", 80);
                start = bounds.Start ?? Math.Clamp(stop.Value - 15, 50, 95);
                if (stop < 55) stop = 80;
                if (stop.Value - start.Value < 5) start = Math.Max(50, stop.Value - 5);
                AppConfig.Set("charge_limit", stop.Value);
            }
            _dellCharge.Enabled = false;
            SetDellStatus($"Applying charge mode {mode}…");
            var (ok, err, verified) = await Task.Run(() =>
            {
                bool success = h.SetChargeMode(mode, start, stop, out string error);
                // Trust set + HAL cache — skip a second CCTK get
                return (success, error, success ? mode : (ChargeMode?)null);
            }).ConfigureAwait(true);
            if (IsDisposed) return;
            _dellCharge.Enabled = true;
            if (ok && verified != null)
            {
                _dellLoading = true;
                _dellCharge.SelectedIndex = verified switch
                {
                    ChargeMode.PrimarilyAC => 1,
                    ChargeMode.Adaptive => 2,
                    ChargeMode.ExpressCharge => 3,
                    ChargeMode.Custom => 4,
                    _ => 0
                };
                _dellLoading = false;
            }
            SetDellStatus(ok ? "Saved charge mode." : $"Error: {err}", !ok);
            if (!ok) try { Program.toast.RunToast("Charge mode failed"); } catch { }
            if (ok && mode == ChargeMode.Custom && stop != null)
                Program.settingsForm.VisualiseBattery(stop.Value);
        }
    }
}
