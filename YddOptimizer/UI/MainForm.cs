using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;
using YddOptimizer.Core;
using YddResize.Core;

namespace YddOptimizer.UI
{
    public class MainForm : Form
    {
        // Settings & State
        private AppSettings _settings;
        private byte[] _currentFileData;
        private int _originalHiTris = 0;
        private long _originalBytes = 0;
        private bool _isSyncing = false;
        private CancellationTokenSource _cts;

        private const int CollapsedWidth = 660;
        private const int ExpandedWidth = 1200;

        // Tabs
        private TabControl _tabMain;
        private TabPage _tabOpt;
        private TabPage _tabSettings;
        private TabPage _tabGuide;

        // Tab 1: Optimization Controls
        private GroupBox _grpMode;
        private RadioButton _rbSingle;
        private RadioButton _rbBatch;

        private GroupBox _grpPaths;
        private Label _lblIn;
        private TextBox _txtInput;
        private Button _btnBrowse;
        private Label _lblOutput;
        private TextBox _txtOutput;
        private Button _btnBrowseOutput;

        private GroupBox _grpStats;
        private Label _lblOriginalTris;
        private Label _lblOriginalMemory;
        private Label _lblEstimatedMemory;

        private GroupBox _grpLod;
        private CheckBox _chkHigh;
        private NumericUpDown _numHighPct;
        private NumericUpDown _numHighTris;
        private Label _lblHighTris;

        private CheckBox _chkMed;
        private NumericUpDown _numMedPct;
        private NumericUpDown _numMedTris;
        private Label _lblMedTris;

        private CheckBox _chkLow;
        private NumericUpDown _numLowPct;
        private NumericUpDown _numLowTris;
        private Label _lblLowTris;

        private CheckBox _chkVLow;
        private NumericUpDown _numVLowPct;
        private NumericUpDown _numVLowTris;
        private Label _lblVLowTris;

        private GroupBox _grpColors;
        private CheckBox _chkCol0;
        private ComboBox _cboCol0;
        private Button _btnCol0Pick;
        private Color _customCol0 = Color.White;

        private CheckBox _chkCol1;
        private ComboBox _cboCol1;
        private Button _btnCol1Pick;
        private Color _customCol1 = Color.White;

        private CheckBox _chkPreviewToggle;
        private Button _btnOptimize;
        private ProgressBar _progressBar;
        private Label _lblStatus;
        private RichTextBox _rtbLog;

        // 3D Preview (Right Container)
        private Panel _pnlPreviewContainer;
        private PreviewControl _previewControl;
        private Label _lblPreviewLod;
        private ComboBox _cboPreviewLod;
        private Label _lblPreviewMode;
        private ComboBox _cboPreviewMode;
        private Button _btnResetCamera;

        // Tab 2: Settings Controls
        private GroupBox _grpGeneralSettings;
        private Label _lblLang;
        private ComboBox _cboLanguage;
        private Label _lblGta5Path;
        private TextBox _txtGta5Path;
        private Button _btnBrowseGta5;
        private Button _btnAutoDetectGta5;
        private Label _lblCodeWalkerPath;
        private TextBox _txtCodeWalkerPath;
        private Button _btnBrowseCw;
        private Button _btnSaveSettings;

        // Tab 3: Guide Controls
        private RichTextBox _rtbGuide;

        public MainForm()
        {
            _settings = AppSettings.Load();
            Localization.CurrentLang = _settings.Language ?? "tr";

            InitializeComponent();
            LoadApplicationIcon();
            ApplyLocalization();
            UpdatePreviewPanelVisibility();
        }

        private void InitializeComponent()
        {
            Text = Localization.Get("AppTitle");
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(CollapsedWidth, 760);
            MinimumSize = new Size(640, 700);
            AllowDrop = true;
            BackColor = SystemColors.Control;
            ForeColor = SystemColors.ControlText;
            Font = new Font("Segoe UI", 9f);

            DragEnter += MainForm_DragEnter;
            DragDrop += MainForm_DragDrop;

            var pnlMain = new Panel { Dock = DockStyle.Fill };
            Controls.Add(pnlMain);

            var pnlLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 640,
                Padding = new Padding(6)
            };
            pnlMain.Controls.Add(pnlLeft);

            _pnlPreviewContainer = new Panel
            {
                Dock = DockStyle.Fill,
                Padding = new Padding(6),
                Visible = false
            };
            pnlMain.Controls.Add(_pnlPreviewContainer);

            SetupPreviewPanel();

            _tabMain = new TabControl
            {
                Dock = DockStyle.Fill,
                Font = new Font("Segoe UI", 9.5f)
            };
            pnlLeft.Controls.Add(_tabMain);

            _tabOpt = new TabPage();
            _tabSettings = new TabPage();
            _tabGuide = new TabPage();

            _tabMain.TabPages.Add(_tabOpt);
            _tabMain.TabPages.Add(_tabSettings);
            _tabMain.TabPages.Add(_tabGuide);

            SetupOptimizationTab();
            SetupSettingsTab();
            SetupGuideTab();
        }

