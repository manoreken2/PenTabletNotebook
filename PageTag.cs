using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PenTabletNotebook {
    class PageTag {
        private string mName;
        private int mPageNr;

        public string Name {
            get { return mName; }
        }

        public int PageNr {
            get { return mPageNr; }
        }

        public PageTag(string name, int pageNr) {
            mName = name;
            mPageNr = pageNr;
        }

        public bool IsTheSameAs(PageTag rhs) {
            if (mPageNr == rhs.mPageNr &&
                mName.Equals(rhs.mName)) {
                return true;
            }
            return false;
        }

        public override string ToString() {
            return string.Format("p{0}: {1}", mPageNr+1, mName);
        }

        public static int Compare(PageTag x, PageTag y) {
            if (x.PageNr != y.PageNr) {
                return x.PageNr - y.PageNr;
            }
            return x.Name.CompareTo(y.Name);
        }
    }


}
