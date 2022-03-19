using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Ink;

namespace PenTabletNotebook {
    class DOPage {
        private string mImgFilename = "";

        public string ImgFilename {
            get { return mImgFilename; }
            set { mImgFilename = value; }
        }

        private string mSaveDir;
        private string mLoadDir;

        /// <summary>
        /// DrawObjの情報をシリアライズし保持。
        /// </summary>
        private System.IO.MemoryStream mMStream;
        private long mStreamBytes = 0;

        /// <summary>
        /// mStreamのバージョン。
        /// </summary>
        private int mStreamFileVersion;

        private StrokeCollection mStrokeCollection = new StrokeCollection();

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
                Serialize(mStrokeCollection);
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

        public void Load(int fileVersion, System.IO.BinaryReader br, string saveDir, string loadDir) {
            mSaveDir = saveDir;
            mLoadDir = loadDir;

            // brから読んでメモリ上にバッファbを作成。
            int bytes = br.ReadInt32();
            var b = br.ReadBytes(bytes);

            // mStreamにバッファbを書き込みます。
            mMStream = new System.IO.MemoryStream();
            mBW = new System.IO.BinaryWriter(mMStream);
            mBW.Write(b);

            // バッファbのメモリストリームから内容を読み出し。
            mBR = new System.IO.BinaryReader(mMStream);
            mBW.Seek(0, System.IO.SeekOrigin.Begin);
            Deserialize(fileVersion, null);
        }


        /// <summary>
        /// ページ情報を内部のMemoryStreamに貯めこみます。
        /// </summary>
        public bool Serialize(StrokeCollection strokeCollection) {
            // 4       FOURCC "PAGE"
            // 4       画像ファイル名バイト数fnBytes
            // fnBytes 画像ファイル名
            // 4       DrawObjの数dCount
            // dCount個のDrawObj
            // 4       inkCanvasの情報バイト数。
            // inkCanvasの情報。

            mBW.Write(FOURCC);

            // mImgFilename文字列を保存します。
            SaveLoad.SerializeString(mImgFilename, mBW);

            // doList:廃止。
            int dCount = 0;
            mBW.Write(dCount);

            // inkCanvasの情報書き込み。
            using (var icMS = new System.IO.MemoryStream()) {
                strokeCollection.Save(icMS);

                int icBytes = (int)icMS.Length;
                var icData = icMS.ToArray();

                mBW.Write(icBytes);
                mBW.Write(icData);
            }

            // Streamの有効データバイト数。
            mStreamBytes = mBW.BaseStream.Position;

            mBW.Seek(0, System.IO.SeekOrigin.Begin);

            // ストリームをクローズするとmMemStreamが消えるのでクローズしない。

            // このフォーマットのバージョンを保持。
            mStreamFileVersion = SaveLoad.FILE_VERSION;

            return true;
        }

        /// <summary>
        /// 内部に蓄えられたMemoryStreamからDrawObjとImageFilenameを実体化します。
        /// </summary>
        /// <param name="inkCanvas">キャンバスに登録しない場合nullを指定します。</param>
        public IEnumerable<DrawObj> Deserialize(int fileVersion, InkCanvas inkCanvas) {
            if (0 <= fileVersion) {
                // ストリームファイルバージョンを更新。
                mStreamFileVersion = fileVersion;
            } else {
                // 最後にストリームを書き込んだ時のバージョン番号を使用。
                fileVersion = mStreamFileVersion;
            }

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
                if (mImgFilename.StartsWith(mSaveDir)) {
                    // mImgFilenameがsaveDirを含む場合、loadDirに置き換えます。
                    mImgFilename = mImgFilename.Replace(mSaveDir, mLoadDir);

                    Console.WriteLine("img {0}", mImgFilename);
                }

                int dCount = mBR.ReadInt32();
                for (int i=0; i<dCount; ++i) {
                    var d = DrawObjNew.Load(mBR);
                    r.Add(d);
                }

                if (6 <= fileVersion) {
                    // inkCanvasの情報読み出し。
                    int icBytes = mBR.ReadInt32();
                    var icData = mBR.ReadBytes(icBytes);

                    using (var icMS = new System.IO.MemoryStream(icData)) {
                        mStrokeCollection = new StrokeCollection(icMS);
                        if (inkCanvas != null) {
                            inkCanvas.Strokes = mStrokeCollection;
                        }
                    }
                } else {
                    // inkCanvasの情報なし。
                    mStrokeCollection = new StrokeCollection();
                }

                mBR.BaseStream.Seek(0, System.IO.SeekOrigin.Begin);
            }

            return r;
        }
    }
}
