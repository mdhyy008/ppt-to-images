using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SlideShot
{
    internal sealed class MainForm : Form
    {
        private readonly List<DocumentTab> tabs = new List<DocumentTab>();
        private string activeId;

        private Panel toolbar;
        private FluentButton checkAllButton;
        private FluentButton uncheckAllButton;
        private FluentButton invertButton;
        private FluentButton exportButton;
        private Label formatLabel;
        private FluentSegmented formatSegment;
        private FluentTabStrip tabStrip;
        private Panel content;
        private DropZone dropZone;
        private FluentStatusBar statusBar;

        private ConversionQueue queue;
        private SynchronizationContext uiContext;
        private List<string> lastExportDirs = new List<string>();
        private bool exporting;
        private string lastMessage = "";

        public MainForm()
        {
            Text = "PPT导出为图片";
            Font = new Font("Microsoft YaHei UI", 9F);
            AutoScaleMode = AutoScaleMode.Dpi;
            StartPosition = FormStartPosition.CenterScreen;
            ClientSize = new Size(1100, 760);
            MinimumSize = new Size(880, 600);
            BackColor = Theme.Window;
            DoubleBuffered = true;

            BuildMenu();
            BuildControls();
        }

        private void BuildMenu()
        {
            MainMenu menu = new MainMenu();

            MenuItem file = new MenuItem("文件");
            MenuItem open = new MenuItem("打开文档…", delegate { OpenDocuments(); });
            open.Shortcut = Shortcut.CtrlO;
            file.MenuItems.Add(open);
            file.MenuItems.Add(new MenuItem("-"));
            file.MenuItems.Add(new MenuItem("退出", delegate { Close(); }));

            MenuItem help = new MenuItem("帮助");
            help.MenuItems.Add(new MenuItem("关于", delegate
            {
                using (AboutForm about = new AboutForm())
                    about.ShowDialog(this);
            }));

            menu.MenuItems.Add(file);
            menu.MenuItems.Add(help);
            Menu = menu;
        }

        private void BuildControls()
        {
            toolbar = new Panel();
            toolbar.BackColor = Theme.Window;
            Controls.Add(toolbar);

            checkAllButton = MakeToolbarButton("全选", delegate { WithActiveTab(delegate(DocumentTab t) { t.Grid.SetAllChecked(true); }); });
            uncheckAllButton = MakeToolbarButton("全不选", delegate { WithActiveTab(delegate(DocumentTab t) { t.Grid.SetAllChecked(false); }); });
            invertButton = MakeToolbarButton("反选", delegate { WithActiveTab(delegate(DocumentTab t) { t.Grid.InvertChecked(); }); });

            formatLabel = new Label();
            formatLabel.Text = "格式";
            formatLabel.AutoSize = true;
            formatLabel.ForeColor = Theme.TextMuted;
            formatLabel.BackColor = Theme.Window;
            formatLabel.Font = Font;
            toolbar.Controls.Add(formatLabel);

            formatSegment = new FluentSegmented();
            formatSegment.SetItems("PNG", "JPG");
            toolbar.Controls.Add(formatSegment);

            exportButton = MakeToolbarButton("导出选中", delegate { ExportChecked(); });
            exportButton.AccentStyle = true;

            tabStrip = new FluentTabStrip();
            tabStrip.Visible = false;
            tabStrip.SelectionChanged += delegate(object sender, EventArgs e)
            {
                string id = sender as string;
                if (id != null) ActivateTab(id);
            };
            tabStrip.CloseRequested += delegate(object sender, EventArgs e)
            {
                string id = sender as string;
                if (id != null) CloseTab(id);
            };
            Controls.Add(tabStrip);

            content = new Panel();
            content.BackColor = Theme.Window;
            Controls.Add(content);

            dropZone = new DropZone();
            dropZone.Dock = DockStyle.Fill;
            content.Controls.Add(dropZone);

            statusBar = new FluentStatusBar();
            statusBar.CancelClicked += delegate { CancelCurrent(); };
            statusBar.OpenFolderClicked += delegate { OpenExportFolder(); };
            Controls.Add(statusBar);

            WireDrop(this);
            UpdateUi();
        }

        private FluentButton MakeToolbarButton(string text, EventHandler onClick)
        {
            FluentButton b = new FluentButton();
            b.Text = text;
            b.Compact = true;
            b.Click += onClick;
            toolbar.Controls.Add(b);
            return b;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            uiContext = SynchronizationContext.Current;
            if (uiContext == null)
                uiContext = new WindowsFormsSynchronizationContext();
            EnsureQueue();
            Native.ApplyWindowChrome(Handle, Theme.IsDark);
            LayoutEverything();
        }

        private void EnsureQueue()
        {
            if (queue != null) return;
            queue = new ConversionQueue(uiContext);
            queue.Opened += QueueOpened;
            queue.Progressed += QueueProgressed;
            queue.PageReady += QueuePageReady;
            queue.PageError += QueuePageError;
            queue.Failed += QueueFailed;
            queue.Finished += QueueFinished;
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);
            LayoutEverything();
        }

        private bool layingOut;

        private void LayoutEverything()
        {
            if (toolbar == null || layingOut) return;
            layingOut = true;
            try
            {
                LayoutCore();
            }
            finally
            {
                layingOut = false;
            }
        }

        private void LayoutCore()
        {
            if (toolbar == null || tabStrip == null || content == null || statusBar == null) return;

            int width = ClientSize.Width;
            int height = ClientSize.Height;
            int toolbarHeight = Draw.Scale(this, 56);
            int tabHeight = Draw.Scale(this, 40);
            int statusHeight = Draw.Scale(this, 46);

            toolbar.SetBounds(0, 0, width, toolbarHeight);
            int top = toolbarHeight;

            tabStrip.Visible = tabs.Count > 0;
            if (tabStrip.Visible)
            {
                tabStrip.SetBounds(0, top, width, tabHeight);
                top += tabHeight;
            }

            int contentHeight = height - top - statusHeight;
            if (contentHeight < 0) contentHeight = 0;
            content.SetBounds(0, top, width, contentHeight);
            statusBar.SetBounds(0, height - statusHeight, width, statusHeight);

            LayoutToolbar();
        }

        private void LayoutToolbar()
        {
            if (toolbar == null || exportButton == null || formatSegment == null || formatLabel == null ||
                checkAllButton == null || uncheckAllButton == null || invertButton == null) return;
            Size exportSize = exportButton.PreferredSize;
            int pad = Draw.Scale(this, 16);
            int gap = Draw.Scale(this, 8);
            int right = toolbar.Width - pad;

            int exportY = (toolbar.Height - exportSize.Height) / 2;
            exportButton.SetBounds(right - exportSize.Width, exportY, exportSize.Width, exportSize.Height);
            right = exportButton.Left - Draw.Scale(this, 14);

            Size segSize = formatSegment.GetPreferredSize(Size.Empty);
            formatSegment.SetBounds(right - segSize.Width, (toolbar.Height - segSize.Height) / 2, segSize.Width, segSize.Height);
            right = formatSegment.Left - Draw.Scale(this, 8);

            Size labelSize = formatLabel.GetPreferredSize(Size.Empty);
            int labelY = (toolbar.Height - labelSize.Height) / 2;
            formatLabel.SetBounds(right - labelSize.Width, labelY, labelSize.Width, labelSize.Height);

            int x = pad;
            FluentButton[] leftButtons = new FluentButton[] { checkAllButton, uncheckAllButton, invertButton };
            for (int i = 0; i < leftButtons.Length; i++)
            {
                Size s = leftButtons[i].PreferredSize;
                leftButtons[i].SetBounds(x, (toolbar.Height - s.Height) / 2, s.Width, s.Height);
                x += s.Width + gap;
            }
        }

        private DocumentTab ActiveTab
        {
            get
            {
                if (activeId == null) return null;
                return FindTab(activeId);
            }
        }

        private DocumentTab FindTab(string id)
        {
            for (int i = 0; i < tabs.Count; i++)
                if (tabs[i].Id == id) return tabs[i];
            return null;
        }

        private void WithActiveTab(Action<DocumentTab> action)
        {
            DocumentTab tab = ActiveTab;
            if (tab == null) return;
            action(tab);
            UpdateUi();
        }

        private void UpdateUi()
        {
            DocumentTab tab = ActiveTab;
            bool hasTab = tab != null;
            int cardCount = hasTab ? tab.Grid.Cards.Count : 0;
            int checkedCount = 0;
            for (int i = 0; i < tabs.Count; i++) checkedCount += tabs[i].Grid.CheckedCount;

            checkAllButton.Enabled = cardCount > 0;
            uncheckAllButton.Enabled = cardCount > 0;
            invertButton.Enabled = cardCount > 0;
            exportButton.Enabled = checkedCount > 0 && !exporting;

            dropZone.Visible = tabs.Count == 0;
            LayoutEverything();
        }

        private static bool IsSupported(string path)
        {
            string ext = Path.GetExtension(path).ToLowerInvariant();
            return ext == ".ppt" || ext == ".pptx" || ext == ".pps" || ext == ".ppsx" ||
                   ext == ".pptm" || ext == ".pot" || ext == ".potx";
        }

        private DocumentTab FindTabByPath(string path)
        {
            string full = Path.GetFullPath(path);
            for (int i = 0; i < tabs.Count; i++)
                if (string.Equals(tabs[i].FilePath, full, StringComparison.OrdinalIgnoreCase)) return tabs[i];
            return null;
        }

        public void AddFiles(IList<string> files)
        {
            if (files == null || files.Count == 0) return;
            EnsureQueue();

            int added = 0;
            for (int i = 0; i < files.Count; i++)
            {
                string file = files[i];
                if (!IsSupported(file))
                {
                    SetMessage("不支持的文件：" + Path.GetFileName(file), true);
                    continue;
                }
                if (!File.Exists(file))
                {
                    SetMessage("文件不存在：" + Path.GetFileName(file), true);
                    continue;
                }
                DocumentTab existingTab = FindTabByPath(file);
                if (existingTab != null)
                {
                    ActivateTab(existingTab.Id);
                    continue;
                }
                CreateTab(file);
                added++;
            }

            if (added > 0)
                SetMessage("", false);
        }

        private void CreateTab(string path)
        {
            DocumentTab tab = new DocumentTab();
            tab.FilePath = Path.GetFullPath(path);
            tab.FileName = Path.GetFileName(path);
            tab.BaseName = Path.GetFileNameWithoutExtension(path);

            ThumbnailGrid grid = new ThumbnailGrid();
            grid.Dock = DockStyle.Fill;
            grid.Visible = false;
            grid.SelectionChanged += delegate { UpdateUi(); };
            grid.PreviewRequested += delegate(object sender, EventArgs e)
            {
                ThumbnailCard card = sender as ThumbnailCard;
                if (card != null) OpenPreview(tab, card);
            };
            tab.Grid = grid;
            content.Controls.Add(grid);
            grid.SendToBack();
            WireDrop(grid);

            tabs.Add(tab);
            tabStrip.Add(tab.Id, tab.FileName);
            tabStrip.SetProgress(tab.Id, "准备中");
            ActivateTab(tab.Id);
            EnsureQueue();
            queue.Enqueue(tab);
            UpdateUi();
        }

        private void ActivateTab(string id)
        {
            if (id == null) return;
            DocumentTab tab = FindTab(id);
            if (tab == null) return;

            activeId = id;
            tabStrip.Select(id);
            for (int i = 0; i < tabs.Count; i++)
            {
                bool visible = tabs[i].Id == id;
                tabs[i].Grid.Visible = visible;
                if (visible) tabs[i].Grid.BringToFront();
            }
            UpdateUi();
        }

        private void CloseTab(string id)
        {
            DocumentTab tab = FindTab(id);
            if (tab == null) return;

            try { tab.Cancel.Cancel(); }
            catch { }

            int index = tabs.IndexOf(tab);
            tabs.Remove(tab);
            tabStrip.Remove(id);
            content.Controls.Remove(tab.Grid);
            try
            {
                tab.Grid.ClearCards();
                tab.Grid.Dispose();
            }
            catch
            {
            }
            if (!tab.IsBusy) AppPaths.DropDocumentCache(tab.Id);

            if (tabs.Count == 0)
            {
                activeId = null;
            }
            else if (activeId == id)
            {
                int next = Math.Min(index, tabs.Count - 1);
                activeId = tabs[next].Id;
            }

            if (activeId != null) ActivateTab(activeId);

            bool anyBusy = false;
            for (int i = 0; i < tabs.Count; i++) if (tabs[i].IsBusy) anyBusy = true;
            if (!anyBusy)
            {
                statusBar.SetProgress(false, 0);
                statusBar.CancelVisible = false;
            }

            UpdateUi();
        }

        private void OpenPreview(DocumentTab tab, ThumbnailCard card)
        {
            List<PageItem> ready = new List<PageItem>();
            for (int i = 0; i < tab.Pages.Count; i++)
                if (!string.IsNullOrEmpty(tab.Pages[i].FullPath)) ready.Add(tab.Pages[i]);
            if (ready.Count == 0) return;

            int start = ready.IndexOf(card.Page);
            if (start < 0) start = 0;
            using (PreviewForm preview = new PreviewForm(this, ready, start))
                preview.ShowDialog(this);
        }

        private void OpenDocuments()
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Title = "打开文档";
                dialog.Filter = "PowerPoint (*.ppt;*.pptx;*.pps;*.ppsx;*.pptm;*.pot;*.potx)" +
                    "|*.ppt;*.pptx;*.pps;*.ppsx;*.pptm;*.pot;*.potx";
                dialog.Multiselect = true;
                if (dialog.ShowDialog(this) != DialogResult.OK) return;
                AddFiles(dialog.FileNames);
            }
        }

        private void SetMessage(string text, bool error)
        {
            lastMessage = text ?? "";
            statusBar.SetMessage(lastMessage, error);
        }

        private void CancelCurrent()
        {
            DocumentTab busy = null;
            for (int i = 0; i < tabs.Count; i++)
                if (tabs[i].IsBusy) busy = tabs[i];
            if (busy == null) return;
            try { busy.Cancel.Cancel(); }
            catch { }
            SetMessage("正在取消…", false);
        }

        private void OpenExportFolder()
        {
            for (int i = 0; i < lastExportDirs.Count; i++)
            {
                try { Process.Start("explorer.exe", "\"" + lastExportDirs[i] + "\""); }
                catch { }
            }
        }

        // ---------- 转换事件 ----------

        private void QueueOpened(DocumentTab tab, int pageCount, double aspect)
        {
            if (FindTab(tab.Id) == null) return;

            tab.PageCount = pageCount;
            tab.State = TabState.Converting;
            tab.Pages.Clear();
            tab.Grid.ClearCards();
            tab.Grid.SetAspect(aspect);

            for (int i = 1; i <= pageCount; i++)
            {
                PageItem item = new PageItem();
                item.Number = i;
                tab.Pages.Add(item);
                ThumbnailCard card = tab.Grid.AddPage(item, false);
                WireDrop(card);
            }
            tab.Grid.Reflow();

            tabStrip.SetProgress(tab.Id, "0/" + pageCount);
            SetMessage("正在转换 " + tab.FileName + "…", false);
            statusBar.SetProgress(true, 0);
            statusBar.CancelVisible = true;
            UpdateUi();
        }

        private void QueuePageReady(DocumentTab tab, int pageNumber, string fullPath, string thumbPath)
        {
            if (FindTab(tab.Id) == null) return;
            if (pageNumber < 1 || pageNumber > tab.Pages.Count) return;

            PageItem item = tab.Pages[pageNumber - 1];
            item.FullPath = fullPath;
            item.ThumbPath = thumbPath;

            if (pageNumber - 1 < tab.Grid.Cards.Count)
                tab.Grid.Cards[pageNumber - 1].LoadThumbnail();
        }

        private void QueueProgressed(DocumentTab tab, int done, int total)
        {
            if (FindTab(tab.Id) == null) return;
            tab.Rendered = done;
            tabStrip.SetProgress(tab.Id, done + "/" + total);
            statusBar.SetProgress(true, total == 0 ? 0 : (double)done / total);
            SetMessage(string.Format("正在转换 {0}（{1}/{2}）", tab.FileName, done, total), false);
        }

        private void QueuePageError(DocumentTab tab, string message)
        {
            if (FindTab(tab.Id) == null) return;
            SetMessage(tab.FileName + "：" + message, true);
        }

        private void QueueFailed(DocumentTab tab, string message)
        {
            if (FindTab(tab.Id) == null)
            {
                AppPaths.DropDocumentCache(tab.Id);
                return;
            }
            SetMessage(tab.FileName + "：" + message, true);
            CloseTab(tab.Id);
            statusBar.SetProgress(false, 0);
            statusBar.CancelVisible = false;
            UpdateUi();
        }

        private void QueueFinished(DocumentTab tab)
        {
            DocumentTab current = FindTab(tab.Id);
            if (current == null)
            {
                AppPaths.DropDocumentCache(tab.Id);
                return;
            }

            bool cancelled = current.Cancel.IsCancellationRequested;
            current.State = cancelled ? TabState.Cancelled : TabState.Ready;

            tabStrip.SetProgress(tab.Id, cancelled ? "已取消" : "");

            bool anyBusy = false;
            for (int i = 0; i < tabs.Count; i++) if (tabs[i].IsBusy) anyBusy = true;
            if (!anyBusy)
            {
                statusBar.SetProgress(false, 0);
                statusBar.CancelVisible = false;
            }

            if (cancelled)
                SetMessage(current.FileName + "：已取消", false);
            else
                SetMessage(string.Format("{0}：共 {1} 页", current.FileName, current.PageCount), false);

            UpdateUi();
        }

        // ---------- 导出 ----------

        private void ExportChecked()
        {
            if (exporting) return;

            List<DocumentTab> targets = new List<DocumentTab>();
            List<List<PageItem>> pageLists = new List<List<PageItem>>();
            int totalCount = 0;

            for (int i = 0; i < tabs.Count; i++)
            {
                List<PageItem> pages = tabs[i].Grid.GetCheckedPages();
                if (pages.Count == 0) continue;
                targets.Add(tabs[i]);
                pageLists.Add(pages);
                totalCount += pages.Count;
            }

            if (targets.Count == 0) return;

            for (int i = 0; i < pageLists.Count; i++)
                for (int j = 0; j < pageLists[i].Count; j++)
                    if (string.IsNullOrEmpty(pageLists[i][j].FullPath))
                    {
                        SetMessage("还有页面在转换中，请稍候", true);
                        return;
                    }

            bool jpg = formatSegment.SelectedIndex == 1;
            string ext = jpg ? ".jpg" : ".png";

            StringBuilder message = new StringBuilder();
            message.AppendFormat("导出 {0} 张图片到：", totalCount);
            int shown = Math.Min(targets.Count, 10);
            for (int i = 0; i < shown; i++)
            {
                message.Append("\r\n");
                message.Append(targets[i].ExportDir);
            }
            if (targets.Count > shown)
            {
                message.Append("\r\n…共 ");
                message.Append(targets.Count);
                message.Append(" 个文件夹");
            }

            int confirm = ConfirmDialog.Ask(this, "导出图片", message.ToString(), "导出", "取消");
            if (confirm != 0) return;

            bool anyExists = false;
            for (int i = 0; i < targets.Count && !anyExists; i++)
            {
                DocumentTab tab = targets[i];
                int pad = Math.Max(2, tab.PageCount.ToString().Length);
                List<PageItem> pages = pageLists[i];
                for (int j = 0; j < pages.Count; j++)
                {
                    string target = Path.Combine(tab.ExportDir,
                        string.Format("{0}_{1}{2}", tab.BaseName, pages[j].Number.ToString("D" + pad), ext));
                    if (File.Exists(target)) { anyExists = true; break; }
                }
            }

            bool overwrite = true;
            if (anyExists)
            {
                int answer = ConfirmDialog.Ask(this, "文件已存在", "目标文件夹里已有同名图片。",
                    "全部覆盖", "全部跳过");
                if (answer < 0) return;
                overwrite = answer == 0;
            }

            RunExport(targets, pageLists, jpg, ext, overwrite);
        }

        private void RunExport(List<DocumentTab> targets, List<List<PageItem>> pageLists, bool jpg, string ext, bool overwrite)
        {
            exporting = true;
            statusBar.OpenFolderVisible = false;
            statusBar.SetProgress(true, 0);
            SetMessage("正在导出…", false);
            UpdateUi();

            int total = 0;
            for (int i = 0; i < pageLists.Count; i++) total += pageLists[i].Count;

            List<string> dirs = new List<string>();
            for (int i = 0; i < targets.Count; i++) dirs.Add(targets[i].ExportDir);

            Task.Run(delegate
            {
                int exported = 0;
                int skipped = 0;
                int progress = 0;
                string error = null;
                try
                {
                    for (int t = 0; t < targets.Count; t++)
                    {
                        DocumentTab tab = targets[t];
                        List<PageItem> pages = pageLists[t];
                        string dir = tab.ExportDir;
                        int pad = Math.Max(2, tab.PageCount.ToString().Length);
                        Directory.CreateDirectory(dir);

                        for (int i = 0; i < pages.Count; i++)
                        {
                            PageItem page = pages[i];
                            string name = string.Format("{0}_{1}{2}", tab.BaseName, page.Number.ToString("D" + pad), ext);
                            string target = Path.Combine(dir, name);
                            progress++;

                            if (File.Exists(target) && !overwrite)
                            {
                                skipped++;
                            }
                            else
                            {
                                if (jpg) ImageTools.SaveJpeg(page.FullPath, target);
                                else File.Copy(page.FullPath, target, true);
                                exported++;
                            }

                            int current = progress;
                            Ui(delegate
                            {
                                statusBar.SetProgress(true, (double)current / total);
                            });
                        }
                    }
                }
                catch (Exception ex)
                {
                    error = ex.Message;
                }

                int exportedCount = exported;
                int skippedCount = skipped;
                Ui(delegate
                {
                    exporting = false;
                    statusBar.SetProgress(false, 0);
                    statusBar.OpenFolderVisible = true;
                    lastExportDirs = dirs;

                    string where = dirs.Count == 1 ? dirs[0] : dirs.Count + " 个文件夹";
                    if (error != null)
                    {
                        SetMessage("导出失败：" + error, true);
                    }
                    else if (exportedCount == 0)
                    {
                        SetMessage("没有导出（图片均已存在）", false);
                    }
                    else if (skippedCount > 0)
                    {
                        SetMessage(string.Format("已导出 {0} 张，跳过 {1} 张到 {2}", exportedCount, skippedCount, where), false);
                    }
                    else
                    {
                        SetMessage(string.Format("已导出 {0} 张到 {1}", exportedCount, where), false);
                    }
                    UpdateUi();
                });
            });
        }

        private void Ui(Action action)
        {
            if (IsDisposed) return;
            try { BeginInvoke(action); }
            catch { }
        }

        // ---------- 拖放 ----------

        private void WireDrop(Control control)
        {
            if (control == null || control.AllowDrop) return;
            control.AllowDrop = true;
            control.DragEnter += HandleDragEnter;
            control.DragOver += HandleDragOver;
            control.DragLeave += HandleDragLeave;
            control.DragDrop += HandleDragDrop;
            for (int i = 0; i < control.Controls.Count; i++) WireDrop(control.Controls[i]);
        }

        private static string[] ExtractFiles(DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return null;
            return e.Data.GetData(DataFormats.FileDrop) as string[];
        }

        private static bool HasSupportedFile(string[] files)
        {
            if (files == null) return false;
            for (int i = 0; i < files.Length; i++)
                if (files[i] != null && IsSupported(files[i])) return true;
            return false;
        }

        private void HandleDragEnter(object sender, DragEventArgs e)
        {
            string[] files = ExtractFiles(e);
            if (HasSupportedFile(files))
            {
                e.Effect = DragDropEffects.Copy;
                SetDragHover(true);
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void HandleDragOver(object sender, DragEventArgs e)
        {
            string[] files = ExtractFiles(e);
            e.Effect = HasSupportedFile(files) ? DragDropEffects.Copy : DragDropEffects.None;
        }

        private void HandleDragLeave(object sender, EventArgs e)
        {
            if (!ClientRectangle.Contains(PointToClient(Cursor.Position)))
                SetDragHover(false);
        }

        private void HandleDragDrop(object sender, DragEventArgs e)
        {
            SetDragHover(false);
            string[] files = ExtractFiles(e);
            if (files == null || files.Length == 0) return;
            AddFiles(files);
        }

        private void SetDragHover(bool value)
        {
            DocumentTab tab = ActiveTab;
            if (tab != null) tab.Grid.DragHover = value;
            else dropZone.DragHover = value;
        }

        // ---------- 主题与生命周期 ----------

        private void ApplyTheme()
        {
            BackColor = Theme.Window;
            toolbar.BackColor = Theme.Window;
            content.BackColor = Theme.Window;
            formatLabel.BackColor = Theme.Window;
            formatLabel.ForeColor = Theme.TextMuted;
            tabStrip.Invalidate();
            statusBar.Invalidate();
            dropZone.Invalidate();
            Invalidate(true);
            Native.ApplyWindowChrome(Handle, Theme.IsDark);
            for (int i = 0; i < tabs.Count; i++)
            {
                tabs[i].Grid.BackColor = Theme.Window;
                tabs[i].Grid.Invalidate(true);
                Native.ApplyDarkScrollbars(tabs[i].Grid.Handle);
            }
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_SETTINGCHANGE = 0x001A;
            const int WM_THEMECHANGED = 0x031A;

            if (m.Msg == WM_SETTINGCHANGE || m.Msg == WM_THEMECHANGED)
            {
                bool wasDark = Theme.IsDark;
                Theme.Reload();
                if (wasDark != Theme.IsDark) ApplyTheme();
            }
            base.WndProc(ref m);
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            for (int i = 0; i < tabs.Count; i++)
            {
                try { tabs[i].Cancel.Cancel(); }
                catch { }
            }
            if (queue != null)
            {
                queue.Opened -= QueueOpened;
                queue.Progressed -= QueueProgressed;
                queue.PageReady -= QueuePageReady;
                queue.PageError -= QueuePageError;
                queue.Failed -= QueueFailed;
                queue.Finished -= QueueFinished;
            }
            base.OnFormClosing(e);
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (queue != null)
            {
                queue.Dispose();
                queue = null;
            }
            AppPaths.ClearCache();
            base.OnFormClosed(e);
        }
    }
}
