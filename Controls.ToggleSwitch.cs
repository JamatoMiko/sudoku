
using System.Drawing.Drawing2D;

namespace System.Windows.Forms;

public class ToggleSwitch : Panel
{
    private bool _isOn;
    private Color onColor;
    public Color OnColor
    {
        get => onColor;
        set
        {
            onColor = value;
            Invalidate();
        }
    }
    private Color offColor;
    public Color OffColor
    {
        get => offColor;
        set
        {
            offColor = value;
            Invalidate();
        }
    }
    public Color KnobColor {get; set;}

    Brush brush;
    public bool IsOn
    {
        get
        {
            return _isOn;
        }
        set
        {
            _isOn = value;
            this.Invalidate();
        }
    }
    public ToggleSwitch()
    {
        this.Size = new Size(100, 50);
        OnColor = Color.FromArgb(0x16, 0x6D, 0xCF);
        OffColor = Color.LightGray;
        KnobColor = Color.White;
        this.Cursor = Cursors.Hand;
        this.BackColor = Color.Transparent;
        this.DoubleBuffered = true;
    }
    protected override void OnMouseClick(MouseEventArgs e)
    {
        base.OnMouseClick(e);
        if (e.Button == MouseButtons.Left)
        {
            IsOn = !IsOn;
        }
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        var x = this.ClientRectangle.X;
        var y = this.ClientRectangle.Y;
        var width = this.ClientRectangle.Width;//横の幅
        var height = this.ClientRectangle.Height;//縦の高さ
        //背景
        brush = new SolidBrush(IsOn ? OnColor : OffColor);
        e.Graphics.FillEllipse(brush, x, y, width / 2, height - 1);
        e.Graphics.FillEllipse(brush, x + width / 2, y, width / 2 - 1, height - 1);
        e.Graphics.FillRectangle(brush, x + width / 4, y, width / 2, height - 1);
        //ノブ
        brush = new SolidBrush(KnobColor);
        if (IsOn)
        {
            e.Graphics.FillEllipse(brush, x + width / 2 + width / 25, y + width / 25, width / 2 - width / 25 * 2 - 1, height - width / 25 * 2 - 1);
        }
        else
        {
            e.Graphics.FillEllipse(brush, x + width / 25, y + width / 25, width / 2 - width / 25 * 2 - 1, height - width / 25 * 2 - 1);
        }
    }
}