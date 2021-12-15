using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace PenTabletNotebook {
    class DrawObjFreePen : DrawObj {
        Brush mBrush;
        System.Windows.Shapes.Path mPath = new System.Windows.Shapes.Path();
        GeometryGroup mGeomGroup = new GeometryGroup();
        PathGeometry mPathGeom = new PathGeometry();
        PathFigure mPathFig = new PathFigure();
        PolyLineSegment mPolyLineSegment = new PolyLineSegment();

        private double LineStrokeThinkness = 2;
        private int mIdx;

        public override void SetBrush(Brush b) {
            mBrush = b;
            mPath.Stroke = mBrush;
        }

        /// <summary>
        /// 描画物が無い時true
        /// </summary>
        public override bool IsEmpty() {
            return 0 == mPolyLineSegment.Points.Count;
        }

        public override Rect GetArea() {
            Point xy = new Point(double.MaxValue, double.MaxValue);
            Vector wh = new Vector(0, 0);

            foreach (var c in mPolyLineSegment.Points) {
                // 左上座標xyを決定します。
                if (c.X < xy.X) {
                    xy.X = c.X;
                }
                if (c.Y < xy.Y) {
                    xy.Y = c.Y;
                }
            }

            foreach (var c in mPolyLineSegment.Points) {
                // 幅高さwhを決定します。
                Vector whC = c - xy;
                if (wh.X < whC.X) {
                    wh.X = whC.X;
                }
                if (wh.Y < whC.Y) {
                    wh.Y = whC.Y;
                }
            }

            return new Rect(xy, wh);
        }

        public override void Resize(Rect rAfter) {
            // 現在のサイズ。
            Rect rBefore = GetArea();

            // 平行移動量。
            Vector dXY = rAfter.TopLeft - rBefore.TopLeft;

            // スケール。小さくなりすぎないようにします。
            double minSz = 2.0;
            double sX = (rAfter.Width < minSz ? minSz : rAfter.Width) / rBefore.Width;
            double sY = (rAfter.Height < minSz ? minSz : rAfter.Height) / rBefore.Height;
            var scale = new Vector(sX, sY);

            // Console.WriteLine("Resize {0} {1} {2} {3}", dXY.X, dXY.Y, sX, sY);

            for (int i=0; i<mPolyLineSegment.Points.Count; ++i) {
                var p = mPolyLineSegment.Points[i];
                var pNew = new Point(
                    dXY.X + rBefore.X + (p.X - rBefore.X) * scale.X,
                    dXY.Y + rBefore.Y + (p.Y - rBefore.Y) * scale.Y);
                mPolyLineSegment.Points[i] = pNew;
                if (i == 0) {
                    // 始点セット。
                    mPathFig.StartPoint = pNew;
                }
            }
        }

        public override void DeleteFromCanvas() {
            if (mCanvas != null) {
                int i = 0;
                foreach (var c in mCanvas.Children) {
                    var p = c as System.Windows.Shapes.Path;
                    if (p != null && (int)p.Tag == mIdx) {
                        mCanvas.Children.RemoveAt(i);
                        Console.WriteLine("DrawObj {0} removed from Canvas", i);
                        break;
                    }
                    ++i;
                }
            } else {
                Console.WriteLine("DeleteFromCanvas() not added to canvas");
            }
        }

        public override bool AddToCanvas(Canvas c) {
            if (c != null) {
                mCanvas = c;
                c.Children.Add(mPath);

                Console.WriteLine("DrawObj {0} added to Canvas", c.Children.Count-1);
                return true;
            }

            return false;
        }

        public DrawObjFreePen(int idx, Canvas c, Brush b) {
            mIdx = idx;
            mCanvas = c;
            mBrush = b;

            mPolyLineSegment.IsSmoothJoin = true;

            mPathFig.Segments.Add(mPolyLineSegment);
            mPathGeom.Figures.Add(mPathFig);
            mGeomGroup.Children.Add(mPathGeom);

            mPath.Tag = idx;
            mPath.Data = mGeomGroup;
            mPath.Stroke = mBrush;
            mPath.StrokeThickness = LineStrokeThinkness;

            AddToCanvas(c);
        }

        public override void MouseLeftButtonDown(object sender, MouseButtonEventArgs e) {

            var msePos = e.GetPosition(mCanvas);

            Console.WriteLine("MLDn {0}, {1}", msePos.X, msePos.Y);

            mPathFig.StartPoint = msePos;
        }

        public override void MouseMove(object sender, MouseEventArgs e) {
            if (e.LeftButton != MouseButtonState.Pressed) {
                return;
            }

            // たまに、LeftButtonDownが1度も来ないままMouseMoveが来る。
            if (mPathFig.StartPoint.X == 0 &&
                mPathFig.StartPoint.Y == 0) {
                Console.WriteLine("Move L without Down!");
                return;
            }

            var msePos = e.GetPosition(mCanvas);

            if (1 <= mPolyLineSegment.Points.Count) { 
                var lastPos = mPolyLineSegment.Points[mPolyLineSegment.Points.Count - 1];
                var lastToCur = msePos - lastPos;
                //Console.WriteLine("MM L {0}, {1}, len={2}", msePos.X, msePos.Y, lastToCur.Length);

                if (lastToCur.Length < 1.3) {
                    // 最後の点が近い場合追加しない。
                    return;
                }
            }

            mPolyLineSegment.Points.Add(msePos);
        }
        public override void MouseLeftButtonUp(object sender, MouseButtonEventArgs e) {
            if (mPolyLineSegment.Points.Count == 0) {
                // マウス操作でメニューからファイルをロードした場合、いきなりLButtonUpイベントが来ます。
                Console.WriteLine("Mouse Lup pass");
                return;
            }

            var msePos = e.GetPosition(mCanvas);
            /*
            var lastPos = mPolyLineSegment.Points[mPolyLineSegment.Points.Count - 1];
            var d = lastPos - msePos;
            if (d.Length < 2.0) { 
                // 最後の点をすこしずらします。
                msePos += new Vector(2, 2);
            }
            */

            Console.WriteLine("MLUp {0}, {1}", msePos.X, msePos.Y);

            mPolyLineSegment.Points.Add(msePos);
        }

        public override void Save(System.IO.BinaryWriter bw) {
            var scb = mBrush as SolidColorBrush;

            int doe = (int)DrawObjEnum.DOE_FreePen;
            bw.Write(doe);
            bw.Write(mIdx);
            bw.Write(scb.Color.R);
            bw.Write(scb.Color.G);
            bw.Write(scb.Color.B);

            int nPoints = mPolyLineSegment.Points.Count;
            bw.Write(nPoints);

            for (int i=0; i<nPoints; ++i) {
                bw.Write(mPolyLineSegment.Points[i].X);
                bw.Write(mPolyLineSegment.Points[i].Y);
            }
        }

        public override bool Load(System.IO.BinaryReader br) {
            // この関数はDrawObjNewから呼び出されます。
            // DOE, mIdx, scbはDrawObjNewが読みます。
            // nPoints以降を読みます。

            int nPoints = br.ReadInt32();
            if (0 == nPoints) {
                return true;
            }

            double x = br.ReadDouble();
            double y = br.ReadDouble();
            var p0 = new Point(x, y);

            mPathFig.StartPoint = p0;
            mPolyLineSegment.Points.Add(p0);

            // p1以降。
            for (int i=1; i<nPoints; ++i) {
                x = br.ReadDouble();
                y = br.ReadDouble();
                p0 = new Point(x, y);
                mPolyLineSegment.Points.Add(p0);
            }

            return true;
        }

    }
}
