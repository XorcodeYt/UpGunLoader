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
        MinimumSize = new Size(45, 22);
    }

    private GraphicsPath GetFigurePath()
    {
        int num = Height - 1;
        Rectangle rect = new Rectangle(0, 0, num, num);
        Rectangle rect2 = new Rectangle(Width - num - 2, 0, num, num);
        GraphicsPath graphicsPath = new GraphicsPath();
        graphicsPath.StartFigure();
        graphicsPath.AddArc(rect, 90f, 180f);
        graphicsPath.AddArc(rect2, 270f, 180f);
        graphicsPath.CloseFigure();
        return graphicsPath;
    }

    protected override void OnPaint(PaintEventArgs pevent)
    {
        int num = Height - 5;
        pevent.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        pevent.Graphics.Clear(Parent.BackColor);
        if (Checked)
        {
            if (solidStyle)
            {
                pevent.Graphics.FillPath(new SolidBrush(onBackColor), GetFigurePath());
            }
            else
            {
                pevent.Graphics.DrawPath(new Pen(onBackColor, 2f), GetFigurePath());
            }
            pevent.Graphics.FillEllipse(new SolidBrush(onToggleColor), new Rectangle(Width - Height + 1, 2, num, num));
        }
        else
        {
            if (solidStyle)
            {
                pevent.Graphics.FillPath(new SolidBrush(offBackColor), GetFigurePath());
            }
            else
            {
                pevent.Graphics.DrawPath(new Pen(offBackColor, 2f), GetFigurePath());
            }
            pevent.Graphics.FillEllipse(new SolidBrush(offToggleColor), new Rectangle(2, 2, num, num));
        }
    }
}