        private void SetupOptimizationTab()
        {
            _tabOpt.AutoScroll = true;
            _tabOpt.Padding = new Padding(8);

            int y = 8;

            // 1. Mode
            _grpMode = new GroupBox
            {
                Location = new Point(8, y),
                Size = new Size(600, 50)
            };
            _rbSingle = new RadioButton { Location = new Point(16, 20), AutoSize = true, Checked = true };
            _rbBatch = new RadioButton { Location = new Point(230, 20), AutoSize = true };
            _rbSingle.CheckedChanged += (s, e) => OnModeChanged();
            _grpMode.Controls.Add(_rbSingle);
            _grpMode.Controls.Add(_rbBatch);
            _tabOpt.Controls.Add(_grpMode);
            y += 56;

            // 2. Paths
            _grpPaths = new GroupBox
            {
                Location = new Point(8, y),
                Size = new Size(600, 95)
            };
            _lblIn = new Label { Location = new Point(14, 22), AutoSize = true };
            _txtInput = new TextBox { Location = new Point(115, 19), Size = new Size(395, 23) };
            _txtInput.TextChanged += (s, e) => LoadInputData();
            _btnBrowse = new Button { Location = new Point(518, 18), Size = new Size(70, 25) };
            _btnBrowse.Click += (s, e) => BrowseInput();

            _lblOutput = new Label { Location = new Point(14, 56), AutoSize = true };
            _txtOutput = new TextBox { Location = new Point(115, 53), Size = new Size(395, 23) };
            _btnBrowseOutput = new Button { Location = new Point(518, 52), Size = new Size(70, 25) };
            _btnBrowseOutput.Click += (s, e) => BrowseOutput();

            _grpPaths.Controls.Add(_lblIn);
            _grpPaths.Controls.Add(_txtInput);
            _grpPaths.Controls.Add(_btnBrowse);
            _grpPaths.Controls.Add(_lblOutput);
            _grpPaths.Controls.Add(_txtOutput);
            _grpPaths.Controls.Add(_btnBrowseOutput);
            _tabOpt.Controls.Add(_grpPaths);
            y += 102;

            // 3. Stats Card
            _grpStats = new GroupBox
            {
                Location = new Point(8, y),
                Size = new Size(600, 56)
            };
            _lblOriginalTris = new Label { Location = new Point(16, 22), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            _lblOriginalMemory = new Label { Location = new Point(225, 22), AutoSize = true };
            _lblEstimatedMemory = new Label { Location = new Point(410, 22), AutoSize = true, ForeColor = Color.FromArgb(34, 139, 34) };
            _grpStats.Controls.Add(_lblOriginalTris);
            _grpStats.Controls.Add(_lblOriginalMemory);
            _grpStats.Controls.Add(_lblEstimatedMemory);
            _tabOpt.Controls.Add(_grpStats);
            y += 62;

            // 4. LOD Rows
            _grpLod = new GroupBox
            {
                Location = new Point(8, y),
                Size = new Size(600, 150)
            };

            int lodY = 22;
            CreateLodRow(_grpLod, lodY, ref _chkHigh, ref _numHighPct, ref _numHighTris, ref _lblHighTris, 100, true);
            lodY += 29;
            CreateLodRow(_grpLod, lodY, ref _chkMed, ref _numMedPct, ref _numMedTris, ref _lblMedTris, 25, true);
            lodY += 29;
            CreateLodRow(_grpLod, lodY, ref _chkLow, ref _numLowPct, ref _numLowTris, ref _lblLowTris, 10, true);
            lodY += 29;
            CreateLodRow(_grpLod, lodY, ref _chkVLow, ref _numVLowPct, ref _numVLowTris, ref _lblVLowTris, 5, false);

            _tabOpt.Controls.Add(_grpLod);
            y += 156;

            // 5. Colors
            _grpColors = new GroupBox
            {
                Location = new Point(8, y),
                Size = new Size(600, 85)
            };

            _chkCol0 = new CheckBox { Location = new Point(14, 22), AutoSize = true };
            _cboCol0 = new ComboBox { Location = new Point(155, 20), Size = new Size(225, 23), DropDownStyle = ComboBoxStyle.DropDownList };
            _btnCol0Pick = new Button { Location = new Point(390, 19), Size = new Size(65, 25), Enabled = false };
            _btnCol0Pick.Click += (s, e) => PickColor(ref _customCol0, _btnCol0Pick);
            _cboCol0.SelectedIndexChanged += (s, e) => { _btnCol0Pick.Enabled = _cboCol0.SelectedIndex == 3; };

            _chkCol1 = new CheckBox { Location = new Point(14, 52), AutoSize = true };
            _cboCol1 = new ComboBox { Location = new Point(155, 50), Size = new Size(225, 23), DropDownStyle = ComboBoxStyle.DropDownList };
            _btnCol1Pick = new Button { Location = new Point(390, 49), Size = new Size(65, 25), Enabled = false };
            _btnCol1Pick.Click += (s, e) => PickColor(ref _customCol1, _btnCol1Pick);
            _cboCol1.SelectedIndexChanged += (s, e) => { _btnCol1Pick.Enabled = _cboCol1.SelectedIndex == 3; };

            _grpColors.Controls.Add(_chkCol0);
            _grpColors.Controls.Add(_cboCol0);
            _grpColors.Controls.Add(_btnCol0Pick);
            _grpColors.Controls.Add(_chkCol1);
            _grpColors.Controls.Add(_cboCol1);
            _grpColors.Controls.Add(_btnCol1Pick);
            _tabOpt.Controls.Add(_grpColors);
            y += 91;

            // 6. Action Panel
            var pnlActions = new Panel { Location = new Point(8, y), Size = new Size(600, 38) };
            _chkPreviewToggle = new CheckBox
            {
                Location = new Point(4, 9),
                AutoSize = true,
                Font = new Font("Segoe UI", 9.5f)
            };
            _chkPreviewToggle.CheckedChanged += (s, e) => UpdatePreviewPanelVisibility();

            _btnOptimize = new Button
            {
                Location = new Point(395, 3),
                Size = new Size(200, 32),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(40, 167, 69),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnOptimize.FlatAppearance.BorderSize = 0;
            _btnOptimize.Click += async (s, e) => await StartOptimizationAsync();

            pnlActions.Controls.Add(_chkPreviewToggle);
            pnlActions.Controls.Add(_btnOptimize);
            _tabOpt.Controls.Add(pnlActions);
            y += 42;

            // 7. Progress & Status
            _progressBar = new ProgressBar { Location = new Point(8, y), Size = new Size(600, 12), Style = ProgressBarStyle.Continuous };
            _tabOpt.Controls.Add(_progressBar);
            y += 16;

            _lblStatus = new Label { Location = new Point(8, y), Size = new Size(600, 18), ForeColor = Color.FromArgb(80, 90, 105) };
            _tabOpt.Controls.Add(_lblStatus);
            y += 22;

            // 8. Log Window
            _rtbLog = new RichTextBox
            {
                Location = new Point(8, y),
                Size = new Size(600, 120),
                ReadOnly = true,
                BackColor = Color.White,
                ForeColor = Color.FromArgb(30, 30, 30),
                Font = new Font("Consolas", 8.5f),
                BorderStyle = BorderStyle.FixedSingle
            };
            _tabOpt.Controls.Add(_rtbLog);
        }

        private void SetupSettingsTab()
        {
            _tabSettings.Padding = new Padding(12);

            _grpGeneralSettings = new GroupBox
            {
                Location = new Point(10, 10),
                Size = new Size(595, 260)
            };

            int y = 25;

            // Language
            _lblLang = new Label { Location = new Point(15, y + 3), AutoSize = true, Font = new Font("Segoe UI", 9f, FontStyle.Bold) };
            _cboLanguage = new ComboBox { Location = new Point(220, y), Size = new Size(200, 23), DropDownStyle = ComboBoxStyle.DropDownList };
            _cboLanguage.Items.AddRange(new object[] { "Türkçe (TR)", "English (EN)", "Deutsch (DE)" });
            _cboLanguage.SelectedIndex = Localization.CurrentLang == "de" ? 2 : (Localization.CurrentLang == "en" ? 1 : 0);
            _cboLanguage.SelectedIndexChanged += (s, e) =>
            {
                string newLang = _cboLanguage.SelectedIndex == 2 ? "de" : (_cboLanguage.SelectedIndex == 1 ? "en" : "tr");
                if (Localization.CurrentLang != newLang)
                {
                    Localization.CurrentLang = newLang;
                    _settings.Language = newLang;
                    _settings.Save();
                    ApplyLocalization();
                }
            };
            _grpGeneralSettings.Controls.Add(_lblLang);
            _grpGeneralSettings.Controls.Add(_cboLanguage);
            y += 45;

            // GTA 5 Path
            _lblGta5Path = new Label { Location = new Point(15, y), AutoSize = true };
            _txtGta5Path = new TextBox { Location = new Point(15, y + 20), Size = new Size(420, 23), Text = _settings.Gta5Path ?? "" };
            _btnBrowseGta5 = new Button { Location = new Point(442, y + 19), Size = new Size(65, 25) };
            _btnBrowseGta5.Click += (s, e) => BrowseDirectory("GTA 5 Oyun Klasörünü Seçin", _txtGta5Path);

            _btnAutoDetectGta5 = new Button { Location = new Point(512, y + 19), Size = new Size(72, 25) };
            _btnAutoDetectGta5.Click += (s, e) => AutoDetectGta5();

            _grpGeneralSettings.Controls.Add(_lblGta5Path);
            _grpGeneralSettings.Controls.Add(_txtGta5Path);
            _grpGeneralSettings.Controls.Add(_btnBrowseGta5);
            _grpGeneralSettings.Controls.Add(_btnAutoDetectGta5);
            y += 60;

            // CodeWalker Path
            _lblCodeWalkerPath = new Label { Location = new Point(15, y), AutoSize = true };
            _txtCodeWalkerPath = new TextBox { Location = new Point(15, y + 20), Size = new Size(420, 23), Text = _settings.CodeWalkerPath ?? "" };
            _btnBrowseCw = new Button { Location = new Point(442, y + 19), Size = new Size(65, 25) };
            _btnBrowseCw.Click += (s, e) => BrowseDirectory("CodeWalker Klasörünü Seçin", _txtCodeWalkerPath);

            _grpGeneralSettings.Controls.Add(_lblCodeWalkerPath);
            _grpGeneralSettings.Controls.Add(_txtCodeWalkerPath);
            _grpGeneralSettings.Controls.Add(_btnBrowseCw);
            y += 65;

            // Save Settings Button
            _btnSaveSettings = new Button
            {
                Location = new Point(15, y),
                Size = new Size(180, 32),
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                BackColor = Color.FromArgb(0, 122, 204),
                ForeColor = Color.White,
                FlatStyle = FlatStyle.Flat,
                Cursor = Cursors.Hand
            };
            _btnSaveSettings.FlatAppearance.BorderSize = 0;
            _btnSaveSettings.Click += (s, e) =>
            {
                _settings.Gta5Path = _txtGta5Path.Text.Trim();
                _settings.CodeWalkerPath = _txtCodeWalkerPath.Text.Trim();
                _settings.Language = Localization.CurrentLang;
                _settings.Save();
                MessageBox.Show(Localization.Get("MsgSettingsSaved"), Localization.Get("AppTitle"), MessageBoxButtons.OK, MessageBoxIcon.Information);
            };
            _grpGeneralSettings.Controls.Add(_btnSaveSettings);

            _tabSettings.Controls.Add(_grpGeneralSettings);
        }

        private void SetupGuideTab()
        {
            _tabGuide.Padding = new Padding(8);

            _rtbGuide = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.FromArgb(252, 253, 255),
                ForeColor = Color.FromArgb(35, 40, 50),
                Font = new Font("Segoe UI", 9.5f),
                BorderStyle = BorderStyle.None
            };
            _tabGuide.Controls.Add(_rtbGuide);
        }

        private void SetupPreviewPanel()
        {
            var pnlTop = new Panel { Dock = DockStyle.Top, Height = 36, Padding = new Padding(4) };

            _lblPreviewLod = new Label { Location = new Point(6, 8), AutoSize = true };
            _cboPreviewLod = new ComboBox { Location = new Point(44, 5), Size = new Size(95, 23), DropDownStyle = ComboBoxStyle.DropDownList };
            _cboPreviewLod.Items.AddRange(new object[] { "High", "Medium", "Low", "VeryLow" });
            _cboPreviewLod.SelectedIndex = 0;
            _cboPreviewLod.SelectedIndexChanged += (s, e) =>
            {
                if (_previewControl != null)
                    _previewControl.CurrentLod = (LodLevel)_cboPreviewLod.SelectedIndex;
            };

            _lblPreviewMode = new Label { Location = new Point(150, 8), AutoSize = true };
            _cboPreviewMode = new ComboBox { Location = new Point(205, 5), Size = new Size(155, 23), DropDownStyle = ComboBoxStyle.DropDownList };
            _cboPreviewMode.SelectedIndexChanged += (s, e) =>
            {
                if (_previewControl != null)
                {
                    if (_cboPreviewMode.SelectedIndex == 0) _previewControl.Mode = RenderMode.SolidAndWireframe;
                    else if (_cboPreviewMode.SelectedIndex == 1) _previewControl.Mode = RenderMode.Solid;
                    else _previewControl.Mode = RenderMode.Wireframe;
                }
            };

            _btnResetCamera = new Button { Location = new Point(370, 4), Size = new Size(130, 25) };
            _btnResetCamera.Click += (s, e) => _previewControl?.ResetCamera();

            pnlTop.Controls.Add(_lblPreviewLod);
            pnlTop.Controls.Add(_cboPreviewLod);
            pnlTop.Controls.Add(_lblPreviewMode);
            pnlTop.Controls.Add(_cboPreviewMode);
            pnlTop.Controls.Add(_btnResetCamera);

            _previewControl = new PreviewControl { Dock = DockStyle.Fill };

            _pnlPreviewContainer.Controls.Add(_previewControl);
            _pnlPreviewContainer.Controls.Add(pnlTop);
        }

        private void CreateLodRow(GroupBox grp, int y, ref CheckBox chk, ref NumericUpDown numPct, ref NumericUpDown numTris, ref Label lblTris, int defaultPct, bool isChecked)
        {
            chk = new CheckBox { Location = new Point(14, y + 2), Size = new Size(115, 20), Checked = isChecked };

            var lblPct = new Label { Text = "%", Location = new Point(135, y + 3), AutoSize = true };
            numPct = new NumericUpDown { Location = new Point(152, y), Size = new Size(60, 23), Minimum = 1, Maximum = 100, Value = defaultPct };

            var lblArrow = new Label { Text = "↔", Location = new Point(220, y + 3), AutoSize = true };

            lblTris = new Label { Location = new Point(245, y + 3), AutoSize = true };
            numTris = new NumericUpDown { Location = new Point(300, y), Size = new Size(100, 23), Minimum = 1, Maximum = 10000000, Value = 1000 };

            var p = numPct;
            var t = numTris;
            var c = chk;

            p.ValueChanged += (s, e) => OnPercentChanged(p, t);
            t.ValueChanged += (s, e) => OnTrisChanged(t, p);
            c.CheckedChanged += (s, e) =>
            {
                p.Enabled = c.Checked;
                t.Enabled = c.Checked;
                RecalculateEstimatedMemory();
            };

            grp.Controls.Add(chk);
            grp.Controls.Add(lblPct);
            grp.Controls.Add(numPct);
            grp.Controls.Add(lblArrow);
            grp.Controls.Add(lblTris);
            grp.Controls.Add(numTris);
        }

        private void ApplyLocalization()
        {
            Text = Localization.Get("AppTitle");

            _tabOpt.Text = Localization.Get("TabOptimization");
            _tabSettings.Text = Localization.Get("TabSettings");
            _tabGuide.Text = Localization.Get("TabGuide");

            _grpMode.Text = Localization.Get("GrpMode");
            _rbSingle.Text = Localization.Get("ModeSingle");
            _rbBatch.Text = Localization.Get("ModeBatch");

            _grpPaths.Text = Localization.Get("GrpPaths");
            _lblIn.Text = _rbSingle.Checked ? Localization.Get("LblInputFile") : Localization.Get("LblInputFolder");
            _lblOutput.Text = _rbSingle.Checked ? Localization.Get("LblOutputFile") : Localization.Get("LblOutputFolder");
            _btnBrowse.Text = Localization.Get("BtnBrowse");
            _btnBrowseOutput.Text = Localization.Get("BtnBrowse");

            _grpStats.Text = Localization.Get("GrpStats");
            if (_originalHiTris > 0)
            {
                _lblOriginalTris.Text = Localization.Format("StatOrigTris", _originalHiTris);
                _lblOriginalMemory.Text = Localization.Format("StatOrigMem", _originalBytes / (1024.0 * 1024.0));
            }
            else
            {
                _lblOriginalTris.Text = Localization.Format("StatOrigTris", 0);
                _lblOriginalMemory.Text = Localization.Format("StatOrigMem", 0.0);
            }

            _grpLod.Text = Localization.Get("GrpLod");
            _chkHigh.Text = Localization.Get("LodHigh");
            _chkMed.Text = Localization.Get("LodMed");
            _chkLow.Text = Localization.Get("LodLow");
            _chkVLow.Text = Localization.Get("LodVLow");
            _lblHighTris.Text = Localization.Get("LblTris");
            _lblMedTris.Text = Localization.Get("LblTris");
            _lblLowTris.Text = Localization.Get("LblTris");
            _lblVLowTris.Text = Localization.Get("LblTris");

            _grpColors.Text = Localization.Get("GrpColors");
            _chkCol0.Text = Localization.Get("Col0Edit");
            _chkCol1.Text = Localization.Get("Col1Edit");
            _btnCol0Pick.Text = Localization.Get("BtnColorPick");
            _btnCol1Pick.Text = Localization.Get("BtnColorPick");

            int idx0 = Math.Max(0, _cboCol0.SelectedIndex);
            int idx1 = Math.Max(0, _cboCol1.SelectedIndex);
            _cboCol0.Items.Clear();
            _cboCol1.Items.Clear();
            var colItems = new object[]
            {
                Localization.Get("ColWhite"),
                Localization.Get("ColBlack"),
                Localization.Get("ColAlpha255"),
                Localization.Get("ColCustom")
            };
            _cboCol0.Items.AddRange(colItems);
            _cboCol1.Items.AddRange(colItems);
            _cboCol0.SelectedIndex = idx0;
            _cboCol1.SelectedIndex = idx1;

            _chkPreviewToggle.Text = Localization.Get("ChkPreviewToggle");
            _btnOptimize.Text = Localization.Get("BtnOptimize");
            _lblStatus.Text = Localization.Get("StatusReady");

            // Settings tab strings
            _grpGeneralSettings.Text = Localization.Get("GrpGeneralSettings");
            _lblLang.Text = Localization.Get("LblLanguage");
            _lblGta5Path.Text = Localization.Get("LblGta5Path");
            _lblCodeWalkerPath.Text = Localization.Get("LblCodeWalkerPath");
            _btnBrowseGta5.Text = Localization.Get("BtnBrowse");
            _btnBrowseCw.Text = Localization.Get("BtnBrowse");
            _btnAutoDetectGta5.Text = Localization.Get("BtnAutoDetect");
            _btnSaveSettings.Text = Localization.Get("BtnSaveSettings");

            // Preview strings
            _lblPreviewLod.Text = Localization.Get("PreviewLod");
            _lblPreviewMode.Text = Localization.Get("PreviewMode");
            _btnResetCamera.Text = Localization.Get("PreviewReset");

            int mIdx = Math.Max(0, _cboPreviewMode.SelectedIndex);
            _cboPreviewMode.Items.Clear();
            _cboPreviewMode.Items.AddRange(new object[]
            {
                Localization.Get("PreviewModeSolidWire"),
                Localization.Get("PreviewModeSolid"),
                Localization.Get("PreviewModeWire")
            });
            _cboPreviewMode.SelectedIndex = mIdx;

            // Guide text
            _rtbGuide.Text = Localization.Get("GuideText");
        }

        private void OnModeChanged()
        {
            if (_rbSingle.Checked)
            {
                _lblIn.Text = Localization.Get("LblInputFile");
                _lblOutput.Text = Localization.Get("LblOutputFile");
                if (!string.IsNullOrEmpty(_txtInput.Text) && File.Exists(_txtInput.Text))
                {
                    string dir = Path.GetDirectoryName(_txtInput.Text);
                    string name = Path.GetFileNameWithoutExtension(_txtInput.Text);
                    _txtOutput.Text = Path.Combine(dir, name + "_resized.ydd");
                }
            }
            else
            {
                _lblIn.Text = Localization.Get("LblInputFolder");
                _lblOutput.Text = Localization.Get("LblOutputFolder");
                if (!string.IsNullOrEmpty(_txtInput.Text) && Directory.Exists(_txtInput.Text))
                {
                    _txtOutput.Text = Path.Combine(_txtInput.Text, "_resized");
                }
            }
        }

        private void BrowseInput()
        {
            if (_rbSingle.Checked)
            {
                using (var ofd = new OpenFileDialog
                {
                    Filter = "GTA 5 Model Dosyaları (*.ydd)|*.ydd|All Files (*.*)|*.*",
                    Title = "YDD"
                })
                {
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        _txtInput.Text = ofd.FileName;
                    }
                }
            }
            else
            {
                using (var fbd = new FolderBrowserDialog())
                {
                    if (fbd.ShowDialog() == DialogResult.OK)
                    {
                        _txtInput.Text = fbd.SelectedPath;
                    }
                }
            }
        }

