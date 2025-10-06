using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Runtime.InteropServices;

public class DarkComboBox : ComboBox
{
    public Color FillColor { get; set; } = Color.FromArgb(20, 22, 30);
    public Color BorderColor { get; set; } = Color.FromArgb(60, 65, 75);
    public Color HoverColor { get; set; } = Color.FromArgb(35, 38, 48);
    public Color TextColor { get; set; } = Color.White;
    public int Radius { get; set; } = 6;
    public int ButtonWidth { get; set; } = 24;

    private bool _hover;

    [DllImport("user32.dll")]
    private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

    private const int CB_SETITEMHEIGHT = 0x0153;

    public int DesiredHeight { get; set; } = 28; // ← hauteur totale souhaitée

    private void ApplyHeights()
    {
        if (!IsHandleCreated) return;

        // Hauteur interne (edit field). Garde une petite marge.
        int editHeight = Math.Max(16, DesiredHeight - 6);

        // 1) Fixer la hauteur de la zone sélectionnée (index = -1)
        SendMessage(Handle, CB_SETITEMHEIGHT, (IntPtr)(-1), (IntPtr)editHeight);

        // 2) Hauteur des items du dropdown
        ItemHeight = editHeight;

        // 3) Hauteur du contrôle lui-même
        Height = DesiredHeight;

        // 4) Optionnel: contrôler la hauteur du menu déroulant
        IntegralHeight = false;
        DropDownHeight = ItemHeight * 8;

        Invalidate();
    }

    protected override void OnHandleCreated(EventArgs e)
    {
        base.OnHandleCreated(e);
        ApplyHeights();
    }

    protected override void OnFontChanged(EventArgs e)
    {
        base.OnFontChanged(e);
        ApplyHeights(); // garder la hauteur même si la police change
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        // si quelqu’un change la hauteur manuellement, on la respecte
        DesiredHeight = Height;
        ApplyHeights();
    }

    public DarkComboBox()
    {
        DrawMode = DrawMode.OwnerDrawFixed;         // on dessine les items
        DropDownStyle = ComboBoxStyle.DropDownList; // pas d’édition libre
        FlatStyle = FlatStyle.Flat;

        // pour que OnPaint/WM_PAINT soit fluide
        SetStyle(ControlStyles.AllPaintingInWmPaint |
                 ControlStyles.OptimizedDoubleBuffer |
                 ControlStyles.UserPaint, true);

        BackColor = FillColor;
        ForeColor = TextColor;
        //ItemHeight = 22;

        MouseEnter += (_, __) => { _hover = true; Invalidate(); };
        MouseLeave += (_, __) => { _hover = false; Invalidate(); };
        DropDownClosed += (_, __) => Invalidate();
        Resize += (_, __) => { DropDownWidth = Width; Invalidate(); };
    }

    protected override void OnDrawItem(DrawItemEventArgs e)
    {
        e.DrawBackground();
        if (e.Index < 0) return;

        bool selected = (e.State & DrawItemState.Selected) == DrawItemState.Selected;
        var bg = selected ? HoverColor : FillColor;

        using (var b = new SolidBrush(bg))
            e.Graphics.FillRectangle(b, e.Bounds);

        var text = GetItemText(Items[e.Index]);
        var textRect = new Rectangle(e.Bounds.X + 10, e.Bounds.Y, e.Bounds.Width - 20, e.Bounds.Height);

        TextRenderer.DrawText(
            e.Graphics,
            text,
            Font,
            textRect,
            TextColor,
            TextFormatFlags.Left | TextFormatFlags.VerticalCenter
        );
    }

    protected override void WndProc(ref Message m)
    {
        base.WndProc(ref m);

        // WM_PAINT = 0x000F (peindre le contrôle fermé)
        if (m.Msg == 0x000F)
        {
            using (var g = Graphics.FromHwnd(Handle))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                var rect = ClientRectangle;

                // fond + bord arrondi
                using (var path = Rounded(rect, Radius))
                using (var fill = new SolidBrush(FillColor))
                using (var pen = new Pen(BorderColor, 1))
                {
                    g.FillPath(fill, path);
                    g.DrawPath(pen, path);
                }

                // zone bouton à droite
                var btn = new Rectangle(Width - ButtonWidth - 1, 1, ButtonWidth, Height - 2);
                using (var pathBtn = Rounded(btn, Radius, topRight: true, bottomRight: true))
                using (var fillBtn = new SolidBrush(_hover ? HoverColor : FillColor))
                using (var penBtn = new Pen(BorderColor))
                {
                    g.FillPath(fillBtn, pathBtn);
                    g.DrawPath(penBtn, pathBtn);
                }

                // chevron ▼
                using (var pen = new Pen(Color.FromArgb(140, 145, 155), 2))
                {
                    int cx = btn.Left + btn.Width / 2;
                    int cy = btn.Top + btn.Height / 2;
                    g.DrawLines(pen, new[]
                    {
                        new Point(cx - 4, cy - 2),
                        new Point(cx,     cy + 3),
                        new Point(cx + 4, cy - 2)
                    });
                }

                // texte sélectionné
                var textRect = new Rectangle(8, 0, Width - ButtonWidth - 16, Height);
                var txt = Text; // SelectedItem -> Text en DropDownList
                TextRenderer.DrawText(
                    g, txt, Font, textRect,
                    Enabled ? TextColor : Color.FromArgb(160, 160, 160),
                    TextFormatFlags.Left | TextFormatFlags.VerticalCenter
                );
            }
        }
    }

    private static GraphicsPath Rounded(Rectangle r, int radius, bool topRight = true, bool bottomRight = true)
    {
        // rayon minimum
        int d = Math.Max(1, radius * 2);
        var path = new GraphicsPath();

        // coins : TL, TR, BR, BL
        path.StartFigure();
        path.AddArc(new Rectangle(r.X, r.Y, d, d), 180, 90); // TL
        if (topRight) path.AddArc(new Rectangle(r.Right - d - 1, r.Y, d, d), 270, 90); // TR
        else path.AddLine(r.Right - 1, r.Y, r.Right - 1, r.Y);

        if (bottomRight) path.AddArc(new Rectangle(r.Right - d - 1, r.Bottom - d - 1, d, d), 0, 90); // BR
        else path.AddLine(r.Right - 1, r.Bottom - 1, r.Right - 1, r.Bottom - 1);

        path.AddArc(new Rectangle(r.X, r.Bottom - d - 1, d, d), 90, 90); // BL
        path.CloseFigure();
        return path;
    }
}