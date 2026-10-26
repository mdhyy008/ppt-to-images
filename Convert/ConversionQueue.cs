using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;

namespace SlideShot
{
    internal sealed class ConversionQueue : IDisposable
    {
        private readonly BlockingCollection<DocumentTab> queue = new BlockingCollection<DocumentTab>();
        private readonly SynchronizationContext ui;
        private readonly Thread worker;
        private readonly PowerPointHost powerPoint = new PowerPointHost();
        private volatile bool disposed;

        public event Action<DocumentTab, int, double> Opened;
        public event Action<DocumentTab, int, int> Progressed;
        public event Action<DocumentTab, int, string, string> PageReady;
        public event Action<DocumentTab, string> Failed;
        public event Action<DocumentTab> Finished;
        public event Action<DocumentTab, string> PageError;

        public ConversionQueue(SynchronizationContext context)
        {
            ui = context;
            worker = new Thread(Run);
            worker.SetApartmentState(ApartmentState.STA);
            worker.IsBackground = true;
            worker.Start();
        }

        public void Enqueue(DocumentTab tab)
        {
            if (disposed) return;
            tab.State = TabState.Queued;
            try { queue.Add(tab); }
            catch (InvalidOperationException) { }
        }

        private void Post(Action action)
        {
            if (ui != null) ui.Post(delegate { action(); }, null);
            else action();
        }

        private void Run()
        {
            try
            {
                foreach (DocumentTab tab in queue.GetConsumingEnumerable())
                {
                    if (tab.Cancel.IsCancellationRequested)
                    {
                        DocumentTab captured = tab;
                        Post(delegate { if (Finished != null) Finished(captured); });
                        continue;
                    }
                    Process(tab);
                }
            }
            catch (ObjectDisposedException)
            {
            }
            catch (InvalidOperationException)
            {
            }
            finally
            {
                try { powerPoint.Shutdown(); }
                catch { }
            }
        }

        private void Process(DocumentTab tab)
        {
            IPageRenderer doc = null;
            try
            {
                tab.State = TabState.Opening;

                string cache = AppPaths.NewDocumentCache(tab.Id);

                doc = new PptDocument(powerPoint, tab.FilePath, cache);

                if (tab.Cancel.IsCancellationRequested) return;

                int total = doc.PageCount;
                double aspect = doc.Aspect;
                tab.PageCount = total;
                tab.State = TabState.Converting;

                DocumentTab t1 = tab;
                Post(delegate { if (Opened != null) Opened(t1, total, aspect); });

                string fullDir = Path.Combine(cache, "full");
                string thumbDir = Path.Combine(cache, "thumb");

                for (int i = 1; i <= total; i++)
                {
                    if (tab.Cancel.IsCancellationRequested) break;

                    string full = Path.Combine(fullDir, string.Format("p{0}.png", i));
                    string thumb = Path.Combine(thumbDir, string.Format("p{0}.png", i));

                    try
                    {
                        doc.RenderPage(i, full, thumb);
                    }
                    catch (Exception ex)
                    {
                        int failedPage = i;
                        string message = ex.Message;
                        DocumentTab failedTab = tab;
                        Post(delegate
                        {
                            if (PageError != null) PageError(failedTab, string.Format("第 {0} 页渲染失败：{1}", failedPage, message));
                        });
                        break;
                    }

                    int pageNumber = i;
                    DocumentTab current = tab;
                    Post(delegate
                    {
                        if (PageReady != null) PageReady(current, pageNumber, full, thumb);
                        if (Progressed != null) Progressed(current, pageNumber, total);
                    });

                    if (tab.Cancel.IsCancellationRequested) break;
                }
            }
            catch (Exception ex)
            {
                string message = ex.Message;
                DocumentTab failed = tab;
                Post(delegate { if (Failed != null) Failed(failed, message); });
            }
            finally
            {
                if (doc != null)
                {
                    try { doc.Dispose(); }
                    catch { }
                }

                DocumentTab done = tab;
                Post(delegate { if (Finished != null) Finished(done); });

                if (queue.Count == 0)
                {
                    try { powerPoint.Shutdown(); }
                    catch { }
                }
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            try { queue.CompleteAdding(); }
            catch { }
            try { worker.Join(6000); }
            catch { }
        }
    }
}
