using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace PenTabletNotebook {
    class DOPage {
        private string mImgFilename;

        public string ImgFilename {
            get { return mImgFilename; }
            set { mImgFilename = value; }
        }

        /// <summary>
        /// DrawObjの情報をシリアライズし保持。
        /// </summary>
        private System.IO.MemoryStream mMStream;
        private long mStreamBytes = 0;

        System.IO.BinaryWriter mBW;
        System.IO.BinaryReader mBR;

        private int FOURCC = 0x45474150; //< "PAGE"

        public DOPage() {
            mMStream = new System.IO.MemoryStream();
            mBW = new System.IO.BinaryWriter(mMStream);
            mBR = new System.IO.BinaryReader(mMStream);
        }

        public bool Save(System.IO.BinaryWriter bw) {
            if (mStreamBytes == 0) {
                // ページが追加されたが一度も表示されなかった場合。
                Serialize(new List<DrawObj>());
            }

            var b = mMStream.ToArray();
            
            // 最後についているごみを除去。
            Array.Resize(ref b, (int)mStreamBytes);

            // バイト数を書き込み。
            int bytes = b.Length;
            bw.Write(bytes);

            bw.Write(b);
            return true;
        }

        public void Load(System.IO.BinaryReader br, string saveDir, string loadDir) {
            // brから読んで、mMStreamに保持します。
            int bytes = br.ReadInt32();
            var b = br.ReadBytes(bytes);

            mMStream = new System.IO.MemoryStream();
            mBW = new System.IO.BinaryWriter(mMStream);
            mBR = new System.IO.BinaryReader(mMStream);
            mBW.Write(b);
            mBW.Seek(0, System.IO.SeekOrigin.Begin);

            Deserialize(null);

            if (!saveDir.Equals(loadDir) && mImgFilename.StartsWith(saveDir)) {
                // mImgFilenameがsaveDirを含む場合、loadDirに置き換えます。
                mImgFilename = mImgFilename.Replace(saveDir, loadDir);
            }
        }


        /// <summary>
        /// ページ情報を内部のMemoryStreamに貯めこみます。
        /// </summary>
        public bool Serialize(IEnumerable<DrawObj> doList) {
            System.Diagnostics.Debug.Assert(doList != null);

            {
                // 4       FOURCC "PAGE"
                // 4       画像ファイル名バイト数fnBytes
                // fnBytes 画像ファイル名
                // 4       DrawObjの数dCount
                // dCount個のDrawObj

                mBW.Write(FOURCC);

                // mImgFilename文字列を保存します。
                SaveLoad.SerializeString(mImgFilename, mBW);

                // doListの保存。空で無いDOの数を数えます。
                int dCount = 0;
                foreach (var d in doList) {
                    if (!d.IsEmpty()) {
                        ++dCount;
                    }
                }

                mBW.Write(dCount);
                foreach (var d in doList) {
                    if (!d.IsEmpty()) {
                        d.Save(mBW);
                    }
                }

                // Streamの有効データバイト数。
                mStreamBytes = mBW.BaseStream.Position;

                mBW.Seek(0, System.IO.SeekOrigin.Begin);

                // ストリームをクローズするとmMemStreamが消えるのでクローズしない。
            }

            return true;
        }

        /// <summary>
        /// 内部に蓄えられたMemoryStreamからDrawObjとImageFilenameを実体化します。
        /// </summary>
        /// <param name="canvas">キャンバスに登録しない場合nullを指定します。</param>
        public IEnumerable<DrawObj> Deserialize(Canvas canvas) {
            var r = new List<DrawObj>();
            {
                mBR.BaseStream.Seek(0, System.IO.SeekOrigin.Begin);

                if (mBR.BaseStream.Length == 0) {
                    // 空である。
                    return r;
                }

                int fourcc = mBR.ReadInt32();
                if (fourcc != FOURCC) {
                    // 読み出しエラー。
                    throw new FormatException("FOURCC mismatch");
                }

                // mImgFilename文字列を読み出します。
                mImgFilename = SaveLoad.DeserializeString(mBR);

                int dCount = mBR.ReadInt32();
                for (int i=0; i<dCount; ++i) {
                    var d = DrawObjNew.Load(mBR, canvas);
                    r.Add(d);
                }

                mBR.BaseStream.Seek(0, System.IO.SeekOrigin.Begin);
            }

            return r;
        }
    }
}
