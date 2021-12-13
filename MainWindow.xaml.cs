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
        public MainWindow() {
            InitializeComponent();
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

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            // デフォルトのペンの色は赤。
            mPLMgr = new PageListMgr(mCanvas, mImage, new SolidColorBrush(Colors.Red));
            mRBCRed.IsChecked = true;

            mInitialized = true;
            UpdateUI();
        }

        private void Undo() {
            mPLMgr.DOMgr.Undo();
        }

        private void Redo() {
            mPLMgr.DOMgr.Redo();
        }

        private void UpdateUI() {
            Console.WriteLine("UpdateUI()");

            mButtonUndo.IsEnabled = mPLMgr.DOMgr.CanUndo();
            mButtonRedo.IsEnabled = mPLMgr.DOMgr.CanRedo();

            // ページ番号関連。
            mTBPageNr.Text           = string.Format("{0}",  mPLMgr.CurPageNr + 1);
            mLabelTotalPages.Content = string.Format("/{0}", mPLMgr.PageCount);

            System.Diagnostics.Debug.Assert(mPLMgr.CurPageNr < mPLMgr.PageCount);

            mButtonNext.IsEnabled = 0 < mPLMgr.PageCount;
            mButtonPrev.IsEnabled = 0 < mPLMgr.PageCount;

            mSliderPageNr.Maximum = mPLMgr.PageCount - 1;
            mSliderPageNr.Value = mPLMgr.CurPageNr;

            mButtonTagBack.IsEnabled = 0 <= mPageNrBeforeJump && mPageNrBeforeJump < mPLMgr.PageCount;

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

        private bool Load() {
            // OpenFileDialogを開いて開くファイル名を取得。
            var ofd = new OpenFileDialog();
            ofd.DefaultExt = DefaultExt;
            ofd.CheckFileExists = true;
            var r = ofd.ShowDialog();
            if (r != true) {
                return false;
            }
            string path = ofd.FileName;
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
            for(int i=0; i<mLBPageTags.Items.Count; ++i) {
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

        // File menu ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

        private void MenuItemFileNew_Click(object sender, RoutedEventArgs e) {
            New();
        }

        private void MenuItemFileOpen_Click(object sender, RoutedEventArgs e) {
            // ファイルからロードします。
            Load();
        }

        private void MenuItemFileSave_Click(object sender, RoutedEventArgs e) {
            if (mSavePath.Length == 0) {
                SaveAs();
            } else {
                Save();
            }
            UpdateUI();
        }


        private void MenuItemFileSaveAs_Click(object sender, RoutedEventArgs e) {
            SaveAs();
            UpdateUI();
        }

        private void MenuItemFileExit_Click(object sender, RoutedEventArgs e) {
            Close();
        }

        // Edit menu ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

        private void MenuItemEditUndo_Click(object sender, RoutedEventArgs e) {
            Undo();
            UpdateUI();
        }

        private void MenuItemEditRedo_Click(object sender, RoutedEventArgs e) {
            Redo();
            UpdateUI();
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
            e.CanExecute = mPLMgr.DOMgr.CanUndo();
        }
        private void EditRedoCmdCanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = mPLMgr.DOMgr.CanRedo();
        }

        // Mouse Events ■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■■

        private void Canvas_MouseMove(object sender, MouseEventArgs e) {
            mPLMgr.DOMgr.MouseMove(sender, e);
        }

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            mPLMgr.DOMgr.MouseLeftButtonDown(sender, e);
        }

        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            mPLMgr.DOMgr.MouseLeftButtonUp(sender, e);
            UpdateUI();
        }

        private void Canvas_MouseRightButtonUp(object sender, MouseButtonEventArgs e) {
            mPLMgr.DOMgr.MouseRightButtonUp(sender, e);
            UpdateUI();
        }

        public void HitObjectMoved(Object tag, HitObjectShape hos, Rect xywh) {
            Console.WriteLine("HitObjectMoved {0} {1} {2} {3} {4}", tag, xywh.Left, xywh.Top, xywh.Width, xywh.Height);
        }

        private void mRBCWhite_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPLMgr.DOMgr.SetCurPenBrush(new SolidColorBrush(Colors.White));
        }

        private void mRBCCyan_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPLMgr.DOMgr.SetCurPenBrush(new SolidColorBrush(Colors.Cyan));
        }

        private void mRBCMagenta_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPLMgr.DOMgr.SetCurPenBrush(new SolidColorBrush(Colors.Magenta));
        }

        private void mRBCYellow_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPLMgr.DOMgr.SetCurPenBrush(new SolidColorBrush(Colors.Yellow));
        }

        private void mRBCRed_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPLMgr.DOMgr.SetCurPenBrush(new SolidColorBrush(Colors.Red));
        }

        private void mRBCGreen_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPLMgr.DOMgr.SetCurPenBrush(new SolidColorBrush(Colors.Green));
        }

        private void mRBCBlue_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPLMgr.DOMgr.SetCurPenBrush(new SolidColorBrush(Colors.Blue));
        }

        private void mRBCBlack_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPLMgr.DOMgr.SetCurPenBrush(new SolidColorBrush(Colors.Black));
        }

        private void ButtonUndo_Click(object sender, RoutedEventArgs e) {
            Undo();
            UpdateUI();
        }

        private void ButtonRedo_Click(object sender, RoutedEventArgs e) {
            Redo();
            UpdateUI();
        }

        private void mRBSelect_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPLMgr.DOMgr.SetPenMode(PenModeEnum.PM_Select);
        }

        private void mRBPen_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }
            mPLMgr.DOMgr.SetPenMode(PenModeEnum.PM_Pen);
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
                var dop = mPLMgr.AddNewPage(mPLMgr.CurPageNr+1);
                dop.ImgFilename = fn;
            }

            UpdateUI();
        }

        private void ButtonPrevPage_Click(object sender, RoutedEventArgs e) {
            System.Diagnostics.Debug.Assert(0 < mPLMgr.PageCount);

            int newPgNr = mPLMgr.CurPageNr - 1;
            if (newPgNr < 0) {
                newPgNr = mPLMgr.PageCount - 1;
            }

            ChangePage(newPgNr);
        }

        private void ButtonNextPage_Click(object sender, RoutedEventArgs e) {
            System.Diagnostics.Debug.Assert(0 < mPLMgr.PageCount);

            int newPgNr = mPLMgr.CurPageNr + 1;
            if (mPLMgr.PageCount <= newPgNr) {
                newPgNr = 0;
            }

            ChangePage(newPgNr);
        }

        private void mTBPageNr_TextChanged(object sender, TextChangedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            Console.WriteLine("mTBPageNr_TextChanged()");

            int newPgNr;
            if (!int.TryParse(mTBPageNr.Text, out newPgNr)) {
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
            mPLMgr.DeleteCurPage();
            UpdateUI();
        }

        private void mCBDispDrawings_Checked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            mCanvas.Visibility = Visibility.Visible;
        }

        private void mCBDispDrawings_Unchecked(object sender, RoutedEventArgs e) {
            if (!mInitialized) {
                return;
            }

            mCanvas.Visibility = Visibility.Hidden;
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

            mPLMgr.DOMgr.Clear(DrawObjMgr.ClearMode.CM_NewDU);
            UpdateUI();
        }

        private void SliderPageNr_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) {
            if (!mInitialized) {
                return;
            }

            int newPgNr = (int)mSliderPageNr.Value;

            ChangePage(newPgNr);
        }

        private void mButtonAddTag_Click(object sender, RoutedEventArgs e) {
            // タグ名tagNameを決定。
            string tagName = "Untitled tag";
            if (0 < mTBTagName.Text.Length) {
                tagName = mTBTagName.Text;
            }

            mLBPageTags.Items.Add(new PageTag(tagName, mPLMgr.CurPageNr));
        }

        private void mButtonTagBack_Click(object sender, RoutedEventArgs e) {
            if (mPageNrBeforeJump < 0 || mPLMgr.PageCount <= mPageNrBeforeJump) {
                Console.WriteLine("TagBack Invalid pageNr {0}", mPageNrBeforeJump);
                return;
            }

            ChangePage(mPageNrBeforeJump);

            mPageNrBeforeJump = -1;
            UpdateUI();
        }

        private void LBTags_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (mLBPageTags.SelectedIndex < 0) {
                // 何も選択されてない。
                return;
            }

            mPageNrBeforeJump = mPLMgr.CurPageNr;

            var tnp = mLBPageTags.SelectedItem as PageTag;

            ChangePage(tnp.PageNr);
        }

        private void mLBPageTags_MouseRightButtonUp(object sender, MouseButtonEventArgs e) {
            if (0 <= mLBPageTags.SelectedIndex) { 
                // 選択されたPageTagを削除。
                mLBPageTags.Items.RemoveAt(mLBPageTags.SelectedIndex);
            }
        }

    }
}
