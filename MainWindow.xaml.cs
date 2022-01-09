using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Linq;

namespace PenTabletNotebook {
    public partial class MainWindow : Window, IHitObjectMoved {
        private void LocalizeUI() {
            mMenuItemFile.Header = Properties.Resources.File;
            mMenuItemFileNew.Header = Properties.Resources.FileNew;
            mMenuItemFileOpen.Header = Properties.Resources.FileOpen;
            mMenuItemFileAddImg.Header = Properties.Resources.FileAddImg;
            mMenuItemFileSave.Header = Properties.Resources.FileSave;
            mMenuItemFileSaveAs.Header = Properties.Resources.FileSaveAs;
            mMenuItemFileExit.Header = Properties.Resources.FileExit;
            mMenuItemEdit.Header = Properties.Resources.Edit;
            mMenuItemEditUndo.Header = Properties.Resources.EditUndo;
            mMenuItemEditRedo.Header = Properties.Resources.EditRedo;
            mGroupBoxSettings.Header = Properties.Resources.Settings;
            mGroupBoxPenMode.Header = Properties.Resources.PenMode;
            mRBPenEraser.Content = Properties.Resources.PenEraser;
            mRBEraser.Content = Properties.Resources.Eraser;
            mGroupBoxPenThickness.Header = Properties.Resources.PenThickness;
            mGroupBoxColor.Header = Properties.Resources.Color;
            mRBCWhite.Content = Properties.Resources.White;
            mRBCCyan.Content = Properties.Resources.Cyan;
            mRBCMagenta.Content = Properties.Resources.Magenta;
            mRBCYellow.Content = Properties.Resources.Yellow;
            mRBCRed.Content = Properties.Resources.Red;
            mRBCGreen.Content = Properties.Resources.Green;
            mRBCBlue.Content = Properties.Resources.Blue;
            mRBCBlack.Content = Properties.Resources.Black;
            mGroupBoxDisplay.Header = Properties.Resources.Display;
            mCBDispDrawings.Content = Properties.Resources.DispDrawings;
            mLabelScale.Content = Properties.Resources.Scale + ":";
            mButtonScaleToImageW.Content = Properties.Resources.ScaleToImageWidth;
            mButtonScaleToFit.Content = Properties.Resources.ScaleToFit;
            mGroupBoxPageControl.Header = Properties.Resources.PageControl;
            mButtonSetImage.Content = Properties.Resources.SetImage;
            mButtonDeletePage.Content = Properties.Resources.DeletePage;
            mButtonAddNewPage.Content = Properties.Resources.AddNewPage;
            mButtonClearDrawings.Content = Properties.Resources.ClearDrawings;
            mGroupBoxPageTags.Header = Properties.Resources.PageTags;
            mButtonTagBack.Content = Properties.Resources.BackBeforeJump;
            mLabelName.Content = Properties.Resources.Name + ":";
            mButtonAddTag.Content = Properties.Resources.NewOrUpdateTag;
            mGroupBoxPageNumber.Header = Properties.Resources.PageNumber;
            mLabelPage.Content = Properties.Resources.Page + ":";
            mButtonPrev.Content = Properties.Resources.Prev;
            mButtonNext.Content = Properties.Resources.Next;
            mGroupBoxCanvas.Header = Properties.Resources.Canvas;
        }

        private bool mInitialized = false;
        private string DefaultExt = ".ptnb";
        private string mSavePath = "";

        private PageListMgr mPLMgr;
        private int mPageNrBeforeJump = -1;

        private static string AssemblyVersion {
            get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(); }
        }

        private void UpdateWindowTitle() {
            var s = string.Format("PenTabletNotebook {0} {1}", AssemblyVersion, mSavePath);
            Title = s;
        }

        private void Undo() {
            UpdateUI();
        }

        private void Redo() {
            UpdateUI();
        }

