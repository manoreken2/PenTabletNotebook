using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PenTabletNotebook {
    class DrawObj {
        public Canvas mCanvas;
        public HitObjectShape mHOS;

        public HitObjectShape HOS {
            get { return mHOS; }
        }

        private static int mNextIID = 0;

        private int mIID = mNextIID++;
        public int IID {
            get { return mIID; }
        }

        public enum DrawObjEnum {
            DOE_None = -1,

            DOE_FreePen,

            DOE_NUM
        }

        public enum Mode {
            M_DrawHitBox,
            M_Draw,
        };

        private Mode mMode = Mode.M_Draw;
        public Mode CurrentMode {
            get { return mMode; }
        }

        public void ModeChangeTo(Mode m) {
            if (m == mMode) {
                return;
            }

            mMode = m;
            switch (m) {
                case Mode.M_DrawHitBox:
                    // リサイズ用のRectangleを表示。
                    System.Diagnostics.Debug.Assert(mHOS == null);
                    mHOS = new HitObjectShape(null, mCanvas, GetArea(), null);
                    break;
                case Mode.M_Draw:
                    // リサイズ用のRectangleを非表示。
                    if (mHOS != null) {
                        mHOS.HitObjectDelete();
                        mHOS = null;
                    }
                    break;
            }
        }

        public bool IsHit(Point xy) {
            return mHOS.XYWH.Contains(xy);
        }

        /// <summary>
        /// 描画物が無い時true
        /// </summary>
        public virtual bool IsEmpty() { return true; }

        public virtual void SetBrush(Brush b) {
        }
        public virtual void MouseMove(object sender, MouseEventArgs e) {
        }
        public virtual void MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
        }
        public virtual void MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
        }

        public virtual bool AddToCanvas(Canvas c) {
            return true;
        }

        public virtual void DeleteFromCanvas() {
        }

        /// <summary>
        /// 図形の占める範囲を算出。
        /// </summary>
        public virtual Rect GetArea() {
            return new Rect();
        }

        /// <summary>
        /// 図形の占める範囲＋つまみ。
        /// </summary>
        public virtual Rect GetHitArea() {
            var r = GetArea();
            return new Rect(
                r.X - HitObjectShape.ThumbRadius,
                r.Y - HitObjectShape.ThumbRadius,
                r.Width + 2.0 * HitObjectShape.ThumbRadius,
                r.Height + 2.0 * HitObjectShape.ThumbRadius);
        }

        public virtual void Resize(Rect xywh) {

        }

        public virtual void Save(BinaryWriter bw) {
            throw new NotImplementedException();
        }

        public virtual bool Load(BinaryReader br) {
            throw new NotImplementedException();
        }
    }
}
