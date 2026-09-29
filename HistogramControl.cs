namespace Task2;

public sealed class HistogramControl : Control
{
    private int[]? _values;

    public HistogramControl()
    {
        DoubleBuffered = true;
        BackColor = Color.White;
        MinimumSize = new Size(300, 180);
    }

    public void SetValues(int[]? values)
    {
        if (values is not null && values.Length != 256)
            throw new ArgumentException("Гистограмма должна содержать 256 значений.", nameof(values));

        _values = values;
        Invalidate();
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;
        g.Clear(BackColor);

        Rectangle plot = new(42, 18, Math.Max(1, Width - 64), Math.Max(1, Height - 55));
        using Pen axis = new(Color.DimGray);
        using Pen bars = new(Color.SteelBlue);
        using Font labelFont = new("Segoe UI", 9);
        using Brush labelBrush = new SolidBrush(Color.DimGray);

        g.DrawLine(axis, plot.Left, plot.Top, plot.Left, plot.Bottom);
        g.DrawLine(axis, plot.Left, plot.Bottom, plot.Right, plot.Bottom);
        g.DrawString("0", labelFont, labelBrush, plot.Left - 7, plot.Bottom + 4);
        g.DrawString("128", labelFont, labelBrush, plot.Left + plot.Width / 2 - 12, plot.Bottom + 4);
        g.DrawString("255", labelFont, labelBrush, plot.Right - 23, plot.Bottom + 4);
        g.DrawString("Интенсивность", labelFont, labelBrush, plot.Right - 100, plot.Bottom + 21);

        if (_values is null) return;
        int max = Math.Max(1, _values.Max());
        g.DrawString(max.ToString(), labelFont, labelBrush, 2, plot.Top - 7);

        // One bin per intensity. Several bins may share a screen column when resized.
        for (int x = 0; x < plot.Width; x++)
        {
            int from = x * 256 / plot.Width;
            int to = Math.Max(from + 1, (x + 1) * 256 / plot.Width);
            int count = 0;
            for (int bin = from; bin < to && bin < 256; bin++)
                count = Math.Max(count, _values[bin]);

            int barHeight = (int)Math.Round(count * (double)plot.Height / max);
            g.DrawLine(bars, plot.Left + x, plot.Bottom, plot.Left + x, plot.Bottom - barHeight);
        }
    }
}
