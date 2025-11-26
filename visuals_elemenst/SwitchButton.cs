using System.ComponentModel;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

public class SwitchButton : CheckBox
{
    private Color onBackColor = Color.DarkViolet;

    private Color onToggleColor = Color.WhiteSmoke;

    private Color offBackColor = Color.Gray;

    private Color offToggleColor = Color.Gainsboro;

    private bool solidStyle = true;

    [Category("Code Advance")]
    public Color OnBackColor
    {
        get
        {
            return onBackColor;
        }
        set
        {
            onBackColor = value;
            Invalidate();
        }
    }

    [Category("Code Advance")]
    public Color OnToggleColor
    {
        get
        {
            return onToggleColor;
        }
        set
        {
            onToggleColor = value;
            Invalidate();
        }
    }

    [Category("Code Advance")]
    public Color OffBackColor
    {
        get
        {
            return offBackColor;
        }
        set
        {
            offBackColor = value;
            Invalidate();
        }
    }

    [Category("Code Advance")]
    public Color OffToggleColor
    {
        get
        {
            return offToggleColor;
        }
        set
        {
            offToggleColor = value;
            Invalidate();
        }
    }

    [Browsable(false)]
    public override string Text
    {
        get
        {
            return base.Text;
        }
        set
        {
        }
    }

    [Category("Code Advance")]
    [DefaultValue(true)]
    public bool SolidStyle
    {
        get
        {
            return solidStyle;
        }
        set
        {
            solidStyle = value;
            Invalidate();
        }
    }

    public SwitchButton()
    {
        MinimumSize = new Size(45, 45);
    }

    private GraphicsPath GetFigurePath()
    {
        int num = Height - 1;
        Rectangle rect = new(0, 0, num, num);
        Rectangle rect2 = new(Width - num - 2, 0, num, num);
        GraphicsPath graphicsPath = new();
        graphicsPath.StartFigure();
        graphicsPath.AddArc(rect, 90f, 180f);
        graphicsPath.AddArc(rect2, 270f, 180f);
        graphicsPath.CloseFigure();
        return graphicsPath;
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        pevent.Graphics.Clear(Parent.BackColor);

        int toggleSize = Height - 5;
        bool isChecked = Checked;

        Color backColor = isChecked ? onBackColor : offBackColor;
        Color toggleColor = isChecked ? onToggleColor : offToggleColor;

        int toggleX = isChecked ? Width - Height + 1 : 2;
        Rectangle toggleRect = new(toggleX, 2, toggleSize, toggleSize);

        using GraphicsPath path = GetFigurePath();
        if (solidStyle)
        {
            using SolidBrush backBrush = new(backColor);
            pevent.Graphics.FillPath(backBrush, path);
        }
        else
        {
            using Pen borderPen = new(backColor, 2f);
            pevent.Graphics.DrawPath(borderPen, path);
        }

        using SolidBrush toggleBrush = new(toggleColor);
        pevent.Graphics.FillEllipse(toggleBrush, toggleRect);
    }
}

