using System.Drawing.Imaging;

namespace Lab2;

public sealed class HsvResult : IDisposable
{
    public Bitmap Adjusted { get; }

    internal HsvResult(Bitmap adjusted)
    {
        Adjusted = adjusted;
    }

    public void Dispose() => Adjusted.Dispose();
}

public static partial class ImageProcessing
{
    public static HsvResult CreateHsvResult(Bitmap source,
                                            float hueShift,
                                            float saturationShift,
                                            float valueShift)
    {
        ArgumentNullException.ThrowIfNull(source);

        using Bitmap input = FlattenOnWhite(source);
        Bitmap adjusted = new(input.Width, input.Height, PixelFormat.Format32bppArgb);
        Rectangle area = new(0, 0, input.Width, input.Height);
        BitmapData? inputData = null;
        BitmapData? outputData = null;
        bool completed = false;

        try
        {
            inputData = input.LockBits(area, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
            outputData = adjusted.LockBits(area, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            unsafe
            {
                for (int y = 0; y < input.Height; y++)
                {
                    byte* inputRow = (byte*)inputData.Scan0 + y * inputData.Stride;
                    byte* outputRow = (byte*)outputData.Scan0 + y * outputData.Stride;

                    for (int x = 0; x < input.Width; x++)
                    {
                        int offset = x * 4;
                        byte b = inputRow[offset];
                        byte g = inputRow[offset + 1];
                        byte r = inputRow[offset + 2];

                        RgbToHsv(r, g, b, out float h, out float s, out float v);

                        h += hueShift;
                        while (h < 0f) h += 360f;
                        while (h >= 360f) h -= 360f;

                        s = saturationShift >= 0f
                            ? s + (1f - s) * saturationShift
                            : s * (1f + saturationShift);
                        s = Math.Clamp(s, 0f, 1f);

                        v = valueShift >= 0f
                            ? v + (1f - v) * valueShift
                            : v * (1f + valueShift);
                        v = Math.Clamp(v, 0f, 1f);

                        HsvToRgb(h, s, v, out byte rOut, out byte gOut, out byte bOut);

                        outputRow[offset] = bOut;
                        outputRow[offset + 1] = gOut;
                        outputRow[offset + 2] = rOut;
                        outputRow[offset + 3] = 255;
                    }
                }
            }

            HsvResult result = new(adjusted);
            completed = true;
            return result;
        }
        finally
        {
            if (inputData is not null) input.UnlockBits(inputData);
            if (outputData is not null) adjusted.UnlockBits(outputData);
            if (!completed) adjusted.Dispose();
        }
    }

    private static void RgbToHsv(byte r, byte g, byte b,
                                 out float h, out float s, out float v)
    {
        float rf = r / 255f;
        float gf = g / 255f;
        float bf = b / 255f;

        float max = Math.Max(rf, Math.Max(gf, bf));
        float min = Math.Min(rf, Math.Min(gf, bf));
        float delta = max - min;

        v = max;

        if (max == 0f)
        {
            s = 0f;
            h = 0f;
            return;
        }

        s = delta / max;

        if (delta == 0f)
        {
            h = 0f;
            return;
        }

        if (max == rf)
            h = 60f * (((gf - bf) / delta) % 6f);
        else if (max == gf)
            h = 60f * (((bf - rf) / delta) + 2f);
        else
            h = 60f * (((rf - gf) / delta) + 4f);

        if (h < 0f) h += 360f;
    }

    private static void HsvToRgb(float h, float s, float v,
                                 out byte r, out byte g, out byte b)
    {
        if (s <= 0f)
        {
            byte gray = ToByte(v * 255f);
            r = g = b = gray;
            return;
        }

        h %= 360f;
        if (h < 0f) h += 360f;

        float c = v * s;
        float hh = h / 60f;
        float x = c * (1f - Math.Abs(hh % 2f - 1f));
        float m = v - c;

        float rf, gf, bf;
        if (hh < 1f) { rf = c; gf = x; bf = 0f; }
        else if (hh < 2f) { rf = x; gf = c; bf = 0f; }
        else if (hh < 3f) { rf = 0f; gf = c; bf = x; }
        else if (hh < 4f) { rf = 0f; gf = x; bf = c; }
        else if (hh < 5f) { rf = x; gf = 0f; bf = c; }
        else { rf = c; gf = 0f; bf = x; }

        r = ToByte((rf + m) * 255f);
        g = ToByte((gf + m) * 255f);
        b = ToByte((bf + m) * 255f);
    }
}