        private void BrowseOutput()
        {
            if (_rbSingle.Checked)
            {
                using (var sfd = new SaveFileDialog
                {
                    Filter = "GTA 5 Model Dosyaları (*.ydd)|*.ydd"
                })
                {
                    if (sfd.ShowDialog() == DialogResult.OK)
                    {
                        _txtOutput.Text = sfd.FileName;
                    }
                }
            }
            else
            {
                using (var fbd = new FolderBrowserDialog())
                {
                    if (fbd.ShowDialog() == DialogResult.OK)
                    {
                        _txtOutput.Text = fbd.SelectedPath;
                    }
                }
            }
        }

        private void BrowseDirectory(string title, TextBox target)
        {
            using (var fbd = new FolderBrowserDialog { Description = title })
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    target.Text = fbd.SelectedPath;
                }
            }
        }

        private void AutoDetectGta5()
        {
            string found = null;
            try
            {
                // Registry checks
                string[] regPaths = new[]
                {
                    @"SOFTWARE\WOW6432Node\Rockstar Games\Grand Theft Auto V",
                    @"SOFTWARE\Rockstar Games\Grand Theft Auto V",
                    @"SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 271590",
                    @"SOFTWARE\WOW6432Node\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 271590"
                };

                foreach (var reg in regPaths)
                {
                    using (var key = Registry.LocalMachine.OpenSubKey(reg))
                    {
                        if (key != null)
                        {
                            object val = key.GetValue("InstallFolder") ?? key.GetValue("InstallLocation") ?? key.GetValue("InstallPath");
                            if (val != null && Directory.Exists(val.ToString()))
                            {
                                found = val.ToString();
                                break;
                            }
                        }
                    }
                }

                if (string.IsNullOrEmpty(found))
                {
                    string[] candidates = new[]
                    {
                        @"C:\Program Files\Rockstar Games\Grand Theft Auto V",
                        @"C:\Program Files (x86)\Steam\steamapps\common\Grand Theft Auto V",
                        @"D:\SteamLibrary\steamapps\common\Grand Theft Auto V",
                        @"E:\SteamLibrary\steamapps\common\Grand Theft Auto V",
                        @"C:\Program Files\Epic Games\GTAV",
                        @"D:\Epic Games\GTAV"
                    };

                    foreach (var c in candidates)
                    {
                        if (Directory.Exists(c) && File.Exists(Path.Combine(c, "GTA5.exe")))
                        {
                            found = c;
                            break;
                        }
                    }
                }
            }
            catch { }

            if (!string.IsNullOrEmpty(found))
            {
                _txtGta5Path.Text = found;
                MessageBox.Show("GTA 5 bulundu: " + found, "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("GTA 5 klasörü otomatik tespit edilemedi. Lütfen 'Gözat...' ile manuel seçin.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void OnPercentChanged(NumericUpDown numPct, NumericUpDown numTris)
        {
            if (_isSyncing) return;
            if (_originalHiTris <= 0) return;
            _isSyncing = true;
            try
            {
                double pct = (double)numPct.Value;
                int targetTris = (int)Math.Max(1, Math.Round(_originalHiTris * (pct / 100.0)));
                numTris.Value = Math.Min(numTris.Maximum, targetTris);
            }
            finally
            {
                _isSyncing = false;
                RecalculateEstimatedMemory();
            }
        }

        private void OnTrisChanged(NumericUpDown numTris, NumericUpDown numPct)
        {
            if (_isSyncing) return;
            if (_originalHiTris <= 0) return;
            _isSyncing = true;
            try
            {
                double tris = (double)numTris.Value;
                decimal pct = (decimal)Math.Max(0.1, Math.Min(100.0, Math.Round((tris / _originalHiTris) * 100.0, 1)));
                numPct.Value = pct;
            }
            finally
            {
                _isSyncing = false;
                RecalculateEstimatedMemory();
            }
        }

        private void RecalculateEstimatedMemory()
        {
            if (_originalBytes <= 0 || _originalHiTris <= 0)
            {
                _lblEstimatedMemory.Text = "Sonuç Bellek: -";
                return;
            }

            double totalRatio = 0.0;
            int count = 0;
            if (_chkHigh.Checked) { totalRatio += (double)_numHighPct.Value / 100.0; count++; }
            if (_chkMed.Checked) { totalRatio += (double)_numMedPct.Value / 100.0; count++; }
            if (_chkLow.Checked) { totalRatio += (double)_numLowPct.Value / 100.0; count++; }
            if (_chkVLow.Checked) { totalRatio += (double)_numVLowPct.Value / 100.0; count++; }

            long estBytes = (long)(_originalBytes * (totalRatio / Math.Max(1, count)));
            double mb = estBytes / (1024.0 * 1024.0);

            _lblEstimatedMemory.Text = Localization.Format("StatResultMem", mb);
        }

        private void UpdatePreviewPanelVisibility()
        {
            bool show = _chkPreviewToggle.Checked;
            _pnlPreviewContainer.Visible = show;
            ClientSize = new Size(show ? ExpandedWidth : CollapsedWidth, Height);
        }

        private void LoadInputData()
        {
            string path = _txtInput.Text;
            if (string.IsNullOrEmpty(path)) return;

            if (_rbSingle.Checked && File.Exists(path))
            {
                string dir = Path.GetDirectoryName(path);
                string name = Path.GetFileNameWithoutExtension(path);
                _txtOutput.Text = Path.Combine(dir, name + "_resized.ydd");

                try
                {
                    _currentFileData = File.ReadAllBytes(path);
                    var ydd = YddResizer.LoadYdd(_currentFileData);
                    _originalHiTris = YddResizer.CountTriangles(ydd, LodLevel.High);
                    _originalBytes = YddResizer.EstimateBytes(ydd);

                    _lblOriginalTris.Text = Localization.Format("StatOrigTris", _originalHiTris);
                    _lblOriginalMemory.Text = Localization.Format("StatOrigMem", _originalBytes / (1024.0 * 1024.0));

                    _isSyncing = true;
                    try
                    {
                        _numHighTris.Value = Math.Max(1, (int)Math.Round(_originalHiTris * ((double)_numHighPct.Value / 100.0)));
                        _numMedTris.Value = Math.Max(1, (int)Math.Round(_originalHiTris * ((double)_numMedPct.Value / 100.0)));
                        _numLowTris.Value = Math.Max(1, (int)Math.Round(_originalHiTris * ((double)_numLowPct.Value / 100.0)));
                        _numVLowTris.Value = Math.Max(1, (int)Math.Round(_originalHiTris * ((double)_numVLowPct.Value / 100.0)));
                    }
                    finally
                    {
                        _isSyncing = false;
                    }

                    RecalculateEstimatedMemory();

                    if (_previewControl != null)
                    {
                        _previewControl.LoadFromYdd(_currentFileData);
                    }

                    Log(string.Format("YDD yüklendi: {0} ({1:N0} tris, {2:0.##} MiB)", Path.GetFileName(path), _originalHiTris, _originalBytes / (1024.0 * 1024.0)));
                }
                catch (Exception ex)
                {
                    Log("Hata: " + ex.Message);
                }
            }
            else if (_rbBatch.Checked && Directory.Exists(path))
            {
                _txtOutput.Text = Path.Combine(path, "_resized");
                int count = Directory.GetFiles(path, "*.ydd", SearchOption.AllDirectories).Length;
                _lblOriginalTris.Text = Localization.Format("StatBatchCount", count);
                _lblOriginalMemory.Text = "";
                _lblEstimatedMemory.Text = "";
                Log(string.Format("Klasör: {0} ({1} YDD)", path, count));
            }
        }

        private void PickColor(ref Color targetColor, Button btn)
        {
            using (var cd = new ColorDialog { Color = targetColor })
            {
                if (cd.ShowDialog() == DialogResult.OK)
                {
                    targetColor = cd.Color;
                    btn.BackColor = targetColor;
                }
            }
        }

        private async Task StartOptimizationAsync()
        {
            if (_rbSingle.Checked)
            {
                if (string.IsNullOrEmpty(_txtInput.Text) || !File.Exists(_txtInput.Text))
                {
                    MessageBox.Show("Lütfen geçerli bir .ydd dosyası seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(_txtInput.Text) || !Directory.Exists(_txtInput.Text))
                {
                    MessageBox.Show("Lütfen geçerli bir klasör seçin.", "Uyarı", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            var opt = BuildResizeOptions();

            _btnOptimize.Enabled = false;
            _lblStatus.Text = Localization.Get("StatusProcessing");
            _progressBar.Value = 0;
            _cts = new CancellationTokenSource();

            try
            {
                if (_rbSingle.Checked)
                {
                    string inPath = _txtInput.Text;
                    string outPath = _txtOutput.Text;
                    if (string.IsNullOrEmpty(outPath))
                    {
                        outPath = Path.Combine(Path.GetDirectoryName(inPath), Path.GetFileNameWithoutExtension(inPath) + "_resized.ydd");
                    }

                    await Task.Run(() =>
                    {
                        byte[] data = File.ReadAllBytes(inPath);
                        ResizeReport rep;
                        byte[] res = YddResizer.Resize(data, opt, out rep);
                        File.WriteAllBytes(outPath, res);

                        Invoke(new Action(() =>
                        {
                            _progressBar.Value = 100;
                            _lblStatus.Text = Localization.Get("StatusReady");
                            Log(string.Format("Kaydedildi -> {0}", outPath));
                            Log(string.Format("  Önceki: {0:0.##} MiB | Sonraki: {1:0.##} MiB",
                                rep.BytesBefore / (1024.0 * 1024.0), rep.BytesAfter / (1024.0 * 1024.0)));

                            if (_previewControl != null && _chkPreviewToggle.Checked)
                            {
                                _previewControl.LoadFromYdd(res);
                            }
                        }));
                    });
                }
                else
                {
                    string inDir = _txtInput.Text;
                    string outDir = _txtOutput.Text;
                    if (string.IsNullOrEmpty(outDir)) outDir = Path.Combine(inDir, "_resized");
                    Directory.CreateDirectory(outDir);

                    string[] files = Directory.GetFiles(inDir, "*.ydd", SearchOption.AllDirectories);
                    if (files.Length == 0)
                    {
                        MessageBox.Show("Seçilen klasörde hiç .ydd dosyası bulunamadı.", "Bilgi", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    await Task.Run(() =>
                    {
                        int processed = 0;
                        int failed = 0;

                        for (int i = 0; i < files.Length; i++)
                        {
                            string file = files[i];
                            string rel = file.Substring(inDir.Length).TrimStart('\\', '/');
                            string targetPath = Path.Combine(outDir, rel);
                            string targetDir = Path.GetDirectoryName(targetPath);
                            if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);

                            string targetFile = Path.Combine(targetDir, Path.GetFileNameWithoutExtension(targetPath) + "_resized.ydd");

                            try
                            {
                                byte[] data = File.ReadAllBytes(file);
                                ResizeReport rep;
                                byte[] res = YddResizer.Resize(data, opt, out rep);
                                File.WriteAllBytes(targetFile, res);
                                processed++;
                            }
                            catch (Exception ex)
                            {
                                failed++;
                                Invoke(new Action(() => Log(string.Format("Hata ({0}): {1}", Path.GetFileName(file), ex.Message))));
                            }

                            int pct = (int)((i + 1) * 100.0 / files.Length);
                            Invoke(new Action(() =>
                            {
                                _progressBar.Value = pct;
                                _lblStatus.Text = Localization.Format("StatusProcessing", i + 1, files.Length);
                            }));
                        }

                        Invoke(new Action(() =>
                        {
                            _lblStatus.Text = Localization.Format("StatusDone", processed, failed);
                            Log(string.Format("Toplu işlem bitti! {0} dosya kaydedildi.", processed));
                        }));
                    });
                }
            }
            catch (Exception ex)
            {
                Log("Kritik Hata: " + ex.Message);
                MessageBox.Show("Optimizasyon sırasında bir hata oluştu:\n" + ex.Message, "Hata", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                _btnOptimize.Enabled = true;
            }
        }

        private ResizeOptions BuildResizeOptions()
        {
            var opt = new ResizeOptions
            {
                Log = s => Invoke(new Action(() => Log("  " + s)))
            };

            if (_chkHigh.Checked)
                opt.Requests.Add(new ResizeRequest { Level = LodLevel.High, Percent = (double)_numHighPct.Value });
            if (_chkMed.Checked)
                opt.Requests.Add(new ResizeRequest { Level = LodLevel.Medium, Percent = (double)_numMedPct.Value });
            if (_chkLow.Checked)
                opt.Requests.Add(new ResizeRequest { Level = LodLevel.Low, Percent = (double)_numLowPct.Value });
            if (_chkVLow.Checked)
                opt.Requests.Add(new ResizeRequest { Level = LodLevel.VeryLow, Percent = (double)_numVLowPct.Value });

            if (_chkCol0.Checked)
            {
                opt.Color0.Enabled = true;
                opt.Color0.Mode = GetColorModeFromCombo(_cboCol0.SelectedIndex);
                opt.Color0.R = _customCol0.R;
                opt.Color0.G = _customCol0.G;
                opt.Color0.B = _customCol0.B;
                opt.Color0.A = _customCol0.A;
            }

            if (_chkCol1.Checked)
            {
                opt.Color1.Enabled = true;
                opt.Color1.Mode = GetColorModeFromCombo(_cboCol1.SelectedIndex);
                opt.Color1.R = _customCol1.R;
                opt.Color1.G = _customCol1.G;
                opt.Color1.B = _customCol1.B;
                opt.Color1.A = _customCol1.A;
            }

            return opt;
        }

        private static VertexColorMode GetColorModeFromCombo(int idx)
        {
            switch (idx)
            {
                case 0: return VertexColorMode.SetWhite;
                case 1: return VertexColorMode.SetBlack;
                case 2: return VertexColorMode.SetAlpha255;
                case 3: return VertexColorMode.CustomColor;
                default: return VertexColorMode.None;
            }
        }

        private void Log(string message)
        {
            string time = DateTime.Now.ToString("HH:mm:ss");
            _rtbLog.AppendText(string.Format("[{0}] {1}\r\n", time, message));
            _rtbLog.SelectionStart = _rtbLog.TextLength;
            _rtbLog.ScrollToCaret();
        }

        private void MainForm_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void MainForm_DragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files != null && files.Length > 0)
            {
                string first = files[0];
                if (Directory.Exists(first))
                {
                    _rbBatch.Checked = true;
                    _txtInput.Text = first;
                }
                else if (File.Exists(first) && first.EndsWith(".ydd", StringComparison.OrdinalIgnoreCase))
                {
                    _rbSingle.Checked = true;
                    _txtInput.Text = first;
                }
            }
        }

        private void LoadApplicationIcon()
        {
            try
            {
                string exeDir = AppDomain.CurrentDomain.BaseDirectory;
                string localIco = Path.Combine(exeDir, "icon.ico");
                if (File.Exists(localIco))
                {
                    Icon = new Icon(localIco);
                    return;
                }

                string projectIco = Path.Combine(exeDir, "..\\..\\icon.ico");
                if (File.Exists(projectIco))
                {
                    Icon = new Icon(projectIco);
                    return;
                }

                Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath);
            }
            catch { }
        }
    }
}
