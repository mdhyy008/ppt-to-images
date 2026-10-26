using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace SlideShot
{
    internal enum TabState
    {
        Queued,
        Opening,
        Converting,
        Ready,
        Cancelled,
        Failed
    }

    internal sealed class PageItem
    {
        public int Number;
        public string FullPath;
        public string ThumbPath;
    }

    internal sealed class DocumentTab
    {
        public string Id = Guid.NewGuid().ToString("N");
        public string FilePath;
        public string FileName;
        public string BaseName;
        public TabState State = TabState.Queued;
        public int PageCount;
        public int Rendered;
        public CancellationTokenSource Cancel = new CancellationTokenSource();
        public List<PageItem> Pages = new List<PageItem>();
        public ThumbnailGrid Grid;

        public string ExportDir
        {
            get
            {
                string dir = Path.GetDirectoryName(FilePath);
                return Path.Combine(dir == null ? AppPaths.DataDir : dir, BaseName + "_图片");
            }
        }

        public bool IsBusy
        {
            get { return State == TabState.Queued || State == TabState.Opening || State == TabState.Converting; }
        }
    }
}
