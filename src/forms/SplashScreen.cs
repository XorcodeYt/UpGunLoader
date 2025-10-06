using System;
using System.Drawing;
using System.Windows.Forms;

public class SplashScreen : Form
{
    private readonly Timer _timer;
    private PictureBox pictureBox1;
    private Label label1;
    private int _dotCount = 0;
    private string _baseStatusText = "";

    public SplashScreen()
    {
        InitializeComponent();

        _baseStatusText = label1.Text;

        _timer = new Timer { Interval = 400 };
        _timer.Tick += (s, e) =>
        {
            label1.Text = _baseStatusText + new string('.', _dotCount);
            _dotCount = (_dotCount + 1) % 4;
        };
        _timer.Start();
    }

    public void SetStatus(string text)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action<string>(SetStatus), text);
            return;
        }

        label1.Location = new Point(label1.Location.X, 60);

        if (text.Length > 26)
        {
            string firstLine = text.Substring(0, 26);
            string secondLine = text.Substring(26);

            label1.Location = new Point(label1.Location.X, 40);
            _baseStatusText = firstLine + Environment.NewLine + secondLine;
        }
        else
        {
            _baseStatusText = text;
        }

        _dotCount = 0;
        label1.Text = _baseStatusText;
    }

    protected override bool ShowWithoutActivation => true;

    private void InitializeComponent()
    {
            this.label1 = new System.Windows.Forms.Label();
            this.pictureBox1 = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Myanmar Text", 12F, System.Drawing.FontStyle.Bold);
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(147, 65);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(158, 29);
            this.label1.TabIndex = 1;
            this.label1.Text = "Operation en cours";
            // 
            // pictureBox1
            // 
            this.pictureBox1.Image = global::Properties.Resources.logo93;
            this.pictureBox1.Location = new System.Drawing.Point(24, 24);
            this.pictureBox1.Name = "pictureBox1";
            this.pictureBox1.Size = new System.Drawing.Size(93, 93);
            this.pictureBox1.TabIndex = 0;
            this.pictureBox1.TabStop = false;
            // 
            // SplashScreen
            // 
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(14)))), ((int)(((byte)(17)))), ((int)(((byte)(27)))));
            this.ClientSize = new System.Drawing.Size(420, 140);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.pictureBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.MaximumSize = new System.Drawing.Size(420, 140);
            this.MinimumSize = new System.Drawing.Size(420, 140);
            this.Name = "SplashScreen";
            this.Padding = new System.Windows.Forms.Padding(20);
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

    }
}