using System.Drawing.Imaging;

namespace Task2;

public sealed class GrayscaleResult : IDisposable
{
    public Bitmap PalNtsc { get; }
    public Bitmap Hdtv { get; }
    public Bitmap Difference { get; }
    public int[] PalNtscHistogram { get; }
    public int[] HdtvHistogram { get; }

    internal GrayscaleResult(Bitmap palNtsc, Bitmap hdtv, Bitmap difference,
        int[] palNtscHistogram, int[] hdtvHistogram)
    {
        PalNtsc = palNtsc;
        Hdtv = hdtv;
        Difference = difference;
        PalNtscHistogram = palNtscHistogram;
        HdtvHistogram = hdtvHistogram;
    }

    public void Dispose()
    {
        PalNtsc.Dispose();
        Hdtv.Dispose();
        Difference.Dispose();
    }
}

public sealed class ChannelResult : IDisposable
{
    public Bitmap Red { get; }
    public Bitmap Green { get; }
    public Bitmap Blue { get; }
    public int[] RedHistogram { get; }
    public int[] GreenHistogram { get; }
    public int[] BlueHistogram { get; }

    internal ChannelResult(Bitmap red, Bitmap green, Bitmap blue,
        int[] redHistogram, int[] greenHistogram, int[] blueHistogram)
    {
        Red = red;
        Green = green;
        Blue = blue;
        RedHistogram = redHistogram;
        GreenHistogram = greenHistogram;
        BlueHistogram = blueHistogram;
    }

    public void Dispose()
    {
        Red.Dispose();
        Green.Dispose();
        Blue.Dispose();
    }
}

public static class ImageProcessing
{
    // Both formulas from slide 44 act on 8-bit RGB channel values.
    // The resulting intensity is rounded to the nearest integer.
    public static byte PalNtscIntensity(byte r, byte g, byte b) =>
        ToByte(0.299 * r + 0.587 * g + 0.114 * b);

    public static byte HdtvIntensity(byte r, byte g, byte b) =>
        ToByte(0.2126 * r + 0.7152 * g + 0.0722 * b);

    private static byte ToByte(double value) =>
        (byte)Math.Clamp((int)Math.Round(value, MidpointRounding.AwayFromZero), 0, 255);

