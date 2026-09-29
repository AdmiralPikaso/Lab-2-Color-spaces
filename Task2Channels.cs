using System.Drawing.Imaging;

namespace Lab2;

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

public static partial class ImageProcessing
{
    
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
                        int offset = x * 4; 
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

}
