using System.Drawing.Imaging;

namespace Task2;

public sealed class MainForm : Form
{
    private readonly PictureBox _original = CreatePictureBox();
    private readonly PictureBox _pal = CreatePictureBox();
    private readonly PictureBox _hdtv = CreatePictureBox();
    private readonly PictureBox _difference = CreatePictureBox();
    private readonly HistogramControl _palHistogram = new();
    private readonly HistogramControl _hdtvHistogram = new();
    private readonly Label _status = new() { AutoSize = true, Text = "Откройте изображение для обработки." };
    private readonly ComboBox _saveChoice = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 190 };
    private Bitmap? _source;
    private GrayscaleResult? _result;

    public MainForm()
    {
        Text = "Цветовые пространства — RGB в оттенки серого";
        MinimumSize = new Size(1020, 650);
        Size = new Size(1280, 800);
        StartPosition = FormStartPosition.CenterScreen;

        TableLayoutPanel root = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 3,
            Padding = new Padding(10)
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        Controls.Add(root);

        FlowLayoutPanel toolbar = new()
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true,
            Padding = new Padding(0, 0, 0, 8)
        };
        Button open = new() { Text = "Открыть изображение", AutoSize = true };
        open.Click += (_, _) => OpenImage();
        Button save = new() { Text = "Сохранить результат", AutoSize = true };
        save.Click += (_, _) => SaveSelectedImage();
        _saveChoice.Items.AddRange(new object[] { "PAL/NTSC", "HDTV", "Разность" });
        _saveChoice.SelectedIndex = 0;
        toolbar.Controls.Add(open);
        toolbar.Controls.Add(new Label { Text = "Сохранить:", AutoSize = true, Padding = new Padding(14, 7, 0, 0) });
        toolbar.Controls.Add(_saveChoice);
        toolbar.Controls.Add(save);
        root.Controls.Add(toolbar, 0, 0);

        TabControl tabs = new() { Dock = DockStyle.Fill };
        tabs.TabPages.Add(CreateImagesTab());
        tabs.TabPages.Add(CreateHistogramsTab());
        root.Controls.Add(tabs, 0, 1);

        _status.Padding = new Padding(0, 7, 0, 0);
        root.Controls.Add(_status, 0, 2);
    }

    private TabPage CreateImagesTab()
    {
        TabPage page = new("Изображения");
        TableLayoutPanel grid = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 2,
            Padding = new Padding(8)
        };
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        grid.Controls.Add(CreateImagePanel("Оригинал", _original), 0, 0);
        grid.Controls.Add(CreateImagePanel("PAL/NTSC: 0,299R + 0,587G + 0,114B", _pal), 1, 0);
        grid.Controls.Add(CreateImagePanel("HDTV: 0,2126R + 0,7152G + 0,0722B", _hdtv), 0, 1);
        grid.Controls.Add(CreateImagePanel("Разность: |PAL/NTSC − HDTV|", _difference), 1, 1);
        page.Controls.Add(grid);
        return page;
    }

    private TabPage CreateHistogramsTab()
    {
        TabPage page = new("Гистограммы");
        TableLayoutPanel grid = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Padding = new Padding(8)
        };
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        grid.RowStyles.Add(new RowStyle(SizeType.Percent, 50));
        grid.Controls.Add(CreateHistogramPanel("PAL/NTSC — число пикселей каждой интенсивности", _palHistogram), 0, 0);
        grid.Controls.Add(CreateHistogramPanel("HDTV — число пикселей каждой интенсивности", _hdtvHistogram), 0, 1);
        page.Controls.Add(grid);
        return page;
    }

    private static Control CreateImagePanel(string title, PictureBox picture)
    {
        GroupBox box = new() { Text = title, Dock = DockStyle.Fill, Padding = new Padding(9) };
        box.Controls.Add(picture);
        return box;
    }

    private static Control CreateHistogramPanel(string title, HistogramControl histogram)
    {
        GroupBox box = new() { Text = title, Dock = DockStyle.Fill, Padding = new Padding(9) };
        histogram.Dock = DockStyle.Fill;
        box.Controls.Add(histogram);
        return box;
    }

    private static PictureBox CreatePictureBox() => new()
    {
        Dock = DockStyle.Fill,
        SizeMode = PictureBoxSizeMode.Zoom,
        BackColor = Color.WhiteSmoke,
        BorderStyle = BorderStyle.FixedSingle
    };

    private void OpenImage()
    {
        using OpenFileDialog dialog = new()
        {
            Title = "Выберите изображение",
            Filter = "Изображения|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff|Все файлы|*.*"
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        Bitmap? nextSource = null;
        GrayscaleResult? nextResult = null;
        try
        {
            // The file is closed immediately after loading, so it can be replaced or deleted.
            using Image loaded = Image.FromFile(dialog.FileName);
            nextSource = ImageProcessing.FlattenOnWhite(loaded);
            nextResult = ImageProcessing.CreateGrayscaleResult(nextSource);

            ClearImages();
            _source = nextSource;
            _result = nextResult;
            nextSource = null;
            nextResult = null;
            _original.Image = _source;
            _pal.Image = _result.PalNtsc;
            _hdtv.Image = _result.Hdtv;
            _difference.Image = _result.Difference;
            _palHistogram.SetValues(_result.PalNtscHistogram);
            _hdtvHistogram.SetValues(_result.HdtvHistogram);
            _status.Text = $"{Path.GetFileName(dialog.FileName)} — {_source.Width} × {_source.Height} пикселей";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Не удалось обработать изображение: {ex.Message}",
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            nextResult?.Dispose();
            nextSource?.Dispose();
        }
    }

    private void SaveSelectedImage()
    {
        if (_result is null)
        {
            MessageBox.Show(this, "Сначала откройте изображение.", "Нет результата",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        Bitmap image = _saveChoice.SelectedIndex switch
        {
            1 => _result.Hdtv,
            2 => _result.Difference,
            _ => _result.PalNtsc
        };
        using SaveFileDialog dialog = new()
        {
            Title = "Сохранить изображение",
            Filter = "PNG (*.png)|*.png",
            DefaultExt = "png",
            AddExtension = true,
            FileName = _saveChoice.SelectedIndex switch
            {
                1 => "grayscale-hdtv.png",
                2 => "difference.png",
                _ => "grayscale-pal-ntsc.png"
            }
        };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            image.Save(dialog.FileName, ImageFormat.Png);
            _status.Text = $"Сохранено: {dialog.FileName}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Не удалось сохранить изображение: {ex.Message}",
                "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ClearImages()
    {
        _original.Image = null;
        _pal.Image = null;
        _hdtv.Image = null;
        _difference.Image = null;
        _palHistogram.SetValues(null);
        _hdtvHistogram.SetValues(null);
        _result?.Dispose();
        _result = null;
        _source?.Dispose();
        _source = null;
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) ClearImages();
        base.Dispose(disposing);
    }
}