    public static GrayscaleResult CreateGrayscaleResult(Bitmap source)
    {
        ArgumentNullException.ThrowIfNull(source);

        using Bitmap input = FlattenOnWhite(source);
        Bitmap palNtsc = new(input.Width, input.Height, PixelFormat.Format32bppArgb);
        Bitmap hdtv = new(input.Width, input.Height, PixelFormat.Format32bppArgb);
        Bitmap difference = new(input.Width, input.Height, PixelFormat.Format32bppArgb);
        int[] palHistogram = new int[256];
        int[] hdtvHistogram = new int[256];
        Rectangle area = new(0, 0, input.Width, input.Height);
        BitmapData? inputData = null;
        BitmapData? palData = null;
        BitmapData? hdtvData = null;
        BitmapData? diffData = null;
        bool completed = false;

        try
        {
            inputData = input.LockBits(area, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            palData = palNtsc.LockBits(area, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            hdtvData = hdtv.LockBits(area, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            diffData = difference.LockBits(area, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            unsafe
            {
                for (int y = 0; y < input.Height; y++)
                {
                    byte* inputRow = (byte*)inputData.Scan0 + y * inputData.Stride;
                    byte* palRow = (byte*)palData.Scan0 + y * palData.Stride;
                    byte* hdtvRow = (byte*)hdtvData.Scan0 + y * hdtvData.Stride;
                    byte* diffRow = (byte*)diffData.Scan0 + y * diffData.Stride;

                    for (int x = 0; x < input.Width; x++)
                    {
                        int offset = x * 4; // Format32bppArgb stores B, G, R, A in memory.
                        byte b = inputRow[offset];
                        byte g = inputRow[offset + 1];
                        byte r = inputRow[offset + 2];
                        byte pal = PalNtscIntensity(r, g, b);
                        byte hd = HdtvIntensity(r, g, b);
                        byte delta = (byte)Math.Abs(pal - hd);

                        WriteGray(palRow + offset, pal);
                        WriteGray(hdtvRow + offset, hd);
                        WriteGray(diffRow + offset, delta);
                        palHistogram[pal]++;
                        hdtvHistogram[hd]++;
                    }
                }
            }

            GrayscaleResult result = new(palNtsc, hdtv, difference, palHistogram, hdtvHistogram);
            completed = true;
            return result;
        }
        finally
        {
            if (inputData is not null) input.UnlockBits(inputData);
            if (palData is not null) palNtsc.UnlockBits(palData);
            if (hdtvData is not null) hdtv.UnlockBits(hdtvData);
            if (diffData is not null) difference.UnlockBits(diffData);
            if (!completed)
            {
                palNtsc.Dispose();
                hdtv.Dispose();
                difference.Dispose();
            }
        }
    }

    // Each channel of the source is shown as a grayscale image: the kept channel
    // is copied into R, G and B, so only that channel survives in the result.
    // Histograms count the original 0-255 values of the corresponding channel.
    public static ChannelResult CreateChannelResult(Bitmap source)
    {
        ArgumentNullException.ThrowIfNull(source);

        using Bitmap input = FlattenOnWhite(source);
        Bitmap red = new(input.Width, input.Height, PixelFormat.Format32bppArgb);
        Bitmap green = new(input.Width, input.Height, PixelFormat.Format32bppArgb);
        Bitmap blue = new(input.Width, input.Height, PixelFormat.Format32bppArgb);
        int[] redHistogram = new int[256];
        int[] greenHistogram = new int[256];
        int[] blueHistogram = new int[256];
        Rectangle area = new(0, 0, input.Width, input.Height);
        BitmapData? inputData = null;
        BitmapData? redData = null;
        BitmapData? greenData = null;
        BitmapData? blueData = null;
        bool completed = false;

        try
        {
            inputData = input.LockBits(area, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            redData = red.LockBits(area, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            greenData = green.LockBits(area, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
            blueData = blue.LockBits(area, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            unsafe
            {
                for (int y = 0; y < input.Height; y++)
                {
                    byte* inputRow = (byte*)inputData.Scan0 + y * inputData.Stride;
                    byte* redRow = (byte*)redData.Scan0 + y * redData.Stride;
                    byte* greenRow = (byte*)greenData.Scan0 + y * greenData.Stride;
                    byte* blueRow = (byte*)blueData.Scan0 + y * blueData.Stride;

                    for (int x = 0; x < input.Width; x++)
                    {
                        int offset = x * 4; // Format32bppArgb stores B, G, R, A in memory.
                        byte b = inputRow[offset];
                        byte g = inputRow[offset + 1];
                        byte r = inputRow[offset + 2];

                        WriteGray(redRow + offset, r);
                        WriteGray(greenRow + offset, g);
                        WriteGray(blueRow + offset, b);
                        redHistogram[r]++;
                        greenHistogram[g]++;
                        blueHistogram[b]++;
                    }
                }
            }

            ChannelResult result = new(red, green, blue, redHistogram, greenHistogram, blueHistogram);
            completed = true;
            return result;
        }
        finally
        {
            if (inputData is not null) input.UnlockBits(inputData);
            if (redData is not null) red.UnlockBits(redData);
            if (greenData is not null) green.UnlockBits(greenData);
            if (blueData is not null) blue.UnlockBits(blueData);
            if (!completed)
            {
                red.Dispose();
                green.Dispose();
                blue.Dispose();
            }
        }
    }

    public static Bitmap FlattenOnWhite(Image source)
    {
        ArgumentNullException.ThrowIfNull(source);
        Bitmap copy = new(source.Width, source.Height, PixelFormat.Format32bppArgb);
        using Graphics graphics = Graphics.FromImage(copy);
        graphics.Clear(Color.White);
        graphics.DrawImage(source, 0, 0, source.Width, source.Height);
        return copy;
    }

    private static unsafe void WriteGray(byte* pixel, byte intensity)
    {
        pixel[0] = intensity;
        pixel[1] = intensity;
        pixel[2] = intensity;
        pixel[3] = 255;
    }
}
