using System;

namespace SlideShot
{
    internal interface IPageRenderer : IDisposable
    {
        int PageCount { get; }
        double Aspect { get; }
        void RenderPage(int pageNumber, string fullPath, string thumbPath);
    }
}
