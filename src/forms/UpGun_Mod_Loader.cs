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
    private Panel pnlTitleText;
    private Label lblTitle;
    private Panel pnlClose;
    private Button BtnReduce;
    private Button BtnClose;
    private PictureBox pbLogo;
    private Button btnNext;
    private Label lblPage;
    private Button btnPrevious;
    private Panel pnlPages;
    private Panel pnlSearch;
    private DarkComboBox cbSearchTypes;
    private TextBox tbSearchBar;
    private FlowLayoutPanel flpnlMods;
    private Button Btn_Discord;
    private Button Btn_Refresh;
    private Button Btn_UpGun;
    private TextBox textBox1;
    private Label label1;
    private bool CanSearch = true;

    public UpGun_Mod_Loader()
    {
        if (mutex.WaitOne(TimeSpan.Zero, exitContext: true))
        {
            InitializeComponent();

            BtnClose.Click += BtnClose_Click;
            BtnReduce.Click += BtnReduce_Click;
            pnlTitleText.MouseDown += TitleDrag_MouseDown;
            pnlTitle.MouseDown += TitleDrag_MouseDown;
            lblTitle.MouseDown += TitleDrag_MouseDown;
            pbLogo.MouseDown += TitleDrag_MouseDown;
            btnNext.Click += PbNextPage_Click;
            btnPrevious.Click += PbPreviousPage_Click;
            cbSearchTypes.SelectedIndexChanged += Search;
            tbSearchBar.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                {
                    e.SuppressKeyPress = true;
                    Search(s, e);
                }
            };

            int radius = 18;

            MakeRounded(pnlSearch, radius);
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

            pnlSearch.Paint += PnlSearch_Paint;

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
        tbSearchBar.ForeColor = Color.Gray;
        tbSearchBar.Text = "Search by name";

        tbSearchBar.GotFocus += (s, e) =>
        {
            if (tbSearchBar.Text == "Search by name")
            {
                tbSearchBar.Text = "";
                tbSearchBar.ForeColor = Color.White;
            }
        };

        tbSearchBar.LostFocus += (s, e) =>
        {
            if (string.IsNullOrWhiteSpace(tbSearchBar.Text))
            {
                tbSearchBar.Text = "Search by name";
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
        GetAllMods(await Fonctions.GetUploadedMods(), tbSearchBar.Text, cbSearchTypes.SelectedItem?.ToString());
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
            Font = new Font("Myanmar Text", 12f, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(195, 10),
            Text = ModName
        };

        var lblCreator = new Label
        {
            Name = $"lblCreator_{ModName}",
            AutoSize = true,
            Font = new Font("Myanmar Text", 9f, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(195, 35),
            Text = OwnerName
        };

        var lblType = new Label
        {
            Name = $"lblType_{ModName}",
            AutoSize = true,
            Font = new Font("Myanmar Text", 9f, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(195, 75),
            Text = ModType
        };

        var lblSize = new Label
        {
            Name = $"lblSize_{ModName}",
            AutoSize = true,
            Font = new Font("Myanmar Text", 9f, FontStyle.Bold),
            ForeColor = Color.White,
            Location = new Point(195, 55),
            Text = ModSize
        };

        var SBDLSwitch = new SwitchButton
        {
            Name = $"switchButton_{ModName}",
            AutoSize = true,
            Location = new Point(270, 50),
            MinimumSize = new Size(45, 22),
            Size = new Size(45, 22),
            OffBackColor = Color.Gray,
            OffToggleColor = Color.Gainsboro,
            OnBackColor = Color.FromArgb(0, 192, 0),
            OnToggleColor = Color.WhiteSmoke,
            Checked = isInstalled
        };

        SBDLSwitch.CheckedChanged += (s, e) =>
        {
            if (!Fonctions.CheckIfModSupportInstalled())
                return;

            if (Process.GetProcessesByName("UpGun-Win64-Shipping").Length != 0)
            {
                Fonctions.ExecuteCmdCommand("taskkill /f /im UpGun-Win64-Shipping.exe");
                MessageBox.Show("UpGun.exe closed!");
            }

            Thread.Sleep(300);

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
        MakeRounded(panel, 18);
        MakeRounded(icnNew, 10);
        MakeRounded(picture, 18);
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

        GetAllMods(await Fonctions.GetUploadedMods(), tbSearchBar.Text, cbSearchTypes.SelectedItem?.ToString());

        CanSearch = true;
    }

    private void InitializeComponent()
    {
            this.pnlTitle = new System.Windows.Forms.Panel();
            this.pbLogo = new System.Windows.Forms.PictureBox();
            this.BtnReduce = new System.Windows.Forms.Button();
            this.pnlClose = new System.Windows.Forms.Panel();
            this.BtnClose = new System.Windows.Forms.Button();
            this.pnlTitleText = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.btnNext = new System.Windows.Forms.Button();
            this.lblPage = new System.Windows.Forms.Label();
            this.btnPrevious = new System.Windows.Forms.Button();
            this.pnlPages = new System.Windows.Forms.Panel();
            this.pnlSearch = new System.Windows.Forms.Panel();
            this.tbSearchBar = new System.Windows.Forms.TextBox();
            this.Btn_Refresh = new System.Windows.Forms.Button();
            this.flpnlMods = new System.Windows.Forms.FlowLayoutPanel();
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.Btn_UpGun = new System.Windows.Forms.Button();
            this.Btn_Discord = new System.Windows.Forms.Button();
            this.cbSearchTypes = new DarkComboBox();
            this.pnlTitle.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.pbLogo)).BeginInit();
            this.pnlClose.SuspendLayout();
            this.pnlTitleText.SuspendLayout();
            this.pnlSearch.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlTitle
            // 
            this.pnlTitle.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(22)))), ((int)(((byte)(29)))));
            this.pnlTitle.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.pnlTitle.Controls.Add(this.pbLogo);
            this.pnlTitle.Controls.Add(this.BtnReduce);
            this.pnlTitle.Controls.Add(this.pnlClose);
            this.pnlTitle.Controls.Add(this.pnlTitleText);
            this.pnlTitle.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlTitle.Location = new System.Drawing.Point(0, 0);
            this.pnlTitle.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pnlTitle.Name = "pnlTitle";
            this.pnlTitle.Size = new System.Drawing.Size(720, 35);
            this.pnlTitle.TabIndex = 0;
            // 
            // pbLogo
            // 
            this.pbLogo.Image = global::UpGunModLoader.Properties.Resources.logo93;
            this.pbLogo.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.pbLogo.Location = new System.Drawing.Point(20, 3);
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
            this.BtnReduce.Location = new System.Drawing.Point(649, 0);
            this.BtnReduce.Name = "BtnReduce";
            this.BtnReduce.Size = new System.Drawing.Size(35, 35);
            this.BtnReduce.TabIndex = 1;
            this.BtnReduce.Text = "—";
            this.BtnReduce.UseVisualStyleBackColor = false;
            // 
            // pnlClose
            // 
            this.pnlClose.Controls.Add(this.BtnClose);
            this.pnlClose.Location = new System.Drawing.Point(685, 0);
            this.pnlClose.Name = "pnlClose";
            this.pnlClose.Size = new System.Drawing.Size(35, 35);
            this.pnlClose.TabIndex = 2;
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
            this.BtnClose.Location = new System.Drawing.Point(0, 0);
            this.BtnClose.Name = "BtnClose";
            this.BtnClose.Size = new System.Drawing.Size(35, 35);
            this.BtnClose.TabIndex = 0;
            this.BtnClose.Text = "X";
            this.BtnClose.UseVisualStyleBackColor = false;
            // 
            // pnlTitleText
            // 
            this.pnlTitleText.Controls.Add(this.lblTitle);
            this.pnlTitleText.Location = new System.Drawing.Point(40, 0);
            this.pnlTitleText.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.pnlTitleText.Name = "pnlTitleText";
            this.pnlTitleText.Size = new System.Drawing.Size(200, 35);
            this.pnlTitleText.TabIndex = 1;
            // 
            // lblTitle
            // 
            this.lblTitle.AutoSize = true;
            this.lblTitle.BackColor = System.Drawing.Color.Transparent;
            this.lblTitle.Font = new System.Drawing.Font("Clickuper", 12F, System.Drawing.FontStyle.Bold);
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lblTitle.Location = new System.Drawing.Point(21, 5);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(187, 29);
            this.lblTitle.TabIndex = 0;
            this.lblTitle.Text = "UpGun Mod Loader";
            this.lblTitle.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.lblTitle.UseMnemonic = false;
            // 
            // btnNext
            // 
            this.btnNext.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(22)))), ((int)(((byte)(30)))));
            this.btnNext.FlatAppearance.BorderSize = 0;
            this.btnNext.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnNext.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            this.btnNext.ForeColor = System.Drawing.Color.White;
            this.btnNext.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.btnNext.Location = new System.Drawing.Point(377, 419);
            this.btnNext.Name = "btnNext";
            this.btnNext.Size = new System.Drawing.Size(40, 40);
            this.btnNext.TabIndex = 1;
            this.btnNext.Text = ">";
            this.btnNext.UseVisualStyleBackColor = false;
            // 
            // lblPage
            // 
            this.lblPage.AutoSize = true;
            this.lblPage.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(22)))), ((int)(((byte)(30)))));
            this.lblPage.Font = new System.Drawing.Font("Myanmar Text", 12F, System.Drawing.FontStyle.Bold);
            this.lblPage.ForeColor = System.Drawing.Color.White;
            this.lblPage.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.lblPage.Location = new System.Drawing.Point(349, 428);
            this.lblPage.Name = "lblPage";
            this.lblPage.Size = new System.Drawing.Size(22, 29);
            this.lblPage.TabIndex = 2;
            this.lblPage.Text = "1";
            this.lblPage.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            // 
            // btnPrevious
            // 
            this.btnPrevious.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(22)))), ((int)(((byte)(30)))));
            this.btnPrevious.FlatAppearance.BorderSize = 0;
            this.btnPrevious.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnPrevious.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold);
            this.btnPrevious.ForeColor = System.Drawing.Color.White;
            this.btnPrevious.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.btnPrevious.Location = new System.Drawing.Point(303, 419);
            this.btnPrevious.Name = "btnPrevious";
            this.btnPrevious.Size = new System.Drawing.Size(40, 40);
            this.btnPrevious.TabIndex = 3;
            this.btnPrevious.Text = "<";
            this.btnPrevious.UseVisualStyleBackColor = false;
            // 
            // pnlPages
            // 
            this.pnlPages.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(22)))), ((int)(((byte)(30)))));
            this.pnlPages.Location = new System.Drawing.Point(303, 419);
            this.pnlPages.Name = "pnlPages";
            this.pnlPages.Size = new System.Drawing.Size(114, 40);
            this.pnlPages.TabIndex = 4;
            // 
            // pnlSearch
            // 
            this.pnlSearch.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(22)))), ((int)(((byte)(30)))));
            this.pnlSearch.Controls.Add(this.cbSearchTypes);
            this.pnlSearch.Controls.Add(this.tbSearchBar);
            this.pnlSearch.Controls.Add(this.Btn_Refresh);
            this.pnlSearch.Location = new System.Drawing.Point(10, 50);
            this.pnlSearch.Name = "pnlSearch";
            this.pnlSearch.Size = new System.Drawing.Size(378, 40);
            this.pnlSearch.TabIndex = 5;
            // 
            // tbSearchBar
            // 
            this.tbSearchBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(22)))), ((int)(((byte)(30)))));
            this.tbSearchBar.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.tbSearchBar.Font = new System.Drawing.Font("Myanmar Text", 8.25F, System.Drawing.FontStyle.Bold);
            this.tbSearchBar.ForeColor = System.Drawing.Color.White;
            this.tbSearchBar.Location = new System.Drawing.Point(216, 10);
            this.tbSearchBar.Name = "tbSearchBar";
            this.tbSearchBar.Size = new System.Drawing.Size(151, 21);
            this.tbSearchBar.TabIndex = 1;
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
            this.Btn_Refresh.TabIndex = 8;
            this.Btn_Refresh.UseVisualStyleBackColor = false;
            this.Btn_Refresh.Click += new System.EventHandler(this.Btn_Refresh_Click);
            // 
            // flpnlMods
            // 
            this.flpnlMods.Location = new System.Drawing.Point(10, 97);
            this.flpnlMods.Name = "flpnlMods";
            this.flpnlMods.Padding = new System.Windows.Forms.Padding(5);
            this.flpnlMods.Size = new System.Drawing.Size(700, 316);
            this.flpnlMods.TabIndex = 6;
            // 
            // textBox1
            // 
            this.textBox1.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(14)))), ((int)(((byte)(17)))), ((int)(((byte)(27)))));
            this.textBox1.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.textBox1.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.textBox1.Enabled = false;
            this.textBox1.Font = new System.Drawing.Font("Riffic Free Medium", 8.25F, System.Drawing.FontStyle.Bold);
            this.textBox1.ForeColor = System.Drawing.Color.Transparent;
            this.textBox1.Location = new System.Drawing.Point(10, 441);
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.ShortcutsEnabled = false;
            this.textBox1.Size = new System.Drawing.Size(100, 14);
            this.textBox1.TabIndex = 9;
            this.textBox1.Text = "v1.1";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Font = new System.Drawing.Font("Clickuper", 12F, System.Drawing.FontStyle.Bold);
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.ImeMode = System.Windows.Forms.ImeMode.NoControl;
            this.label1.Location = new System.Drawing.Point(394, 54);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(162, 29);
            this.label1.TabIndex = 1;
            this.label1.Text = "V2 coming soon";
            this.label1.TextAlign = System.Drawing.ContentAlignment.BottomCenter;
            this.label1.UseMnemonic = false;
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
            this.Btn_UpGun.TabIndex = 8;
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
            // cbSearchTypes
            // 
            this.cbSearchTypes.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(22)))), ((int)(((byte)(30)))));
            this.cbSearchTypes.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(65)))), ((int)(((byte)(75)))));
            this.cbSearchTypes.ButtonWidth = 24;
            this.cbSearchTypes.DesiredHeight = 28;
            this.cbSearchTypes.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cbSearchTypes.DropDownHeight = 176;
            this.cbSearchTypes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbSearchTypes.DropDownWidth = 160;
            this.cbSearchTypes.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(22)))), ((int)(((byte)(30)))));
            this.cbSearchTypes.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cbSearchTypes.Font = new System.Drawing.Font("Myanmar Text", 8.25F, System.Drawing.FontStyle.Bold);
            this.cbSearchTypes.ForeColor = System.Drawing.Color.White;
            this.cbSearchTypes.FormattingEnabled = true;
            this.cbSearchTypes.HoverColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(38)))), ((int)(((byte)(48)))));
            this.cbSearchTypes.IntegralHeight = false;
            this.cbSearchTypes.ItemHeight = 22;
            this.cbSearchTypes.Items.AddRange(new object[] {
            "All",
            "Map",
            "Gun Skin",
            "Cosmetic",
            "Player Model",
            "Settings",
            "Lobby Settings",
            "HUD",
            "Font",
            "Event"});
            this.cbSearchTypes.Location = new System.Drawing.Point(48, 5);
            this.cbSearchTypes.Name = "cbSearchTypes";
            this.cbSearchTypes.Radius = 6;
            this.cbSearchTypes.Size = new System.Drawing.Size(160, 28);
            this.cbSearchTypes.TabIndex = 2;
            this.cbSearchTypes.TextColor = System.Drawing.Color.White;
            // 
            // UpGun_Mod_Loader
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(14)))), ((int)(((byte)(17)))), ((int)(((byte)(27)))));
            this.ClientSize = new System.Drawing.Size(720, 470);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.Btn_UpGun);
            this.Controls.Add(this.Btn_Discord);
            this.Controls.Add(this.flpnlMods);
            this.Controls.Add(this.btnPrevious);
            this.Controls.Add(this.lblPage);
            this.Controls.Add(this.btnNext);
            this.Controls.Add(this.pnlPages);
            this.Controls.Add(this.pnlTitle);
            this.Controls.Add(this.pnlSearch);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximumSize = new System.Drawing.Size(720, 470);
            this.MinimumSize = new System.Drawing.Size(720, 470);
            this.Name = "UpGun_Mod_Loader";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Mod Loader";
            this.pnlTitle.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pbLogo)).EndInit();
            this.pnlClose.ResumeLayout(false);
            this.pnlTitleText.ResumeLayout(false);
            this.pnlTitleText.PerformLayout();
            this.pnlSearch.ResumeLayout(false);
            this.pnlSearch.PerformLayout();
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
}