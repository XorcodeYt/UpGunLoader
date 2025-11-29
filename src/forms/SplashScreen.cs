using System;
using System.Windows.Forms;

public class SplashScreen : Form
{
    private PictureBox pictureBox1;
    private Label label1;
    private string _baseStatusText = "";

    public SplashScreen()
    {
        InitializeComponent();

        _baseStatusText = label1.Text;
    }

    public void SetStatus(string text)
    {
        if (InvokeRequired)
        {
            BeginInvoke(new Action<string>(SetStatus), text);
            return;
        }

        if (text.Length > 26)
        {
            string firstLine = text.Substring(0, 26);
            string secondLine = text.Substring(26);
            _baseStatusText = firstLine + Environment.NewLine + secondLine;
        }
        else
        {
            _baseStatusText = text;
        }

        label1.Text = _baseStatusText;
    }

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
        this.label1.Font = new System.Drawing.Font("Unispace", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, 0);
        this.label1.ForeColor = System.Drawing.Color.White;
        this.label1.Location = new System.Drawing.Point(123, 62);
        this.label1.Name = "label1";
        this.label1.Size = new System.Drawing.Size(189, 19);
        this.label1.TabIndex = 1;
        this.label1.Text = "J'adore le cocombre";
        // 
        // pictureBox1
        // 
        this.pictureBox1.Image = global::UpGunModLoader.Properties.Resources.logo93;
        this.pictureBox1.Location = new System.Drawing.Point(24, 24);
        this.pictureBox1.Name = "pictureBox1";
        this.pictureBox1.Size = new System.Drawing.Size(93, 93);
        this.pictureBox1.TabIndex = 0;
        this.pictureBox1.TabStop = false;
        // 
        // SplashScreen
        // 
        this.BackColor = System.Drawing.Color.FromArgb(((byte)(14)), ((byte)(17)), ((byte)(27)));
        this.ClientSize = new System.Drawing.Size(420, 140);
        this.Controls.Add(this.label1);
        this.Controls.Add(this.pictureBox1);
        this.Cursor = System.Windows.Forms.Cursors.No;
        this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
        this.MaximumSize = new System.Drawing.Size(420, 140);
        this.MinimumSize = new System.Drawing.Size(420, 140);
        this.Name = "SplashScreen";
        this.Padding = new System.Windows.Forms.Padding(20);
        this.ShowIcon = false;
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        ((System.ComponentModel.ISupportInitialize)(this.pictureBox1)).EndInit();
        this.ResumeLayout(false);
        this.PerformLayout();

    }
}