        private void UpdateUI() {
            Console.WriteLine("UpdateUI()");

            // ページ番号関連。
            mTBPageNr.Text = string.Format("{0}", mPLMgr.CurPageNr + 1);
            mLabelTotalPages.Content = string.Format("/ {0}", mPLMgr.PageCount);

            System.Diagnostics.Debug.Assert(mPLMgr.CurPageNr < mPLMgr.PageCount);

            mButtonNext.IsEnabled = 0 < mPLMgr.PageCount;
            mButtonPrev.IsEnabled = 0 < mPLMgr.PageCount;

            mSliderPageNr.Maximum = mPLMgr.PageCount - 1;
            mSliderPageNr.Value = mPLMgr.CurPageNr;

            mButtonTagBack.IsEnabled = 0 <= mPageNrBeforeJump && mPageNrBeforeJump < mPLMgr.PageCount;

            // タグに該当ページがあるときタグを選択状態にします。
            for (int i=0; i<mLBPageTags.Items.Count; ++i) {
                var pt = mLBPageTags.Items[i] as PageTag;
                if (pt.PageNr == mPLMgr.CurPageNr) {
                    mLBPageTags.SelectedIndex = i;
                    mLBPageTags.ScrollIntoView(pt);
                    break;
                }
            }

            UpdateWindowTitle();
        }

        private bool ChangePage(int pageNr) {
            if (mPLMgr.CurPageNr == pageNr) {
                Console.WriteLine("ChangePage() already shows the page specified : {0}", pageNr);
                return false;
            }
            System.Diagnostics.Debug.Assert(mPLMgr.CurPageNr != pageNr);

            Console.WriteLine("ChangePage() from {0} to {1}", mPLMgr.CurPageNr, pageNr);

            var dop = mPLMgr.ChangePage(pageNr);

            UpdateUI();
            return true;
        }

        private bool OpenFileDialogAndLoad() {
            // OpenFileDialogを開いて開くファイル名を取得。
            var ofd = new OpenFileDialog();
            ofd.DefaultExt = DefaultExt;
            ofd.CheckFileExists = true;
            var r = ofd.ShowDialog();
            if (r != true) {
                return false;
            }

            string path = ofd.FileName;

            return LoadSpecifiedFile(path);
        }

