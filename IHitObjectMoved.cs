using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace PenTabletNotebook {
    public interface IHitObjectMoved {

        void HitObjectMoved(Object tag, HitObjectShape hos, Rect xywh);
    }
}
