using System;
using System.Windows.Controls;
using System.Windows.Media;

namespace PenTabletNotebook {
    class DrawObjNew {
        public static int mNextIdx = 0;
        public static DrawObj Load(System.IO.BinaryReader br, Canvas canvas) {
            var doe = (DrawObj.DrawObjEnum)br.ReadInt32();
            switch (doe) {
                case DrawObj.DrawObjEnum.DOE_FreePen:
                    {
                        int idx = mNextIdx++;
                        
                        // idxが入っているが、不要。
                        br.ReadInt32();

                        byte r = br.ReadByte();
                        byte g = br.ReadByte();
                        byte b = br.ReadByte();
                        var brush = new SolidColorBrush(Color.FromRgb(r, g, b));
                        var d = new DrawObjFreePen(idx, canvas, brush);
                        bool rv = d.Load(br);
                        if (!rv) {
                            Console.WriteLine("E: DrawObjNew Load failed");
                            return null;
                        }
                        return d;
                    }
                default:
                    return null;
            }
        }
    }
}
