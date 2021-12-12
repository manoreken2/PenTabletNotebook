using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PenTabletNotebook {
    class DrawObjMgr {
        private List<DrawObj> mDOList = new List<DrawObj>();
        private List<DrawObj> mDOUndoList = new List<DrawObj>();

        private int mSelectedDOidx = -1;
        private Canvas mCanvas;
        private Brush mBrush;
        private PenModeEnum mPenMode = PenModeEnum.PM_Pen;

        public Canvas GetCanvas() {
            return mCanvas;
        }

        public PenModeEnum PenMode {
            get { return mPenMode; }
        }

        /// <summary>
        /// 中で保持しているDrawObjのリストを戻します。
        /// コピーではなくオリジナルのポインタを戻します。
        /// </summary>
        public IEnumerable<DrawObj> GetDOList() {
            return mDOList;
        }

        /// <summary>
        /// 中で保持しているDrawObjのリストを引数の物に入れ替えます。
        /// doListのオブジェクトは、Canvasに追加してからこの関数を呼びます。
        /// 新しいDOを最後に追加します。
        /// </summary>
        public void SetDOList(IEnumerable<DrawObj> doList) {
            Clear(ClearMode.CM_ZeroObj);
            foreach (var d in doList) {
                mDOList.Add(d);
            }
            NewDU();
        }

        public void SetPenMode(PenModeEnum pm) {
            mPenMode = pm;
            switch (pm) {
                case PenModeEnum.PM_Pen:
                    // 全オブジェクトの描画モードを通常描画に戻す。
                    foreach (var d in mDOList) {
                        d.ModeChangeTo(DrawObj.Mode.M_Draw);
                    }
                    // 選択DO番号を最後のDUにします。
                    System.Diagnostics.Debug.Assert(0 < mDOList.Count);
                    mSelectedDOidx = mDOList.Count - 1;
                    break;
                case PenModeEnum.PM_Select:
                    break;
                default:
                    System.Diagnostics.Debug.Assert(false);
                    break;
            }
        }

        public void SetCurPenBrush(Brush b) {
            mBrush = b;
            CurDU.SetBrush(b);
        }

        private DrawObj CurDU {
            get { 
                return mDOList[mSelectedDOidx];
            }
        }

        public DrawObjMgr(Canvas c, Brush b) {
            mCanvas = c;
            mBrush = b;

            // 最初の1個目の描画オブジェクト作成。選択状態にする。
            NewDU();
        }

        /// <summary>
        /// 新しいDOをリストの最後に追加し選択状態にします。
        /// </summary>
        private void NewDU() {
            var dObj = new DrawObjFreePen(DrawObjNew.mNextIdx++, mCanvas, mBrush);
            mDOList.Add(dObj);
            mSelectedDOidx = mDOList.Count - 1;
        }

        private bool mHitBoxSelected = false;

        public void MouseMove(Object sender, MouseEventArgs e) {
            var msePos = e.GetPosition(mCanvas);

            //Console.WriteLine("MM {0},{1} {2}", (int)msePos.X, (int)msePos.Y, e.LeftButton == MouseButtonState.Pressed ? "L" : "");

            switch (mPenMode) {
                case PenModeEnum.PM_Pen:
                    CurDU.MouseMove(sender, e);
                    break;
                case PenModeEnum.PM_Select:
                    if (e.LeftButton == MouseButtonState.Pressed) {
                        // マウス左ボタン押下中。
                        if (!mHitBoxSelected) {
                            return;
                        }
                        CurDU.HOS.MouseMove(sender, e);
                        CurDU.Resize(CurDU.HOS.XYWH);
                    } else {
                        // ホバー中。
                        if (mHitBoxSelected) {
                            // ヒット矩形選択状態。
                            Console.WriteLine("D: MouseMove HitBox is selected");
                        } else {
                            // ヒットしたらヒット矩形描画モードにします。
                            bool bHit = false;
                            for (int i = mDOList.Count-1; 0 <= i; --i) {
                                var d = mDOList[i];
                                var ha = d.GetHitArea();

                                //Console.WriteLine("d {0} : {1},{2},{3},{4}", i, ha.Left, ha.Top, ha.Right, ha.Bottom);

                                if (!bHit && ha.Contains(msePos)) {
                                    // 当たった。ヒット矩形表示。
                                    if (d.CurrentMode != DrawObj.Mode.M_DrawHitBox) {
                                        d.ModeChangeTo(DrawObj.Mode.M_DrawHitBox);
                                        Console.WriteLine("DHB {0},{1}", (int)msePos.X, (int)msePos.Y);
                                    }
                                    d.HOS.MouseMove(sender, e);
                                    bHit = true;
                                } else {
                                    // 当たらない。ヒット矩形非表示。
                                    if (d.CurrentMode != DrawObj.Mode.M_Draw) {
                                        d.ModeChangeTo(DrawObj.Mode.M_Draw);
                                    }
                                }
                            }
                        }
                    }
                    break;
            }
        }

        public void MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            var msePos = e.GetPosition(mCanvas);

            Console.WriteLine("Ldown {0} {1}", (int)msePos.X, (int)msePos.Y);

            switch (mPenMode) {
                case PenModeEnum.PM_Pen:
                    CurDU.MouseLeftButtonDown(sender, e);
                    break;
                case PenModeEnum.PM_Select:
                    if (mHitBoxSelected) {
                        // 選択中のDrawObjを選択解除。通常描画モードに戻します。
                        var d = mDOList[mSelectedDOidx];
                        d.ModeChangeTo(DrawObj.Mode.M_Draw);
                        mHitBoxSelected = false;
                    }

                    for (int i=mDOList.Count-1; 0<=i; --i) {
                        var d = mDOList[i];
                        if (d.GetHitArea().Contains(msePos)) {
                            // 当たった。ヒット矩形を表示します。
                            mSelectedDOidx = i;
                            d.ModeChangeTo(DrawObj.Mode.M_DrawHitBox);
                            mHitBoxSelected = true;
                            d.HOS.MouseLeftButtonDown(sender, e);
                            break;
                        }
                    }
                    break;
            }
        }

        public void MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            var msePos = e.GetPosition(mCanvas);
            //Console.WriteLine("Lup {0} {1}", (int)msePos.X, (int)msePos.Y);

            switch (mPenMode) {
                case PenModeEnum.PM_Pen:
                    CurDU.MouseLeftButtonUp(sender, e);
                    NewDU();
                    break;
                case PenModeEnum.PM_Select:
                    if (mHitBoxSelected) {
                        // 通常描画モードに戻します。
                        var d = mDOList[mSelectedDOidx];
                        d.HOS.MouseLeftButtonUp(sender, e);

                        // DrawObjをリサイズします。
                        d.Resize(d.HOS.XYWH);
                        //d.ModeChangeTo(DrawObj.Mode.M_Draw);
                        //mHitBoxSelected = false;
                    }

                    // 最後のオブジェクトを選択状態にします。
                    //mSelectedDOidx = mDOList.Count - 1;
                    break;
            }
        }

        public void MouseRightButtonUp(object sender, MouseButtonEventArgs e) {
            var msePos = e.GetPosition(mCanvas);
            Console.WriteLine("Rup {0} {1}", (int)msePos.X, (int)msePos.Y);

            switch (mPenMode) {
                case PenModeEnum.PM_Pen:
                    // ペンモードで右クリ：Undo。
                    Undo();
                    break;
                case PenModeEnum.PM_Select:
                    // 選択モードで右クリ：削除。
                    for (int i = mDOList.Count - 1; 0 <= i; --i) {
                        var d = mDOList[i];
                        if (d.GetHitArea().Contains(msePos)) {
                            // 当たったオブジェクト削除。
                            d.ModeChangeTo(DrawObj.Mode.M_Draw);
                            d.DeleteFromCanvas();
                            mDOList.RemoveAt(i);

                            // 最後のオブジェクトを選択状態にします。
                            mSelectedDOidx = mDOList.Count - 1;
                            break;
                        }
                    }
                    break;
            }
        }

        public bool CanUndo() {
            // 空ではないDOを数えます。
            int count = 0;
            for (int i=0; i<mDOList.Count; ++i) {
                var o = mDOList[i];
                if (!o.IsEmpty()) {
                    ++count;
                }
            }

            return count != 0;
        }

        public bool CanRedo() {
            return 0 < mDOUndoList.Count;
        }

        /// <summary>
        /// mDOListの最新dObjが空の時削除。
        /// </summary>
        private void ClearEmptyObj() {
            while (0 < mDOList.Count) { 
                var o = mDOList[mDOList.Count-1];
                if (o.IsEmpty()) {
                    o.DeleteFromCanvas();
                    mDOList.RemoveAt(mDOList.Count - 1);
                } else {
                    // 空ではないdObjが現れた。
                    return;
                }
            }
        }


        public enum ClearMode {
            CM_NewDU,
            CM_ZeroObj,
        }

        /// <summary>
        /// 中で保持しているDrawObjを全て消去、キャンバスからも消します。
        /// </summary>
        /// <param name="cm">CM_NewDUのとき、空のDrawObjを追加します。CM_ZeroObjの場合DrawObjの数が0となり、mSelectedDOidxが無効な位置を指します。</param>
        public void Clear(ClearMode cm) {
            Console.WriteLine("DOMgr.Clear({0})", cm);

            while (0 < mDOList.Count) {
                var o = mDOList[mDOList.Count - 1];
                o.DeleteFromCanvas();
                mDOList.RemoveAt(mDOList.Count - 1);
            }
            mDOUndoList.Clear();
            
            if (cm == ClearMode.CM_NewDU) { 
                NewDU();
            } else {
                mSelectedDOidx = -1;
            }
        }

        public bool Undo() {
            if (mDOList.Count < 2) {
                return false;
            }

            // 最新の空で無いobjを削除。
            ClearEmptyObj();
            if (0 < mDOList.Count) {
                DrawObj o = mDOList[mDOList.Count-1];
                mDOList.RemoveAt(mDOList.Count-1);
                o.DeleteFromCanvas();

                // oは空で無い描画物なので、Undoに追加。
                mDOUndoList.Add(o);
            } else {
                // 空のOBJしか無い。
                Console.WriteLine("Undo but there are only empty objects!");
            }

            NewDU();
            return true;
        }

        public bool Redo() {
            if (mDOUndoList.Count == 0) {
                return false;
            }

            var o = mDOUndoList[mDOUndoList.Count-1];
            mDOUndoList.RemoveAt(mDOUndoList.Count-1);

            ClearEmptyObj();

            mDOList.Add(o);
            o.AddToCanvas(mCanvas);

            NewDU();
            return true;
        }

        /*
        public bool Save(string path) {
            // 描画物のないdObjを削除。
            ClearEmptyObj();

            // ファイルに保存します。
            using (var bw = new BinaryWriter(File.Open(path, FileMode.Create))) {
                bw.Write(FILE_FOURCC);
                bw.Write(FILE_VERSION);
                bw.Write(mUniqueIdx);

                int doCount = mDOList.Count;
                bw.Write(doCount);

                foreach (var o in mDOList) {
                    o.Save(bw);
                }
            }

            NewDU();
            return true;
        }

        public bool Load(string path) {
            Clear(ClearMode.CM_ZeroObj);

            // ファイルから読み出します。
            using (var br = new BinaryReader(File.Open(path, FileMode.Open))) {
                int fourcc = br.ReadInt32();
                if (fourcc != FILE_FOURCC) {
                    Console.WriteLine("FourCC mismatch");
                    return false;
                }
                
                int version = br.ReadInt32();
                if (version != FILE_VERSION) {
                    Console.WriteLine("Version mismatch");
                    return false;
                }

                mUniqueIdx = br.ReadInt32();
                int doCount = br.ReadInt32();
                for (int i=0; i<doCount; ++i) {
                    var o = DrawObjNew.Load(br, mCanvas);
                    if (o == null) {
                        Console.WriteLine("E: DrawObjMgr Load failed");
                        return false;
                    }
                    mDOList.Add(o);
                }
            }

            NewDU();
            return true;
        }
        */
    }
}
