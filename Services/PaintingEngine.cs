using RasterEditor.WinFormsDemo.Operations;

namespace RasterEditor.WinFormsDemo.Services;

internal sealed class PaintingEngine
{
    private PointF _lastSmoothedPoint;
    private bool _hasSmoothedPoint;

    public void BeginStroke(PointF startPoint)
    {
        _lastSmoothedPoint = startPoint;
        _hasSmoothedPoint = true;
    }

    public void EndStroke() => _hasSmoothedPoint = false;

    /// <summary>
    /// Exponential smoothing toward the raw pointer. <paramref name="stabilization"/> is 0..1
    /// (0 = follow cursor immediately, 1 = maximum lag / smooth line).
    /// </summary>
    public PointF GetSmoothedPoint(PointF rawPoint, float stabilization)
    {
        if (!_hasSmoothedPoint)
        {
            BeginStroke(rawPoint);
            return rawPoint;
        }

        float s = Math.Clamp(stabilization, 0f, 1f);
        // Linear map: s=0 → follow cursor fully (t=1), s=1 → strong lag (t≈minT).
        // Previously used s*s which made almost all effect only near 100%.
        const float minT = 0.028f;
        float t = float.Lerp(1f, minT, s);

        _lastSmoothedPoint = new PointF(
            _lastSmoothedPoint.X + (rawPoint.X - _lastSmoothedPoint.X) * t,
            _lastSmoothedPoint.Y + (rawPoint.Y - _lastSmoothedPoint.Y) * t);
        return _lastSmoothedPoint;
    }

    public static void DrawStrokeSegment(Bitmap target, PointF from, PointF to, Color color, int brushSize, bool eraseToTransparency)
    {
        using var g = Graphics.FromImage(target);
        g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        if (eraseToTransparency)
            g.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
        using var pen = new Pen(color, brushSize)
        {
            StartCap = System.Drawing.Drawing2D.LineCap.Round,
            EndCap = System.Drawing.Drawing2D.LineCap.Round,
            LineJoin = System.Drawing.Drawing2D.LineJoin.Round
        };
        g.DrawLine(pen, from, to);
    }

    public static void FloodFill(Bitmap bitmap, Point start, Color replacementColor)
    {
        if ((uint)start.X >= (uint)bitmap.Width || (uint)start.Y >= (uint)bitmap.Height)
            return;

        using var access = new BitmapAccess(bitmap, System.Drawing.Imaging.ImageLockMode.ReadWrite);
        uint target = access.GetPixelArgb(start.X, start.Y);
        uint replacement = (uint)replacementColor.ToArgb();
        if (target == replacement) return;

        var visited = new bool[access.Width * access.Height];
        var queue = new Queue<Point>();
        queue.Enqueue(start);

        while (queue.Count > 0)
        {
            var p = queue.Dequeue();
            if ((uint)p.X >= (uint)access.Width || (uint)p.Y >= (uint)access.Height)
                continue;

            int idx = p.Y * access.Width + p.X;
            if (visited[idx]) continue;
            visited[idx] = true;

            if (access.GetPixelArgb(p.X, p.Y) != target) continue;
            access.SetPixelArgb(p.X, p.Y, replacement);

            queue.Enqueue(new Point(p.X + 1, p.Y));
            queue.Enqueue(new Point(p.X - 1, p.Y));
            queue.Enqueue(new Point(p.X, p.Y + 1));
            queue.Enqueue(new Point(p.X, p.Y - 1));
        }
    }
}