        private bool LoadSpecifiedFile(string path) {
            var ptList = new List<PageTag>();
            bool result = mPLMgr.Load(path, ref ptList);
            if (result) {
                // 読み出し成功。
                mSavePath = path;

                // PageTagリストを更新します。
                mLBPageTags.Items.Clear();
                foreach (var pt in ptList) {
                    mLBPageTags.Items.Add(pt);
                }

                ScaleToFit();

                // 現在表示ページ番号の同期等。
                UpdateUI();
            } else {
                // 読み出し失敗。
                MessageBox.Show("Error Opening File", "Error opening file", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            return result;
        }

        /// <summary>
        /// mSavePathのファイルにセーブします。
        /// </summary>
        private void Save() {
            System.Diagnostics.Debug.Assert(0 < mSavePath.Length);

            var ptList = new List<PageTag>();
            for (int i = 0; i < mLBPageTags.Items.Count; ++i) {
                var pt = mLBPageTags.Items[i] as PageTag;
                ptList.Add(pt);
            }
            ptList.Sort(PageTag.Compare);

            bool result = mPLMgr.Save(mSavePath, ptList);
            if (!result) {
                MessageBox.Show("Error Saving File", "Error saving file {0}", MessageBoxButton.OK, MessageBoxImage.Error);
            }

            UpdateUI();
        }

        private void SaveAs() {
            var sfd = new SaveFileDialog();
            sfd.DefaultExt = DefaultExt;
            var r = sfd.ShowDialog();
            if (r != true) {
                return;
            }

            mSavePath = sfd.FileName;
            Save();
        }

        private void New() {
            mPLMgr.ClearAndNewPage();
            mImage.Source = null;
            mLBPageTags.Items.Clear();
            mPageNrBeforeJump = -1;
            UpdateUI();
        }

        private void DeleteCurPage() {
            mPLMgr.DeleteCurPage();

            // Backボタンで戻るページ番号の更新。
            if (0 <= mPageNrBeforeJump && mPLMgr.CurPageNr <= mPageNrBeforeJump) {
                --mPageNrBeforeJump;
            }

            UpdateUI();
        }

        private void NextPage() {
            System.Diagnostics.Debug.Assert(0 < mPLMgr.PageCount);

            if (mPLMgr.PageCount == 1) {
                return;
            }

            int newPgNr = mPLMgr.CurPageNr + 1;
            if (mPLMgr.PageCount <= newPgNr) {
                newPgNr = 0;
            }

            ChangePage(newPgNr);
        }

        private void PrevPage() {
            System.Diagnostics.Debug.Assert(0 < mPLMgr.PageCount);

            if (mPLMgr.PageCount == 1) {
                return;
            }

            int newPgNr = mPLMgr.CurPageNr - 1;
            if (newPgNr < 0) {
                newPgNr = mPLMgr.PageCount - 1;
            }

            ChangePage(newPgNr);
        }

        private void DrawHLines() {
            mPLMgr.DrawHLines((int)mSliderHLine.Value, new SolidColorBrush(Colors.Gray), 1.0);
        }

        private void UndrawHLines() {
            mPLMgr.UndrawHLines();
        }

        private void DrawVLines() {
            mPLMgr.DrawVLines((int)mSliderVLine.Value, new SolidColorBrush(Colors.Gray), 1.0);
        }

        private void UndrawVLines() {
            mPLMgr.UndrawVLines();
        }

        private void ScaleToFit() {
            double imageW = mImage.ActualWidth;
            if (imageW <= 0) {
                imageW = mImage.Width;
            }
            double imageH = mImage.ActualHeight;
            if (imageH <= 0) {
                imageH = mImage.Height;
            }

            double scaleX = mSVCanvas.ViewportWidth / imageW;
            double scaleY = mSVCanvas.ViewportHeight / imageH;
            if (scaleX < scaleY) {
                mSliderScaling.Value = scaleX;
            } else {
                mSliderScaling.Value = scaleY;
            }
        }

        private void ScaleToImage() {
            double imageW = mImage.ActualWidth;
            if (imageW <= 0) {
                imageW = mImage.Width;
            }
            double scaleX = mSVCanvas.ViewportWidth / imageW;
            mSliderScaling.Value = scaleX;
        }

        // アプリ起動 ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

        public MainWindow() {
            InitializeComponent();
            LocalizeUI();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            // デフォルトのペンの色は赤。ペンの太さ==3。
            mPLMgr = new PageListMgr(mInkCanvas, mCanvas, mImage, Colors.Red, 4.0);
            mRBCRed.IsChecked = true;
            mRBT4.IsChecked = true;

            mInitialized = true;

            var args = System.Environment.GetCommandLineArgs();
            if (1 < args.Length) {
                var path = args[1];

                if (0 == ".ptnb".CompareTo(System.IO.Path.GetExtension(path))) {
                    // 第1引数のファイルを開きます。
                    LoadSpecifiedFile(path);
                }
            }

            UpdateUI();
        }

        // File menu ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

        private void MenuItemFileNew_Click(object sender, RoutedEventArgs e) {
            New();
        }

        private void MenuItemFileOpen_Click(object sender, RoutedEventArgs e) {
            // ファイルからロードします。
            OpenFileDialogAndLoad();
        }

        private void MenuItemFileSave_Click(object sender, RoutedEventArgs e) {
            if (mSavePath.Length == 0) {
                SaveAs();
            } else {
                Save();
            }
        }


        private void MenuItemFileSaveAs_Click(object sender, RoutedEventArgs e) {
            SaveAs();
        }

        private void MenuItemFileExit_Click(object sender, RoutedEventArgs e) {
            Close();
        }

        // Edit menu ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

        private void MenuItemEditUndo_Click(object sender, RoutedEventArgs e) {
            Undo();
        }

        private void MenuItemEditRedo_Click(object sender, RoutedEventArgs e) {
            Redo();
        }

        // Command handlings ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

        private void FileNewCmdCanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = true;
        }
        private void FileOpenCmdCanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = true;
        }
        private void FileSaveCmdCanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = true;
        }
        private void FileSaveAsCmdCanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = true;
        }
        private void FileExitCmdCanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = true;
        }

        private void EditUndoCmdCanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = mPLMgr.CanUndo();
        }
        private void EditRedoCmdCanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = mPLMgr.CanRedo();
        }

        // Mouse Events ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

        public void HitObjectMoved(Object tag, HitObjectShape hos, Rect xywh) {
            Console.WriteLine("HitObjectMoved {0} {1} {2} {3} {4}", tag, xywh.Left, xywh.Top, xywh.Width, xywh.Height);
        }

        private void RBCWhite_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPLMgr.SetCurPenColor(Colors.White);
        }

        private void RBCCyan_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPLMgr.SetCurPenColor(Colors.Cyan);
        }

        private void RBCMagenta_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPLMgr.SetCurPenColor(Colors.Magenta);
        }

        private void RBCYellow_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPLMgr.SetCurPenColor(Colors.Yellow);
        }

        private void RBCRed_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPLMgr.SetCurPenColor(Colors.Red);
        }

        private void RBCGreen_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPLMgr.SetCurPenColor(Colors.Green);
        }

        private void RBCBlue_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPLMgr.SetCurPenColor(Colors.Blue);
        }

        private void RBCBlack_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPLMgr.SetCurPenColor(Colors.Black);
        }

        private void ButtonUndo_Click(object sender, RoutedEventArgs e) {
            Undo();
        }

        private void ButtonRedo_Click(object sender, RoutedEventArgs e) {
            Redo();
        }

        private void RBSelect_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPLMgr.SetPenMode(PenModeEnum.PM_Select);
        }

        private void RBPen_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPLMgr.SetPenMode(PenModeEnum.PM_Pen);
        }

        private void RBEraser_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPLMgr.SetPenMode(PenModeEnum.PM_Eraser);
        }

        private void MenuItemFileAddImg_Click(object sender, RoutedEventArgs e) {
            var ofd = new OpenFileDialog();
            ofd.Filter = "Image files (*.BMP;*.JPG;*.PNG;*.TIF)|*.BMP;*.JPG;*.PNG;*.TIF|All files (*.*)|*.*";
            ofd.CheckFileExists = true;
            ofd.Multiselect = true;
            var b = ofd.ShowDialog();
            if (b != true) {
                return;
            }

            var fnList = new List<string>();
            foreach (var fn in ofd.FileNames) {
                fnList.Add(fn);
            }
            fnList.Sort();

            foreach (var fn in fnList) {
                // 今の位置の次のページに追加。
                var dop = mPLMgr.AddNewPage(mPLMgr.CurPageNr + 1);
                dop.ImgFilename = fn;
            }

            UpdateUI();
        }

        private void ButtonPrevPage_Click(object sender, RoutedEventArgs e) {
            PrevPage();
        }

        private void ButtonNextPage_Click(object sender, RoutedEventArgs e) {
            NextPage();
        }

        private void TBPageNr_TextChanged(object sender, TextChangedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            Console.WriteLine("mTBPageNr_TextChanged()");

            if (!int.TryParse(mTBPageNr.Text, out int newPgNr)) {
                return;
            }

            // 画面表示ページ番号は1多いので。
            --newPgNr;

            if (newPgNr < 0 || mPLMgr.PageCount <= newPgNr) {
                // 範囲外の値。入力中かもしれないので無視。
                return;
            }

            ChangePage(newPgNr);
        }

        private void ButtonSetImage_Click(object sender, RoutedEventArgs e) {
            var ofd = new OpenFileDialog();
            ofd.Filter = "Image files (*.BMP;*.JPG;*.PNG;*.TIF)|*.BMP;*.JPG;*.PNG;*.TIF|All files (*.*)|*.*";
            ofd.CheckFileExists = true;
            var b = ofd.ShowDialog();
            if (b != true) {
                return;
            }

            mPLMgr.SetImage(ofd.FileName);
        }

        private void ButtonDeletePage_Click(object sender, RoutedEventArgs e) {
            DeleteCurPage();
        }

        private void CBDispDrawings_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            mInkCanvas.Visibility = Visibility.Visible;
        }

        private void CBDispDrawings_Unchecked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            mInkCanvas.Visibility = Visibility.Hidden;
        }


        private void CBDispHLine_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            DrawHLines();
        }

        private void CBDispHLine_Unchecked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            UndrawHLines();
        }

        private void CBDispVLine_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            DrawVLines();
        }

        private void CBDispVLine_Unchecked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            UndrawVLines();
        }

        private void ButtonAddNewPage_Click(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            mPLMgr.AddNewPage(mPLMgr.CurPageNr + 1);
            mPLMgr.ChangePage(mPLMgr.CurPageNr + 1);
            UpdateUI();
        }

        private void ButtonClearDrawings_Click(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            mInkCanvas.Strokes.Clear();
            UpdateUI();
        }

        private void SliderPageNr_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (!mInitialized) {
                return;
            }

            int newPgNr = (int)mSliderPageNr.Value;

            ChangePage(newPgNr);
        }

        private void ButtonAddTag_Click(object sender, RoutedEventArgs e) {
            // タグ名tagNameを決定。
            string tagName = "Untitled tag";
            if (0 < mTBTagName.Text.Length) {
                tagName = mTBTagName.Text;
            }

            for (int i = mLBPageTags.Items.Count - 1; 0 <= i; --i) {
                var pt = mLBPageTags.Items[i] as PageTag;
                if (pt.PageNr == mPLMgr.CurPageNr) {
                    // 同じページのタグが既にあるので削除。
                    mLBPageTags.Items.RemoveAt(i);
                    // 処理続行。
                }
            }

            var newPageTag = new PageTag(tagName, mPLMgr.CurPageNr);

            bool bAdded = false;
            for (int i = mLBPageTags.Items.Count - 1; 0 <= i; --i) {
                var pt = mLBPageTags.Items[i] as PageTag;
                if (pt.PageNr < mPLMgr.CurPageNr) {
                    mLBPageTags.Items.Insert(i + 1, newPageTag);
                    bAdded = true;
                    mLBPageTags.SelectedIndex = i + 1;
                    mLBPageTags.ScrollIntoView(mLBPageTags.Items[i + 1]);
                    break;
                }
            }
            if (!bAdded) {
                // リストの先頭に追加します。
                mLBPageTags.Items.Insert(0, newPageTag);
                mLBPageTags.SelectedIndex = 0;
            }
        }

        private void ButtonTagBack_Click(object sender, RoutedEventArgs e) {
            if (mPageNrBeforeJump < 0 || mPLMgr.PageCount <= mPageNrBeforeJump) {
                Console.WriteLine("TagBack Invalid pageNr {0}", mPageNrBeforeJump);
                return;
            }

            ChangePage(mPageNrBeforeJump);

            mPageNrBeforeJump = -1;
            UpdateUI();
        }

        private void LBPageTags_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (mLBPageTags.SelectedIndex < 0) {
                // 何も選択されてない。
                return;
            }

            mPageNrBeforeJump = mPLMgr.CurPageNr;

            var tnp = mLBPageTags.SelectedItem as PageTag;

            ChangePage(tnp.PageNr);
        }

        private void LBPageTags_MouseRightButtonUp(object sender, MouseButtonEventArgs e) {
            if (0 <= mLBPageTags.SelectedIndex) {
                // 選択されたPageTagを削除。
                mLBPageTags.Items.RemoveAt(mLBPageTags.SelectedIndex);
            }
        }

        private void Window_KeyUp(object sender, KeyEventArgs e) {
            switch (e.Key) {
            case Key.PageDown:
                NextPage();
                e.Handled = true;
                break;
            case Key.PageUp:
                PrevPage();
                e.Handled = true;
                break;
            default:
                break;
            }
        }

        private void Window_MouseWheel(object sender, MouseWheelEventArgs e) {
            if (0 < e.Delta) {
                // 上に回す。
                PrevPage();
                e.Handled = true;
            } else if (e.Delta < 0) {
                // 下に回す。
                NextPage();
                e.Handled = true;
            }
        }

        private void ButtonScaleToFit_Click(object sender, RoutedEventArgs e) {
            ScaleToFit();
        }

        private void ButtonScaleToImageW_Click(object sender, RoutedEventArgs e) {
            ScaleToImage();
        }

        private void RBT1_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPLMgr.SetCurPenThickness(1.0);
        }
        private void RBT2_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPLMgr.SetCurPenThickness(2.0);
        }
        private void RBT4_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPLMgr.SetCurPenThickness(4.0);
        }
        private void RBT6_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPLMgr.SetCurPenThickness(6.0);
        }
        private void RBT8_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPLMgr.SetCurPenThickness(8.0);
        }
        private void RBT12_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPLMgr.SetCurPenThickness(12.0);
        }

        private void SliderHLine_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (!mInitialized) {
                return;
            }
            DrawHLines();
        }
        private void SliderVLine_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (!mInitialized) {
                return;
            }
            DrawVLines();
        }
    }
}
