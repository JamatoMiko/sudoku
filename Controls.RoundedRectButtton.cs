using System.Drawing.Drawing2D;

namespace System.Windows.Forms;

public class RoundedRectButton : Button
{
    public int Radius{get; set;}
    public int BorderSize {get; set;}
    private new Color BackColor {get; set;}
    private Color BorderColor {get; set;}
    private Color defaultBackColor;
    public new Color DefaultBackColor
    {
        get => defaultBackColor;
        set
        {
            defaultBackColor = value;
            BackColor = DefaultBackColor;
            Invalidate();
        }
    }
    private Color hoverBackColor;
    public Color HoverBackColor
    {
        get => hoverBackColor;
        set
        {
            hoverBackColor = value;
            Invalidate();
        }
    }
    private Color defaultBorderColor;
    public Color DefaultBorderColor
    {
        get => defaultBorderColor;
        set{
            defaultBorderColor = value;
            BorderColor = DefaultBorderColor;
            Invalidate();
        }
    }
    private Color hoverBorderColor;
    public Color HoverBorderColor
    {
        get => hoverBorderColor;
        set
        {
            hoverBorderColor = value;
            Invalidate();
        }
    }
    public RoundedRectButton()
    {
        Cursor = Cursors.Hand;
        Size = new Size(75 * 2, 23 * 2);
        Radius = 23;
        BorderSize = 1;
        DefaultBackColor = Color.FromArgb(0xE1, 0xE1, 0xE1);
        HoverBackColor = Color.FromArgb(0xE5, 0xF1, 0xFB);
        DefaultBorderColor =  Color.FromArgb(0xB2, 0xB2, 0xB2);
        HoverBorderColor  = Color.FromArgb(0x00, 0x78, 0xD7);

        //元々のボタンの描画を消す
        FlatStyle = FlatStyle.Flat;
        FlatAppearance.BorderSize = 0;
        FlatAppearance.MouseOverBackColor = Color.Transparent;
        FlatAppearance.MouseDownBackColor = Color.Transparent;

        //ボタンがフォーカスを取得しないように設定
        SetStyle(ControlStyles.Selectable, false);
    }
    protected override void OnMouseEnter(EventArgs e)
    {
        base.OnMouseEnter(e);
        BackColor = HoverBackColor;
        BorderColor = HoverBorderColor;
    }
    protected override void OnMouseLeave(EventArgs e)
    {
        base.OnMouseLeave(e);
        BackColor = DefaultBackColor;
        BorderColor = DefaultBorderColor;
    }
    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        var g = e.Graphics;
        g.SmoothingMode = SmoothingMode.AntiAlias;
        var rect = ClientRectangle;
        var path = new GraphicsPath();
        path.StartFigure();
        //GraphicPathはAddArcで追加した円弧のそれぞれの端点を自動で繋いでくれるので丸角を追加するだけで矩形になる
        path.AddArc(rect.X, rect.Y, Radius, Radius, 180, 90);
        path.AddArc(rect.Right - Radius, rect.Y, Radius, Radius, 270, 90);
        path.AddArc(rect.Right - Radius, rect.Bottom - Radius, Radius, Radius, 0, 90);
        path.AddArc(rect.X, rect.Bottom - Radius, Radius, Radius, 90, 90);
        path.CloseFigure();
        Region = new Region(path);
        g.FillPath(new SolidBrush(BackColor), path);
        if (BorderSize > 0)
        {
            g.DrawPath(new Pen(BorderColor, BorderSize), path);
        }
        TextRenderer.DrawText(g, Text, Font, ClientRectangle, ForeColor, TextFormatFlags.HorizontalCenter | TextFormatFlags.VerticalCenter);
    }
}