using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Ink;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace PenTabletNotebook {
    class PageListMgr {
        private List<DOPage> mPageList = new List<DOPage>();

        private InkCanvas mInkCanvas;
        private Image mImage;

        /// <summary>
        /// 0で始まる番号のページ番号。
        /// </summary>
        private int mCurPageNr = 0;

        private Color mPenColor = Colors.Red;
        private double mPenThickness = 3;

        public int CurPageNr {
            get { return mCurPageNr; }
            set { mCurPageNr = value; }
        }

        public int PageCount {
            get { return mPageList.Count; }
        }

        public DOPage CurPage {
            get { return mPageList[mCurPageNr]; }
        }

        /// <summary>
        /// コピーではなく、オリジナルを戻します。
        /// </summary>
        public IEnumerable<DOPage> PageList() {
            return mPageList;
        }

        // ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

        /// <summary>
        /// ctor.
        /// </summary>
        public PageListMgr(InkCanvas inkCanvas, Image image, Color penColor, double penThickness) {
            mInkCanvas = inkCanvas;
            mImage = image;

            UpdatePenColorThickness(penColor, penThickness);

            // 最初の空ページを作成。
            AddNewPage(0);
        }

        /// <summary>
        /// penColorとpenThicknessの値をメンバ変数に保存、mInkCanvasにセットします。
        /// </summary>
        private void UpdatePenColorThickness(Color penColor, double penThickness) {
            mPenColor = penColor;
            mPenThickness = penThickness;

            var da = new DrawingAttributes();
            da.Color = mPenColor;
            da.Width = mPenThickness;
            da.Height = mPenThickness;
            da.FitToCurve = true;
            mInkCanvas.DefaultDrawingAttributes = da;
        }

        public void SetCurPenColor(Color c) {
            UpdatePenColorThickness(c, mPenThickness);
        }

        public void SetCurPenThickness(double penThickness) {
            UpdatePenColorThickness(mPenColor, penThickness);
        }

        /// <summary>
        /// ページが一つもないとき追加します。
        /// </summary>
        public bool NewPageIfEmpty() {
            if (0 < mPageList.Count) {
                // ページがあるのでNewPageを追加しない。
                return false;
            }

            // 追加します。
            AddNewPage(0);
            return true;
        }

        /// <summary>
        /// ページを位置posに挿入します。mCurPageNrのずれに注意して下さい(0 ≦ pos ≦ mCurPageの場合ずれが生じます)。
        /// </summary>
        /// <param name="pos">0 ≦ pos ≦ mPageList.Count。0のとき最初のページがNewPage。Countのとき最後のページの後ろにNewPage。</param>
        public DOPage AddNewPage(int pos) {
            System.Diagnostics.Debug.Assert(0 <= pos && pos <= mPageList.Count);
            var dop = new DOPage();
            mPageList.Insert(pos, dop);
            return dop;
        }

        /// <summary>
        /// 現在表示中ページをページエントリーにシリアライズします。
        /// </summary>
        public void SerializeCurPage() {
            var dop = mPageList[CurPageNr];
            dop.Serialize(mInkCanvas.Strokes);
        }

        private void ShowImg(string path) {
            if (path == null || path.Length == 0) {
                mImage.Source = null;
                return;
            }

            var bi = new BitmapImage();
            bi.BeginInit();
            bi.UriSource = new Uri(path);
            bi.EndInit();
            bi.Freeze();

            mImage.Source = bi;
        }

        /// <summary>
        /// 現在表示ページの画像セット。
        /// </summary>
        public void SetImage(string path) {
            var dop = CurPage;
            dop.ImgFilename = path;
            ShowImg(path);
        }

        /// <summary>
        /// 指定ページのDrawObjをDOPから実体化しCanvasに追加します。現在表示中ページ番号を更新。
        /// </summary>
        public DOPage ChangePage(int pageNr) {
            if (0 <= mCurPageNr) {
                // 現在表示中のページをシリアライズします。
                SerializeCurPage();
            }

            // Inkキャンバスをクリアー。
            mInkCanvas.Strokes.Clear();

            // Pageをデシリアライズし実体化、mInkCanvasに表示します。
            var dop = mPageList[pageNr];
            var doList = dop.Deserialize(-1, mInkCanvas);

            ShowImg(dop.ImgFilename);

            mCurPageNr = pageNr;
            return dop;
        }

        public bool Load(string path, ref List<PageTag> ptList_return) {
            mInkCanvas.Strokes.Clear();
            mPageList.Clear();
            ptList_return.Clear();

            var sl = new SaveLoad();
            var sc = new SaveCtx();
            sc.pageList = mPageList;
            sc.pageTagList = ptList_return;
            bool b = sl.Load(path, ref sc);
            if (b) {
                // 最初のページを表示。
                mCurPageNr = -1;
                ChangePage(sc.curPageNr);
            } else {
                // 読み出し失敗。
            }

            NewPageIfEmpty();
            return b;
        }

        public bool Save(string path, List<PageTag> ptList) {
            // 現在表示中のページをシリアライズします。
            SerializeCurPage();

            var sl = new SaveLoad();

            for (int i = 0; i < mPageList.Count; ++i) {
                var p = mPageList[i];
                sl.SaveAddPage(p);
            }

            foreach (var pt in ptList) {
                sl.SaveAddPageTag(pt);
            }

            return sl.Save(path, mCurPageNr);
        }

        public void ClearAndNewPage() {
            mInkCanvas.Strokes.Clear();
            mPageList.Clear();
            NewPageIfEmpty();
            mCurPageNr = 0;
        }

        public void DeleteCurPage() {
            System.Diagnostics.Debug.Assert(1 <= PageCount);
            if (PageCount == 1) {
                // 唯一のページが削除された場合。
                ClearAndNewPage();
            } else {
                // 削除するページは現在表示ページmCurPageNr。

                int nextDispPgNr;
                int curPgNrAfterDelete;
                if (mCurPageNr == PageCount -1) {
                    // 最後のページを削除した場合、前のページを表示する。
                    nextDispPgNr = mCurPageNr - 1;
                    curPgNrAfterDelete = mCurPageNr - 1;
                } else {
                    // 削除後に表示するページは基本次のページ。
                    nextDispPgNr = mCurPageNr + 1;
                    curPgNrAfterDelete = mCurPageNr;
                }
                
                ChangePage(nextDispPgNr);

                mPageList.RemoveAt(mCurPageNr);

                mCurPageNr = curPgNrAfterDelete;
            }
        }

        public bool CanUndo() {
            return false;
        }

        public bool CanRedo() {
            return false;
        }

        public void SetPenMode(PenModeEnum pm) {
            switch (pm) {
            case PenModeEnum.PM_Pen:
                mInkCanvas.EditingMode = InkCanvasEditingMode.Ink;
                break;
            case PenModeEnum.PM_Eraser:
                mInkCanvas.EditingMode = InkCanvasEditingMode.EraseByStroke;
                break;
            default:
                System.Diagnostics.Debug.Assert(false);
                break;
            }
        }
    }
}
