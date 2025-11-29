using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

public class UpGun_Mod_Loader : Form
{
    [DllImport("user32.dll")] private static extern bool ReleaseCapture();
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HTCAPTION = 0x2;


    private readonly static Mutex mutex = new(initiallyOwned: true, "{E3D2A813-62E2-4DDF-9E91-38C20EC890D6}");

    private int currentPage;

    private const int itemsPerPage = 6;
    private Panel pnlTitle;
    private Button BtnReduce;
    private Button BtnClose;
    private PictureBox pbLogo;
    private Button btnNext;
    private Label lblPage;
    private Button btnPrevious;
    private Panel pnlPages;
    private Panel PnlSearch;
    private FlowLayoutPanel flpnlMods;
    private Button Btn_Discord;
    private Button Btn_Refresh;
    private Button Btn_UpGun;
    private ComboBox CbSearchTypes;
    private TextBox tbSearchBar;
    private Label lblTitle;
    private Label Version;
    private bool CanSearch = true;

    public UpGun_Mod_Loader()
    {
        if (mutex.WaitOne(TimeSpan.Zero, exitContext: true))
        {
            InitializeComponent();

            BtnClose.Click += BtnClose_Click;
            BtnReduce.Click += BtnReduce_Click;
            pnlTitle.MouseDown += TitleDrag_MouseDown;
            lblTitle.MouseDown += TitleDrag_MouseDown;
            pbLogo.MouseDown += TitleDrag_MouseDown;
            pnlPages.MouseDoubleClick += TitleDrag_MouseDown;
            btnNext.Click += PbNextPage_Click;
            btnPrevious.Click += PbPreviousPage_Click;
            CbSearchTypes.SelectedIndexChanged += Search;
            tbSearchBar.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    Search(s, e);
                }
            };

            int radius = 18;

            MakeRounded(PnlSearch, radius);
            MakeRounded(btnNext, radius);
            MakeRounded(btnPrevious, radius);
            MakeRounded(pnlPages, radius);
            MakeRounded(this, radius);
            Resize += (s, e) => MakeRounded(this, radius);

            tbSearchBar.BorderStyle = BorderStyle.None;
            tbSearchBar.BackColor = Color.FromArgb(20, 22, 30);
            tbSearchBar.ForeColor = Color.White;
            tbSearchBar.Font = new Font("Segoe UI", 10f, FontStyle.Regular);
            tbSearchBar.Padding = new Padding(6, 4, 6, 4);
            InitSearchBarPlaceholder();

            MakeRounded(tbSearchBar, 6);
            tbSearchBar.Resize += (s, e) => MakeRounded(tbSearchBar, 6);

            PnlSearch.Paint += PnlSearch_Paint;

            CheckForIllegalCrossThreadCalls = false;

