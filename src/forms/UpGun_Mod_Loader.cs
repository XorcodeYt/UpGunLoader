using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

public class UpGun_Mod_Loader : Form
{
    [DllImport("user32.dll")] private static extern bool ReleaseCapture();
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, int lParam);
    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HTCAPTION = 0x2;


    private static Mutex mutex = new Mutex(initiallyOwned: true, "{E3D2A813-62E2-4DDF-9E91-38C20EC890D6}");

	private int currentPage;

	private const int itemsPerPage = 6;
    private Panel pnlTitle;
    private Panel pnlTitleText;
    private Label lblTitle;
    private Panel pnlClose;
    private Button btnReduce;
    private Button btnClose;
    private PictureBox pbLogo;
    private Button btnNext;
    private Label lblPage;
    private Button btnPrevious;
    private Panel pnlPages;
    private Panel pnlSearch;
    private DarkComboBox cbSearchTypes;
    private TextBox tbSearchBar;
    private FlowLayoutPanel flpnlMods;
    private IContainer components;
    private Button button1;
    private Button button3;
    private bool CanSearch = true;

    public UpGun_Mod_Loader()
	{
		if (mutex.WaitOne(TimeSpan.Zero, exitContext: true))
		{
			InitializeComponent();

            btnClose.Click += btnClose_Click;
            btnReduce.Click += btnReduce_Click;
			pnlTitleText.MouseDown += TitleDrag_MouseDown;
            pnlTitle.MouseDown += TitleDrag_MouseDown;
            lblTitle.MouseDown += TitleDrag_MouseDown;
            pbLogo.MouseDown += TitleDrag_MouseDown;
            btnNext.Click += pbNextPage_Click;
            btnPrevious.Click += pbPreviousPage_Click;
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

            pnlSearch.Paint += pnlSearch_Paint;

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
                Fonctions.CheckLoaderUpdate();

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


    private void pnlSearch_Paint(object sender, PaintEventArgs e)
    {
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        Color border = Color.FromArgb(60, 65, 75);
        Color fill = Color.FromArgb(20, 22, 30);

        var tbRect = tbSearchBar.Bounds;
        tbRect.Inflate(2, 2);
        using (var path = GetRoundedPath(tbRect, 6))
        using (var pen = new Pen(border, 1))
        using (var brush = new SolidBrush(fill))
        {
            e.Graphics.FillPath(brush, path);
            e.Graphics.DrawPath(pen, path);
        }
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
        GraphicsPath path = new GraphicsPath();
        int d = radius * 2;

        path.StartFigure();
        path.AddArc(bounds.X, bounds.Y, d, d, 180, 90);
        path.AddArc(bounds.Right - d, bounds.Y, d, d, 270, 90);
        path.AddArc(bounds.Right - d, bounds.Bottom - d, d, d, 0, 90);
        path.AddArc(bounds.X, bounds.Bottom - d, d, d, 90, 90);
        path.CloseFigure();

        ctrl.Region = new Region(path);
    }

    private void btnClose_Click(object sender, EventArgs e)
    {
        Close();
    }

    private void btnReduce_Click(object sender, EventArgs e)
    {
        WindowState = FormWindowState.Minimized;
    }

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
        string[] array = allMods.Split(new string[2] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
		List<string[]> list = new List<string[]>();
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

        try { 
            modDate = DateTime.ParseExact(Date, "dd/MM/yyyy", CultureInfo.InvariantCulture);
        }
        catch {
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
            Image = global::Properties.Resources.New_icon,
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
            if (SBDLSwitch.Checked)
            {
                if (Fonctions.CheckIfModSupportInstalled())
                {
                    if (Process.GetProcessesByName("UpGun-Win64-Shipping").Length != 0)
                    {
                        Fonctions.ExecuteCmdCommand("taskkill /f /im UpGun-Win64-Shipping.exe");
                        MessageBox.Show("UpGun.exe closed!");
                    }
                    Thread.Sleep(300);
                    Fonctions.InstallMod(ModDLUrl, newZipFileName);
                }
            }
            else
            {
                if (Fonctions.CheckIfModSupportInstalled())
                {
                    if (Process.GetProcessesByName("UpGun-Win64-Shipping").Length != 0)
                    {
                        Fonctions.ExecuteCmdCommand("taskkill /f /im UpGun-Win64-Shipping.exe");
                        MessageBox.Show("UpGun.exe closed!");
                    }
                    Thread.Sleep(300);
                    Fonctions.DeleteMod(newZipFileName);
                }
            }
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

    private void pbPreviousPage_Click(object sender, EventArgs e)
	{
		if (currentPage != 0)
		{
			currentPage--;
			foreach (Control control in flpnlMods.Controls)
			{
				control.Dispose();
			}
			flpnlMods.Controls.Clear();
			ResetPageNum();
			GetModsFileIds();
		}
		else
		{
			MessageBox.Show("There is no more pages here!");
		}
	}

	private void pbNextPage_Click(object sender, EventArgs e)
	{
		int num = 0;
		foreach (Control control in flpnlMods.Controls)
		{
			_ = control;
			num++;
		}
		if (num < 6)
		{
			MessageBox.Show("There is no more pages here!");
			return;
		}
		currentPage++;
		foreach (Control control2 in flpnlMods.Controls)
		{
			control2.Dispose();
		}
		flpnlMods.Controls.Clear();
		ResetPageNum();
		GetModsFileIds();
	}

	private void ResetPageNum()
	{
		int num = currentPage + 1;
		lblPage.Text = num.ToString();
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(UpGun_Mod_Loader));
            this.pnlTitle = new System.Windows.Forms.Panel();
            this.pbLogo = new System.Windows.Forms.PictureBox();
            this.btnReduce = new System.Windows.Forms.Button();
            this.pnlClose = new System.Windows.Forms.Panel();
            this.btnClose = new System.Windows.Forms.Button();
            this.pnlTitleText = new System.Windows.Forms.Panel();
            this.lblTitle = new System.Windows.Forms.Label();
            this.btnNext = new System.Windows.Forms.Button();
            this.lblPage = new System.Windows.Forms.Label();
            this.btnPrevious = new System.Windows.Forms.Button();
            this.pnlPages = new System.Windows.Forms.Panel();
            this.pnlSearch = new System.Windows.Forms.Panel();
            this.tbSearchBar = new System.Windows.Forms.TextBox();
            this.flpnlMods = new System.Windows.Forms.FlowLayoutPanel();
            this.button1 = new System.Windows.Forms.Button();
            this.button3 = new System.Windows.Forms.Button();
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
            this.pnlTitle.Controls.Add(this.btnReduce);
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
            this.pbLogo.Image = global::Properties.Resources.logo_30;
            this.pbLogo.Location = new System.Drawing.Point(20, 3);
            this.pbLogo.Name = "pbLogo";
            this.pbLogo.Size = new System.Drawing.Size(35, 35);
            this.pbLogo.TabIndex = 3;
            this.pbLogo.TabStop = false;
            // 
            // btnReduce
            // 
            this.btnReduce.BackColor = System.Drawing.Color.Transparent;
            this.btnReduce.FlatAppearance.BorderSize = 0;
            this.btnReduce.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Gray;
            this.btnReduce.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnReduce.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnReduce.ForeColor = System.Drawing.Color.White;
            this.btnReduce.Location = new System.Drawing.Point(649, 0);
            this.btnReduce.Name = "btnReduce";
            this.btnReduce.Size = new System.Drawing.Size(35, 35);
            this.btnReduce.TabIndex = 1;
            this.btnReduce.Text = "—";
            this.btnReduce.UseVisualStyleBackColor = false;
            // 
            // pnlClose
            // 
            this.pnlClose.Controls.Add(this.btnClose);
            this.pnlClose.Location = new System.Drawing.Point(685, 0);
            this.pnlClose.Name = "pnlClose";
            this.pnlClose.Size = new System.Drawing.Size(35, 35);
            this.pnlClose.TabIndex = 2;
            // 
            // btnClose
            // 
            this.btnClose.BackColor = System.Drawing.Color.Transparent;
            this.btnClose.FlatAppearance.BorderSize = 0;
            this.btnClose.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Red;
            this.btnClose.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.btnClose.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnClose.ForeColor = System.Drawing.Color.White;
            this.btnClose.Location = new System.Drawing.Point(0, 0);
            this.btnClose.Name = "btnClose";
            this.btnClose.Size = new System.Drawing.Size(35, 35);
            this.btnClose.TabIndex = 0;
            this.btnClose.Text = "X";
            this.btnClose.UseVisualStyleBackColor = false;
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
            this.lblTitle.BackColor = System.Drawing.Color.Transparent;
            this.lblTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblTitle.Font = new System.Drawing.Font("Myanmar Text", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblTitle.ForeColor = System.Drawing.Color.White;
            this.lblTitle.Location = new System.Drawing.Point(0, 0);
            this.lblTitle.Margin = new System.Windows.Forms.Padding(0);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(200, 35);
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
            this.btnNext.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnNext.ForeColor = System.Drawing.Color.White;
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
            this.lblPage.Location = new System.Drawing.Point(349, 430);
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
            this.btnPrevious.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnPrevious.ForeColor = System.Drawing.Color.White;
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
            this.pnlSearch.Controls.Add(this.button3);
            this.pnlSearch.Controls.Add(this.tbSearchBar);
            this.pnlSearch.Controls.Add(this.cbSearchTypes);
            this.pnlSearch.Location = new System.Drawing.Point(160, 50);
            this.pnlSearch.Name = "pnlSearch";
            this.pnlSearch.Size = new System.Drawing.Size(399, 40);
            this.pnlSearch.TabIndex = 5;
            // 
            // tbSearchBar
            // 
            this.tbSearchBar.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(22)))), ((int)(((byte)(30)))));
            this.tbSearchBar.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.tbSearchBar.Font = new System.Drawing.Font("Myanmar Text", 8.25F, System.Drawing.FontStyle.Bold);
            this.tbSearchBar.ForeColor = System.Drawing.Color.White;
            this.tbSearchBar.Location = new System.Drawing.Point(192, 11);
            this.tbSearchBar.Name = "tbSearchBar";
            this.tbSearchBar.Size = new System.Drawing.Size(151, 21);
            this.tbSearchBar.TabIndex = 1;
            this.tbSearchBar.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // flpnlMods
            // 
            this.flpnlMods.Location = new System.Drawing.Point(10, 97);
            this.flpnlMods.Name = "flpnlMods";
            this.flpnlMods.Padding = new System.Windows.Forms.Padding(5);
            this.flpnlMods.Size = new System.Drawing.Size(700, 316);
            this.flpnlMods.TabIndex = 6;
            // 
            // button1
            // 
            this.button1.BackColor = System.Drawing.Color.Transparent;
            this.button1.FlatAppearance.BorderSize = 0;
            this.button1.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.button1.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.ForeColor = System.Drawing.Color.White;
            this.button1.Image = global::Properties.Resources.DiscordLogo;
            this.button1.Location = new System.Drawing.Point(663, 421);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(40, 40);
            this.button1.TabIndex = 7;
            this.button1.UseVisualStyleBackColor = false;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // button3
            // 
            this.button3.BackColor = System.Drawing.Color.Transparent;
            this.button3.FlatAppearance.BorderSize = 0;
            this.button3.FlatAppearance.MouseDownBackColor = System.Drawing.Color.Transparent;
            this.button3.FlatAppearance.MouseOverBackColor = System.Drawing.Color.Transparent;
            this.button3.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button3.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button3.ForeColor = System.Drawing.Color.White;
        this.button3.Image = global::Properties.Resources.refresh;
            this.button3.Location = new System.Drawing.Point(356, 8);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(25, 25);
            this.button3.TabIndex = 8;
            this.button3.UseVisualStyleBackColor = false;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // cbSearchTypes
            // 
            this.cbSearchTypes.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(22)))), ((int)(((byte)(30)))));
            this.cbSearchTypes.BorderColor = System.Drawing.Color.FromArgb(((int)(((byte)(60)))), ((int)(((byte)(65)))), ((int)(((byte)(75)))));
            this.cbSearchTypes.ButtonWidth = 24;
            this.cbSearchTypes.DesiredHeight = 29;
            this.cbSearchTypes.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.cbSearchTypes.DropDownHeight = 184;
            this.cbSearchTypes.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbSearchTypes.DropDownWidth = 160;
            this.cbSearchTypes.FillColor = System.Drawing.Color.FromArgb(((int)(((byte)(20)))), ((int)(((byte)(22)))), ((int)(((byte)(30)))));
            this.cbSearchTypes.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.cbSearchTypes.Font = new System.Drawing.Font("Myanmar Text", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbSearchTypes.ForeColor = System.Drawing.Color.White;
            this.cbSearchTypes.FormattingEnabled = true;
            this.cbSearchTypes.HoverColor = System.Drawing.Color.FromArgb(((int)(((byte)(35)))), ((int)(((byte)(38)))), ((int)(((byte)(48)))));
            this.cbSearchTypes.IntegralHeight = false;
            this.cbSearchTypes.ItemHeight = 23;
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
            this.cbSearchTypes.SelectedIndex = 0;
            this.cbSearchTypes.Location = new System.Drawing.Point(14, 6);
            this.cbSearchTypes.Name = "cbSearchTypes";
            this.cbSearchTypes.Radius = 6;
            this.cbSearchTypes.Size = new System.Drawing.Size(160, 29);
            this.cbSearchTypes.TabIndex = 2;
            this.cbSearchTypes.TextColor = System.Drawing.Color.White;
            // 
            // UpGun_Mod_Loader
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(14)))), ((int)(((byte)(17)))), ((int)(((byte)(27)))));
            this.ClientSize = new System.Drawing.Size(720, 470);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.flpnlMods);
            this.Controls.Add(this.pnlSearch);
            this.Controls.Add(this.btnPrevious);
            this.Controls.Add(this.lblPage);
            this.Controls.Add(this.btnNext);
            this.Controls.Add(this.pnlPages);
            this.Controls.Add(this.pnlTitle);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.MaximumSize = new System.Drawing.Size(720, 470);
            this.MinimumSize = new System.Drawing.Size(720, 470);
            this.Name = "UpGun_Mod_Loader";
            this.Text = "Mod Loader";
            this.pnlTitle.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.pbLogo)).EndInit();
            this.pnlClose.ResumeLayout(false);
            this.pnlTitleText.ResumeLayout(false);
            this.pnlSearch.ResumeLayout(false);
            this.pnlSearch.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

	}

    private void button1_Click(object sender, EventArgs e)
    {
        Process.Start(
            new ProcessStartInfo
            {
                FileName = "https://discord.gg/9VKrCEbyAV",
                UseShellExecute = true
            }
        );
    }
    private void button3_Click(object sender, EventArgs e)
    {
        Search(sender, e);
    }
}