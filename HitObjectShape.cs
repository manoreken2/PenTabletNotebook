using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PenTabletNotebook {
    public class HitObjectShape {
        private Canvas mCanvas;
        private Rect mXYWH;
        private Object mTag;

        private Rectangle mRectangle;
        private SolidColorBrush mFgColor = new SolidColorBrush(Colors.Black);
        private SolidColorBrush mSelectedColor = new SolidColorBrush(Colors.Red);

        public Rect XYWH {
            get { return mXYWH; }
        }

        enum ThumbId {
            T_TopLeft,
            T_TopRight,
            T_BottomLeft,
            T_BottomRight,
            
            T_Left,
            T_Right,
            T_Top,
            T_Bottom,

            T_NUM
        };

        private Ellipse [] mThumbs = new Ellipse[(int)ThumbId.T_NUM];
        enum HitObjectId {
            H_None = -1,

            H_ThumbTL,
            H_ThumbTR,
            H_ThumbBL,
            H_ThumbBR,
            H_ThumbLC,

            H_ThumbRC,
            H_ThumbTC,
            H_ThumbBC,
            H_Rect,

            H_NUM
        }

        private UIElement GetElem(HitObjectId hid) {
            switch (hid) {
                case HitObjectId.H_None:
                    return null;
                case HitObjectId.H_Rect:
                    return mRectangle;
                default:
                    return mThumbs[(int)hid];
            }
        }

        private const double mThumbSize = 12;
        private const double mThickness = 2;

        public static double ThumbRadius {
            get { return mThumbSize / 2; }
        }

        private void SetElemXY(UIElement e, double x, double y) {
            // キャンバスに左上座標をセット。
            Canvas.SetLeft(e, x);
            Canvas.SetTop(e,  y);
        }

        private void SetElemXY(UIElement e, Point xy) {
            SetElemXY(e, xy.X, xy.Y);
        }

        private Point ThumbCenterPos(ThumbId tid) {
            return new Point(
                Canvas.GetLeft(mThumbs[(int)tid]) + mThumbSize / 2,
                Canvas.GetTop(mThumbs[(int)tid])  + mThumbSize / 2);
        }

        /// <summary>
        /// Shapeの描画形状を更新します。
        /// </summary>
        private void XYWHUpdated() {
            var r = mXYWH;
            mRectangle.Width  = r.Width;
            mRectangle.Height = r.Height;
            SetElemXY(mRectangle, r.X, r.Y);

            // thumbの左上座標は、半径分だけ小さい値をセットします。
            SetElemXY(mThumbs[(int)ThumbId.T_TopLeft],     r.X - mThumbSize / 2,               r.Y - mThumbSize / 2);
            SetElemXY(mThumbs[(int)ThumbId.T_Top],         r.X + r.Width / 2 - mThumbSize / 2, r.Y - mThumbSize / 2);
            SetElemXY(mThumbs[(int)ThumbId.T_TopRight],    r.X + r.Width - mThumbSize / 2,     r.Y - mThumbSize / 2);
            SetElemXY(mThumbs[(int)ThumbId.T_Left],        r.X - mThumbSize / 2,               r.Y + r.Height / 2 - mThumbSize / 2);
            SetElemXY(mThumbs[(int)ThumbId.T_Right],       r.X + r.Width - mThumbSize / 2,     r.Y + r.Height / 2 - mThumbSize / 2);
            SetElemXY(mThumbs[(int)ThumbId.T_BottomLeft],  r.X - mThumbSize / 2,               r.Y + r.Height - mThumbSize / 2);
            SetElemXY(mThumbs[(int)ThumbId.T_Bottom],      r.X + r.Width / 2 - mThumbSize / 2, r.Y + r.Height - mThumbSize / 2);
            SetElemXY(mThumbs[(int)ThumbId.T_BottomRight], r.X + r.Width - mThumbSize / 2,     r.Y + r.Height - mThumbSize / 2);
        }

        IHitObjectMoved mMovedCb;


        /// <summary>
        /// 初期化。
        /// </summary>
        public HitObjectShape(Object tag, Canvas c, Rect r, IHitObjectMoved movedCb) {
            Console.WriteLine("HitObjectShape ctor");

            mTag = tag;
            mCanvas = c;
            mXYWH = r;
            mMovedCb = movedCb;

            mRectangle = new Rectangle();
            mRectangle.StrokeThickness = mThickness;
            mRectangle.Stroke = mFgColor;
            mCanvas.Children.Add(mRectangle);

            for (int i = 0; i < (int)ThumbId.T_NUM; ++i) {
                var e = new Ellipse();
                e.Stroke = mFgColor;
                e.Fill = mFgColor;
                e.StrokeThickness = mThickness;
                c.Children.Add(e);
                mThumbs[i] = e;

                e.Width  = mThumbSize;
                e.Height = mThumbSize;
            }

            XYWHUpdated();
        }

        public void HitObjectDelete() {
            CanvasUnset(mCanvas, mRectangle);
            for (int i = 0; i < (int)ThumbId.T_NUM; ++i) {
                CanvasUnset(mCanvas, mThumbs[i]);
            }
            mMovedCb = null;
        }

        private void CanvasUnset(Canvas c, UIElement e) {
            if (e != null) { 
                c.Children.Remove(e);
            }
        }

        private HitObjectId ProcHit(Point p) {
            for (int i = 0; i < (int)ThumbId.T_NUM; ++i) {
                var dXY = p - ThumbCenterPos((ThumbId)i);
                if (dXY.Length < mThumbSize / 2) {
                    // i番目のThumbにヒット。
                    return (HitObjectId)i;
                }
            }

            if (mXYWH.Contains(p)) {
                // mRectangleの中にヒット。
                return HitObjectId.H_Rect;
            }

            // 何にもヒットしない。
            return HitObjectId.H_None;
        }

        private void UpdateShapeColors(HitObjectId hid) {
            // 予めすべてのオブジェクトを非ヒット色に初期化。
            for (int i = 0; i < (int)ThumbId.T_NUM; ++i) {
                mThumbs[i].Stroke = mFgColor;
                mThumbs[i].Fill = mFgColor;
            }
            mRectangle.Stroke = mFgColor;

            // ヒットしたオブジェクトをヒット色にする。
            switch (hid) {
                case HitObjectId.H_None:
                    // 何もヒットしない。
                    break;
                case HitObjectId.H_Rect:
                    // Rectにヒット。
                    mRectangle.Stroke = mSelectedColor;
                    break;
                default:
                    // n番目のThumbにヒット。
                    mThumbs[(int)hid].Stroke = mSelectedColor;
                    mThumbs[(int)hid].Fill = mSelectedColor;
                    break;
            }
        }

        private void CallCb() {
            if (mMovedCb == null) {
                return;
            }

            mMovedCb.HitObjectMoved(mTag, this, mXYWH);
        }

        private void HitObjectMove(HitObjectId hid, Vector dXY) {
            Point xy = new Point(Canvas.GetLeft(mRectangle), Canvas.GetTop(mRectangle));
            Vector wh = new Vector(mRectangle.Width, mRectangle.Height);
            Vector dX = new Vector(dXY.X, 0);
            Vector dY = new Vector(0, dXY.Y);

            switch (hid) {
                case HitObjectId.H_None:
                    // 何もヒットしてない。
                    break;
                case HitObjectId.H_Rect:
                    // mRectangleを平行移動します。
                    mXYWH = new Rect(xy + dXY, wh);
                    XYWHUpdated();
                    CallCb();
                    break;
                case HitObjectId.H_ThumbTL:
                    // ↖のつまみがヒット。Rectの左上座標が移動し、Rectの右下座標が移動しない。
                    mXYWH = new Rect(xy + dXY, wh - dXY);
                    XYWHUpdated();
                    CallCb();
                    break;
                case HitObjectId.H_ThumbTC:
                    // ↑のつまみがヒット。Rect上端を上下に移動、下端は移動しない。
                    mXYWH = new Rect(xy + dY, wh - dY);
                    XYWHUpdated();
                    CallCb();
                    break;
                case HitObjectId.H_ThumbTR:
                    // ↗のつまみがヒット。Rect右上移動、左下は移動しない。
                    mXYWH = new Rect(xy + dY, wh + new Vector(dXY.X, -dXY.Y));
                    XYWHUpdated();
                    CallCb();
                    break;
                case HitObjectId.H_ThumbLC:
                    // ←のつまみがヒット。Rect左端移動、右端移動しない。
                    mXYWH = new Rect(xy + dX, wh - dX);
                    XYWHUpdated();
                    CallCb();
                    break;
                case HitObjectId.H_ThumbRC:
                    // →のつまみがヒット。Rect右端移動、左端移動しない。
                    mXYWH = new Rect(xy, wh + dX);
                    XYWHUpdated();
                    CallCb();
                    break;
                case HitObjectId.H_ThumbBL:
                    // ↙のつまみがヒット。Rect左下移動、右上移動しない。
                    mXYWH = new Rect(xy + new Vector(dXY.X, 0), wh + new Vector(-dXY.X, dXY.Y));
                    XYWHUpdated();
                    CallCb();
                    break;
                case HitObjectId.H_ThumbBC:
                    // ↓のつまみがヒット。Rect上端移動しない、下端移動。
                    mXYWH = new Rect(xy, wh + dY);
                    XYWHUpdated();
                    CallCb();
                    break;
                case HitObjectId.H_ThumbBR:
                    // ↘のつまみがヒット。Rect左上移動しない、右下移動。
                    mXYWH = new Rect(xy, wh + dXY);
                    XYWHUpdated();
                    CallCb();
                    break;
                default:
                    break;
            }
        }

        HitObjectId mPrevHitObjId = HitObjectId.H_None;
        Point mButtonPrevPos;

        public void MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {
            var msePos = e.GetPosition(mCanvas);
            var hid = ProcHit(msePos);
            UpdateShapeColors(hid);

            if (hid == HitObjectId.H_None) {
                // このオブジェクトはヒットしてない。
                Console.WriteLine("HOS L H_None");
                return;
            }

            // ヒットした。ヒット物と場所を記録。
            mPrevHitObjId = hid;
            mButtonPrevPos = msePos;
        }

        public void MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            mPrevHitObjId = HitObjectId.H_None;
        }

        public void MouseMove(object sender, MouseEventArgs e) {
            var msePos = e.GetPosition(mCanvas);
            if (e.LeftButton == MouseButtonState.Pressed) {
                // 左ボタンが押されている。
                if (mPrevHitObjId == HitObjectId.H_None) {
                    // ボタン押下時に何もヒットしてない。
                    Console.WriteLine("HOS MM prev H_None");
                    return;
                }

                // マウスドラッグ移動量。
                var dXY = msePos - mButtonPrevPos;

                // ヒット物をドラッグ移動量で移動します。
                HitObjectMove(mPrevHitObjId, dXY);
                mButtonPrevPos = msePos;
            } else {
                var hid = ProcHit(msePos);

                UpdateShapeColors(hid);
            }
        }

        /// <summary>
        /// ヒット形状を更新します。
        /// IHitObjectMovedイベントは発生しません。
        /// </summary>
        public void UpdateShape(Rect xywh) {
            mXYWH = xywh;
            XYWHUpdated();
        }
    }
}