            if (!Directory.Exists(Fonctions.appdatapath))
            {
                MessageBox.Show(
                    "Unable get appdata path.\nPlease check if the path bellow is valid.\n" + Fonctions.appdatapath,
                    "Unable get appdata path",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
                return;
            }

            if (Fonctions.CheckIfModSupportInstalled())
            {
                _ = Fonctions.CheckLoaderUpdate();

            }
            Fonctions.CheckGameUpdates();
            GetModsFileIds();
            mutex.ReleaseMutex();
        }
        else
        {
            MessageBox.Show("The mod loader is already opened!");
            Close();
        }
    }
    private void InitSearchBarPlaceholder()
    {
        const string placeholder = "Search by name";

        tbSearchBar.Text = placeholder;
        tbSearchBar.ForeColor = Color.Gray;

        tbSearchBar.GotFocus += (s, e) =>
        {
            if (tbSearchBar.Text == placeholder)
            {
                tbSearchBar.Text = "";
                tbSearchBar.ForeColor = Color.White;
            }
        };

        tbSearchBar.LostFocus += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(tbSearchBar.Text))
            {
                tbSearchBar.Text = placeholder;
                tbSearchBar.ForeColor = Color.Gray;
            }
        };
    }


    private void PnlSearch_Paint(object sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Color border = Color.FromArgb(60, 65, 75);
        Color fill = Color.FromArgb(20, 22, 30);

        var tbRect = tbSearchBar.Bounds;
        tbRect.Inflate(2, 2);
        using var path = GetRoundedPath(tbRect, 6);
        using var pen = new Pen(border, 1);
        using var brush = new SolidBrush(fill);
        e.Graphics.FillPath(brush, path);
        e.Graphics.DrawPath(pen, path);
    }

    private GraphicsPath GetRoundedPath(Rectangle rect, int radius)
    {
        var path = new GraphicsPath();
        int d = radius * 2;
        path.AddArc(rect.X, rect.Y, d, d, 180, 90);
        path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
        path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
        path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
        path.CloseFigure();
        return path;
    }

    private void MakeRounded(Control ctrl, int radius)
    {
        var bounds = ctrl.ClientRectangle;
        GraphicsPath path = new();
        int d = radius * 2;

        path.StartFigure();
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();

        ctrl.Region = new Region(path);
    }

    private void BtnClose_Click(object sender, EventArgs e) => Close();

    private void BtnReduce_Click(object sender, EventArgs e) => WindowState = FormWindowState.Minimized;

    private void TitleDrag_MouseDown(object sender, MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, HTCAPTION, 0);
        }
    }


    private async void GetModsFileIds()
    {
        GetAllMods(await Fonctions.GetUploadedMods(), tbSearchBar.Text, CbSearchTypes.SelectedItem?.ToString());
    }

    private void GetAllMods(string allMods, string searchName, string searchType)
    {
        if (string.IsNullOrWhiteSpace(searchName) ||
        string.Equals(searchName, "Search by name", StringComparison.OrdinalIgnoreCase))
            searchName = null;
        else
            searchName = searchName.Trim();
        if (string.IsNullOrWhiteSpace(searchType) ||
        string.Equals(searchType, "Search by types", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(searchType, "All", StringComparison.OrdinalIgnoreCase))
            searchType = null;
        else
            searchType = searchType.Trim();
        string[] array = allMods.Split(["\r\n", "\n"], StringSplitOptions.RemoveEmptyEntries);
        List<string[]> list = [];
        string[] array2 = array;
        for (int i = 0; i < array2.Length; i++)
        {
            string[] array3 = array2[i].Split(',');
            string text = array3[1];
            string text2 = array3[6];
            if ((string.IsNullOrEmpty(searchName) || text.IndexOf(searchName, StringComparison.OrdinalIgnoreCase) != -1) && (string.IsNullOrEmpty(searchType) || text2 == searchType))
            {
                list.Add(array3);
            }
        }
        int num = currentPage * itemsPerPage;
        int num2 = Math.Min((currentPage + 1) * itemsPerPage, list.Count);
        for (int j = num; j < num2; j++)
        {
            string[] values = list[j];
            Invoke((MethodInvoker)delegate
            {
                LoadMod(values[0], values[1], values[2], values[3], values[4], values[5], values[6], values[7]);
            });
        }
    }

    private void LoadMod(string OwnerName, string ModName, string Date, string ImageUrl, string ModDLUrl, string ZipFileName, string ModType, string ModSize)
    {
        DateTime modDate;

        try
        {
            modDate = DateTime.ParseExact(Date, "dd/MM/yyyy", CultureInfo.InvariantCulture);
        }
        catch
        {
            modDate = DateTime.ParseExact("01/01/2000", "dd/MM/yyyy", CultureInfo.InvariantCulture);
        }

        string newZipFileName = ZipFileName.EndsWith(".zip", StringComparison.OrdinalIgnoreCase)
            ? Path.GetFileNameWithoutExtension(ZipFileName)
            : ZipFileName;

        bool isInstalled = File.Exists(Path.Combine(Fonctions.UpGunPath, newZipFileName + ".pak"));

        var panel = new Panel
        {
            Name = $"pnlMod_{OwnerName}_{ModName}",
            BackColor = Color.FromArgb(20, 22, 30),
            Size = new Size(335, 100),
            Padding = new Padding(10),
            Margin = new Padding(5, 0, 5, 5),
        };

        var icnNew = new PictureBox
        {
            Name = $"icnNew_{ModName}",
            Image = global::UpGunModLoader.Properties.Resources.New_icon,
            Location = new Point(5, 5),
            Size = new Size(50, 25),
            SizeMode = PictureBoxSizeMode.StretchImage,
            TabStop = false
        };

        var picture = new PictureBox
        {
            Name = $"pictureBox_{ModName}",
            ImageLocation = ImageUrl,
            Location = new Point(10, 10),
            Size = new Size(143, 80),
            SizeMode = PictureBoxSizeMode.StretchImage,
            TabStop = false
        };

        var lblName = new Label
        {
            Name = $"lblName_{ModName}",
            AutoSize = true,
            Font = new Font("Franklin Gothic", 11f, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(175, 5),
            Text = ModName
        };

        var lblCreator = new Label
        {
            Name = $"lblCreator_{ModName}",
            AutoSize = true,
            Font = new Font("Myanmar Text", 9f, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(175, 30),
            Text = OwnerName
        };

        var lblType = new Label
        {
            Name = $"lblType_{ModName}",
            AutoSize = true,
            Font = new Font("Myanmar Text", 9f, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(175, 50),
            Text = ModType
        };

        var lblSize = new Label
        {
            Name = $"lblSize_{ModName}",
            AutoSize = true,
            Font = new Font("Myanmar Text", 9f, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(175, 70),
            Text = ModSize
        };

        var SBDLSwitch = new SwitchButton
        {
            Name = $"switchButton_{ModName}",
            AutoSize = true,
            Location = new Point(270, 55),
            MinimumSize = new Size(50, 20),
            Size = new Size(45, 45),
            OffBackColor = Color.Gray,
            OffToggleColor = Color.Gainsboro,
            OnBackColor = Color.Gold,
            OnToggleColor = Color.White,
            Checked = isInstalled
        };

        SBDLSwitch.CheckedChanged += async (s, e) =>
        {
            if (!Fonctions.CheckIfModSupportInstalled())
                return;

            if (Process.GetProcessesByName("UpGun-Win64-Shipping").Length != 0)
            {
                Fonctions.ExecuteCmdCommand("taskkill /f /im UpGun-Win64-Shipping.exe");
                MessageBox.Show("UpGun closed!");
            }

            if (SBDLSwitch.Checked)
                Fonctions.InstallMod(ModDLUrl, newZipFileName);
            else
                Fonctions.DeleteMod(newZipFileName);
        };

        panel.Controls.Add(SBDLSwitch);
        panel.Controls.Add(lblType);
        panel.Controls.Add(lblCreator);
        panel.Controls.Add(lblName);
        panel.Controls.Add(lblSize);
        if (modDate > DateTime.Now.AddDays(-7))
        {
            panel.Controls.Add(icnNew);
        }
        panel.Controls.Add(picture);

        flpnlMods.Controls.Add(panel);
        MakeRounded(panel, 15);
        MakeRounded(icnNew, 5);
        MakeRounded(picture, 15);
    }

    private void ChangePage(int delta)
    {
        bool invalidBack = (delta < 0 && currentPage == 0);
        bool invalidForward = (delta > 0 && flpnlMods.Controls.Count < 6);

        if (invalidBack || invalidForward)
        {
            MessageBox.Show("There is no more pages here!");
            return;
        }

        currentPage += delta;

        foreach (Control c in flpnlMods.Controls)
            c.Dispose();
        flpnlMods.Controls.Clear();

        ResetPageNum();
        GetModsFileIds();
    }


    private void PbPreviousPage_Click(object sender, EventArgs e)
    {
        ChangePage(-1);
    }

    private void PbNextPage_Click(object sender, EventArgs e)
    {
        ChangePage(1);
    }

    private void ResetPageNum()
    {
        lblPage.Text = (currentPage + 1).ToString();
    }



    private async void Search(object sender, EventArgs e)
    {
        if (!CanSearch)
        {
            MessageBox.Show("Please wait for the search to complete.");
            return;
        }
        CanSearch = false;
        foreach (Control control in flpnlMods.Controls)
        {
            control.Dispose();
        }
        flpnlMods.Controls.Clear();
        currentPage = 0;
        ResetPageNum();

        GetAllMods(await Fonctions.GetUploadedMods(), tbSearchBar.Text, CbSearchTypes.SelectedItem?.ToString());

        CanSearch = true;
    }

    private void InitializeComponent()
    {
            this.pnlTitle = new System.Windows.Forms.Panel();
            this.BtnClose = new System.Windows.Forms.Button();
            this.lblTitle = new System.Windows.Forms.Label();
            this.pbLogo = new System.Windows.Forms.PictureBox();
            this.BtnReduce = new System.Windows.Forms.Button();
            this.btnNext = new System.Windows.Forms.Button();
            this.lblPage = new System.Windows.Forms.Label();
            this.btnPrevious = new System.Windows.Forms.Button();
            this.pnlPages = new System.Windows.Forms.Panel();
            this.PnlSearch = new System.Windows.Forms.Panel();
            this.tbSearchBar = new System.Windows.Forms.TextBox();
            this.Btn_Refresh = new System.Windows.Forms.Button();
            this.CbSearchTypes = new System.Windows.Forms.ComboBox();
            this.flpnlMods = new System.Windows.Forms.FlowLayoutPanel();
            this.Version = new System.Windows.Forms.Label();
            this.Btn_UpGun = new System.Windows.Forms.Button();
            this.Btn_Discord = new System.Windows.Forms.Button();
            this.pnlTitle.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbLogo)).BeginInit();
            this.PnlSearch.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlTitle
            // 
            this.pnlTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(22)))), ((int)(((byte)(29)))));
            this.pnlTitle.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.pnlTitle.Controls.Add(this.BtnClose);
            this.pnlTitle.Controls.Add(this.lblTitle);
            this.pnlTitle.Controls.Add(this.pbLogo);
            this.pnlTitle.Controls.Add(this.BtnReduce);
            this.pnlTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTitle.Location = new System.Drawing.Point(0, 0);
            this.pnlTitle.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pnlTitle.Name = "pnlTitle";
            this.pnlTitle.Size = new System.Drawing.Size(720, 35);
            this.pnlTitle.TabIndex = 0;
            // 
            // BtnClose
            // 
            this.BtnClose.BackColor = System.Drawing.Color.Transparent;
            this.BtnClose.FlatAppearance.BorderSize = 0;
            this.BtnClose.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Red;
            this.BtnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.BtnClose.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold);
            this.BtnClose.ForeColor = System.Drawing.Color.White;
            this.BtnClose.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.BtnClose.Location = new System.Drawing.Point(685, 0);
            this.BtnClose.Name = "BtnClose";
            this.BtnClose.Size = new System.Drawing.Size(35, 35);
            this.BtnClose.TabIndex = 0;
            this.BtnClose.TabStop = false;
            this.BtnClose.Text = "X";
            this.BtnClose.UseVisualStyleBackColor = false;
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.BackColor = System.Drawing.Color.Transparent;
            this.lblTitle.Font = new System.Drawing.Font("Unispace", 12F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lblTitle.Location = new System.Drawing.Point(61, 9);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(169, 19);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "UpGun Mod Loader";
            // 
            // pbLogo
            // 
            this.pbLogo.Image = global::UpGunModLoader.Properties.Resources.logo93;
            this.pbLogo.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.pbLogo.Location = new System.Drawing.Point(20, 0);
            this.pbLogo.Name = "pbLogo";
            this.pbLogo.Size = new System.Drawing.Size(35, 35);
            this.pbLogo.SizeMode = System.Windows.Forms.PictureBoxSizeMode.StretchImage;
            this.pbLogo.TabIndex = 3;
            this.pbLogo.TabStop = false;
            // 
            // BtnReduce
            // 
            this.BtnReduce.BackColor = System.Drawing.Color.Transparent;
            this.BtnReduce.FlatAppearance.BorderSize = 0;
            this.BtnReduce.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Gray;
            this.BtnReduce.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.BtnReduce.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold);
            this.BtnReduce.ForeColor = System.Drawing.Color.White;
            this.BtnReduce.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.BtnReduce.Location = new System.Drawing.Point(647, 0);
            this.BtnReduce.Name = "BtnReduce";
            this.BtnReduce.Size = new System.Drawing.Size(35, 35);
            this.BtnReduce.TabIndex = 0;
            this.BtnReduce.TabStop = false;
            this.BtnReduce.Text = "—";
            this.BtnReduce.UseVisualStyleBackColor = false;
            // 
            // btnNext
            // 
            this.btnNext.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(22)))), ((int)(((byte)(30)))));
            this.btnNext.FlatAppearance.BorderSize = 0;
            this.btnNext.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNext.Font = new System.Drawing.Font("Unispace", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnNext.ForeColor = System.Drawing.Color.White;
            this.btnNext.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.btnNext.Location = new System.Drawing.Point(378, 420);
            this.btnNext.Name = "btnNext";
            this.btnNext.Size = new System.Drawing.Size(40, 40);
            this.btnNext.TabIndex = 5;
            this.btnNext.Text = ">";
            this.btnNext.UseVisualStyleBackColor = false;
            // 
            // lblPage
            // 
            this.lblPage.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(22)))), ((int)(((byte)(30)))));
            this.lblPage.Font = new System.Drawing.Font("Unispace", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblPage.ForeColor = System.Drawing.Color.White;
            this.lblPage.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lblPage.Location = new System.Drawing.Point(335, 420);
            this.lblPage.Name = "lblPage";
            this.lblPage.Size = new System.Drawing.Size(40, 40);
            this.lblPage.TabIndex = 0;
            this.lblPage.Text = "1";
            this.lblPage.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // btnPrevious
            // 
            this.btnPrevious.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(22)))), ((int)(((byte)(30)))));
            this.btnPrevious.FlatAppearance.BorderSize = 0;
            this.btnPrevious.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPrevious.Font = new System.Drawing.Font("Unispace", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnPrevious.ForeColor = System.Drawing.Color.White;
            this.btnPrevious.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.btnPrevious.Location = new System.Drawing.Point(292, 420);
            this.btnPrevious.Name = "btnPrevious";
            this.btnPrevious.Size = new System.Drawing.Size(40, 40);
            this.btnPrevious.TabIndex = 4;
            this.btnPrevious.Text = "<";
            this.btnPrevious.UseVisualStyleBackColor = false;
            // 
            // pnlPages
            // 
            this.pnlPages.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(22)))), ((int)(((byte)(30)))));
            this.pnlPages.Location = new System.Drawing.Point(292, 420);
            this.pnlPages.Name = "pnlPages";
            this.pnlPages.Size = new System.Drawing.Size(125, 40);
            this.pnlPages.TabIndex = 0;
            // 
            // PnlSearch
            // 
            this.PnlSearch.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(22)))), ((int)(((byte)(30)))));
            this.PnlSearch.Controls.Add(this.tbSearchBar);
            this.PnlSearch.Controls.Add(this.Btn_Refresh);
            this.PnlSearch.Controls.Add(this.CbSearchTypes);
            this.PnlSearch.Location = new System.Drawing.Point(10, 47);
            this.PnlSearch.Name = "PnlSearch";
            this.PnlSearch.Size = new System.Drawing.Size(703, 40);
            this.PnlSearch.TabIndex = 0;
            this.PnlSearch.Paint += new System.Windows.Forms.PaintEventHandler(this.PnlSearch_Paint_1);
            // 
            // tbSearchBar
            // 
            this.tbSearchBar.Font = new System.Drawing.Font("Unispace", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.tbSearchBar.Location = new System.Drawing.Point(197, 11);
            this.tbSearchBar.Name = "tbSearchBar";
            this.tbSearchBar.Size = new System.Drawing.Size(164, 21);
            this.tbSearchBar.TabIndex = 3;
            this.tbSearchBar.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // Btn_Refresh
            // 
            this.Btn_Refresh.BackColor = System.Drawing.Color.Transparent;
            this.Btn_Refresh.Cursor = System.Windows.Forms.Cursors.Hand;
            this.Btn_Refresh.FlatAppearance.BorderSize = 0;
            this.Btn_Refresh.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.Btn_Refresh.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.Btn_Refresh.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.Btn_Refresh.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            this.Btn_Refresh.ForeColor = System.Drawing.Color.White;
            this.Btn_Refresh.Image = global::UpGunModLoader.Properties.Resources.refresh;
            this.Btn_Refresh.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.Btn_Refresh.Location = new System.Drawing.Point(10, 4);
            this.Btn_Refresh.Name = "Btn_Refresh";
            this.Btn_Refresh.Size = new System.Drawing.Size(32, 32);
            this.Btn_Refresh.TabIndex = 1;
            this.Btn_Refresh.UseVisualStyleBackColor = false;
            this.Btn_Refresh.Click += new System.EventHandler(this.Btn_Refresh_Click);
            // 
            // CbSearchTypes
            // 
            this.CbSearchTypes.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(22)))), ((int)(((byte)(30)))));
            this.CbSearchTypes.Cursor = System.Windows.Forms.Cursors.Hand;
            this.CbSearchTypes.DropDownHeight = 125;
            this.CbSearchTypes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.CbSearchTypes.DropDownWidth = 125;
            this.CbSearchTypes.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.CbSearchTypes.Font = new System.Drawing.Font("Unispace", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.CbSearchTypes.ForeColor = System.Drawing.Color.Transparent;
            this.CbSearchTypes.IntegralHeight = false;
            this.CbSearchTypes.Items.AddRange(new object[] {
            "All",
            "Cosmetic",
            "Event",
            "Gameplay",
            "HUD",
            "Lobby Settings",
            "Map",
            "Player Model",
            "Settings",
            "Skin"});
            this.CbSearchTypes.Location = new System.Drawing.Point(55, 10);
            this.CbSearchTypes.Name = "CbSearchTypes";
            this.CbSearchTypes.Size = new System.Drawing.Size(121, 22);
            this.CbSearchTypes.TabIndex = 2;
            this.CbSearchTypes.SelectedIndexChanged += new System.EventHandler(this.CbSearchTypes_SelectedIndexChanged);
            // 
            // flpnlMods
            // 
            this.flpnlMods.Location = new System.Drawing.Point(10, 100);
            this.flpnlMods.Name = "flpnlMods";
            this.flpnlMods.Padding = new System.Windows.Forms.Padding(5);
            this.flpnlMods.Size = new System.Drawing.Size(703, 315);
            this.flpnlMods.TabIndex = 0;
            // 
            // Version
            // 
            this.Version.AutoSize = true;
            this.Version.BackColor = System.Drawing.Color.Transparent;
            this.Version.Font = new System.Drawing.Font("Unispace", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.Version.ForeColor = System.Drawing.Color.White;
            this.Version.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.Version.Location = new System.Drawing.Point(20, 435);
            this.Version.Name = "Version";
            this.Version.Size = new System.Drawing.Size(63, 14);
            this.Version.TabIndex = 0;
            this.Version.Text = "V1.1.0.6";
            // 
            // Btn_UpGun
            // 
            this.Btn_UpGun.BackColor = System.Drawing.Color.Transparent;
            this.Btn_UpGun.Cursor = System.Windows.Forms.Cursors.Hand;
            this.Btn_UpGun.FlatAppearance.BorderSize = 0;
            this.Btn_UpGun.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.Btn_UpGun.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.Btn_UpGun.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.Btn_UpGun.ForeColor = System.Drawing.Color.White;
            this.Btn_UpGun.Image = global::UpGunModLoader.Properties.Resources.logo_upgun;
            this.Btn_UpGun.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.Btn_UpGun.Location = new System.Drawing.Point(617, 421);
            this.Btn_UpGun.Name = "Btn_UpGun";
            this.Btn_UpGun.Size = new System.Drawing.Size(40, 40);
            this.Btn_UpGun.TabIndex = 6;
            this.Btn_UpGun.UseVisualStyleBackColor = false;
            this.Btn_UpGun.Click += new System.EventHandler(this.Btn_UpGun_Click);
            // 
            // Btn_Discord
            // 
            this.Btn_Discord.BackColor = System.Drawing.Color.Transparent;
            this.Btn_Discord.Cursor = System.Windows.Forms.Cursors.Hand;
            this.Btn_Discord.FlatAppearance.BorderSize = 0;
            this.Btn_Discord.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.Btn_Discord.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.Btn_Discord.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.Btn_Discord.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            this.Btn_Discord.ForeColor = System.Drawing.Color.White;
            this.Btn_Discord.Image = global::UpGunModLoader.Properties.Resources.DiscordLogo;
            this.Btn_Discord.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.Btn_Discord.Location = new System.Drawing.Point(663, 421);
            this.Btn_Discord.Name = "Btn_Discord";
            this.Btn_Discord.Size = new System.Drawing.Size(40, 40);
            this.Btn_Discord.TabIndex = 7;
            this.Btn_Discord.UseVisualStyleBackColor = false;
            this.Btn_Discord.Click += new System.EventHandler(this.Btn_Discord_Click);
            // 
            // UpGun_Mod_Loader
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(14)))), ((int)(((byte)(17)))), ((int)(((byte)(27)))));
            this.ClientSize = new System.Drawing.Size(720, 470);
            this.Controls.Add(this.Version);
            this.Controls.Add(this.lblPage);
            this.Controls.Add(this.Btn_UpGun);
            this.Controls.Add(this.Btn_Discord);
            this.Controls.Add(this.flpnlMods);
            this.Controls.Add(this.btnPrevious);
            this.Controls.Add(this.btnNext);
            this.Controls.Add(this.pnlPages);
            this.Controls.Add(this.pnlTitle);
            this.Controls.Add(this.PnlSearch);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximumSize = new System.Drawing.Size(720, 470);
            this.MinimumSize = new System.Drawing.Size(720, 470);
            this.Name = "UpGun_Mod_Loader";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Mod Loader";
            this.pnlTitle.ResumeLayout(false);
            this.pnlTitle.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbLogo)).EndInit();
            this.PnlSearch.ResumeLayout(false);
            this.PnlSearch.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

    }

    private void Btn_Discord_Click(object sender, EventArgs e)
    {
        Process.Start("https://discord.gg/9VKrCEbyAV");
    }

    private void Btn_Refresh_Click(object sender, EventArgs e)
    {
        Search(sender, e);
    }

    private void Btn_UpGun_Click(object sender, EventArgs e)
    {
        Process.Start("steam://rungameid/1575870");
    }

    private void PnlSearch_Paint_1(object sender, PaintEventArgs e)
    {

    }

    private void CbSearchTypes_SelectedIndexChanged(object sender, EventArgs e)
    {

    }
}