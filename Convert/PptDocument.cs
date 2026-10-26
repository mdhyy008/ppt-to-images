using System;
using System.Runtime.InteropServices;

namespace SlideShot
{
    internal sealed class PowerPointHost : IDisposable
    {
        private object app;

        public dynamic App
        {
            get
            {
                if (app == null)
                {
                    Type type = Type.GetTypeFromProgID("PowerPoint.Application");
                    if (type == null)
                        throw new InvalidOperationException("本机未安装 PowerPoint，无法打开 PPT 文件。");
                    app = Activator.CreateInstance(type);
                    try { ((dynamic)app).DisplayAlerts = 1; }
                    catch { }
                }
                return app;
            }
        }

        public void Shutdown()
        {
            if (app == null) return;
            try { ((dynamic)app).Quit(); }
            catch { }
            try { Marshal.ReleaseComObject(app); }
            catch { }
            app = null;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public void Dispose()
        {
            Shutdown();
        }
    }

    internal sealed class PptDocument : IPageRenderer
    {
        private object presentation;
        private readonly double slideWidth;
        private readonly double slideHeight;
        private readonly int pageCount;

        public PptDocument(PowerPointHost host, string path, string cacheDir)
        {
            try
            {
                string copy = System.IO.Path.Combine(cacheDir,
                    "source" + System.IO.Path.GetExtension(path).ToLowerInvariant());
                System.IO.File.Copy(path, copy, true);
                Native.RemoveZoneIdentifier(copy);

                dynamic app = host.App;
                presentation = app.Presentations.Open(copy, -1, 0, 0);
                dynamic pres = presentation;
                slideWidth = Convert.ToDouble(pres.PageSetup.SlideWidth);
                slideHeight = Convert.ToDouble(pres.PageSetup.SlideHeight);
                pageCount = Convert.ToInt32(pres.Slides.Count);
            }
            catch (Exception ex)
            {
                CloseQuietly();
                throw new InvalidOperationException("PowerPoint 无法打开该文件（可能已加密、损坏或受保护）。", ex);
            }
        }

        public int PageCount
        {
            get { return pageCount; }
        }

        public double Aspect
        {
            get { return slideHeight <= 0 ? 16.0 / 9.0 : slideWidth / slideHeight; }
        }

        public void RenderPage(int pageNumber, string fullPath, string thumbPath)
        {
            dynamic pres = presentation;
            object slide = pres.Slides.Item(pageNumber);
            try
            {
                dynamic s = slide;
                int width = (int)Math.Round(slideWidth * 2);
                int height = (int)Math.Round(slideHeight * 2);
                if (width < 2) width = 2;
                if (height < 2) height = 2;
                s.Export(fullPath, "PNG", width, height);
            }
            finally
            {
                try { Marshal.ReleaseComObject(slide); }
                catch { }
            }
            ImageTools.SaveThumbnail(fullPath, thumbPath);
        }

        private void CloseQuietly()
        {
            if (presentation == null) return;
            try { ((dynamic)presentation).Close(); }
            catch { }
            try { Marshal.ReleaseComObject(presentation); }
            catch { }
            presentation = null;
        }

        public void Dispose()
        {
            CloseQuietly();
        }
    }
}
