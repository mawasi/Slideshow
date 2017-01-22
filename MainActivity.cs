using Android.App;
using Android.Widget;
using Android.OS;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.IO;

using Android.Graphics;
using Android.Views;

namespace Slideshow
{
	[Activity(Label = "Slideshow",MainLauncher = true,Icon = "@drawable/Slideshow")]
	public class MainActivity:Activity
	{


		Java.IO.File	mDownloads = null;

		List<string>	mFileNameList = new List<string>();

		int				mCurrentIndex = 0;
		// 画像切り替え用タイマー
		Timer			mSlideshowTimer = null;
		// 画像表示用
		ImageView		mImageView = null;
		// ファイルパスとか表示用
		TextView		mTextView = null;
		// スライドショー開始、停止ボタン
		Button			mButton = null;

		const int		BufferNum = 10;
		Bitmap[]		mBitmaps = new Bitmap[BufferNum];
		int				mBufferIndex = 0;

		// インターバル時間設定用エディットテキスト
		EditText		mEditText = null;

		// ロック用オブジェクト
		object			mLock = new object();


		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);

			mCurrentIndex = 0;
			mBufferIndex = 0;

			for(int i = 0; i < mBitmaps.Length; i++) {
				mBitmaps[i] = null;
			}

			// Downloadsディレクトリ取得
			mDownloads = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads);

#if false
			// Downloadsディレクトリ内のファイル名全取得
			mFileNameList = Directory.GetFiles(mDownloads.AbsolutePath).ToList();
#else
			try {
			var filelist = Directory.GetFiles(mDownloads.AbsolutePath);
				foreach(var file in filelist) {
					BitmapFactory.Options options = new BitmapFactory.Options();
					options.InJustDecodeBounds = true;
					try {
						BitmapFactory.DecodeFile(file, options);
						if(options.OutMimeType != null) {
							// 情報が取得できたということは画像ファイルなのでリストに追加する
							mFileNameList.Add(file);
						}
					}
					catch (Exception e) {
						System.Diagnostics.Debug.WriteLine(e);
					}
				}
			}
			catch(Exception e) {
				System.Diagnostics.Debug.WriteLine(e);
				// このタイミングの例外発生は多分Directryがないので、その旨を通知してボタン無効化とかしてやるのが良さそう
			}
#endif


			mSlideshowTimer = new Timer();
//			mSlideshowTimer.Interval = 500;//5000; // millisec
			mSlideshowTimer.AutoReset = true;	// 自動繰り返し
			mSlideshowTimer.Elapsed += OnSlideshowTimerEvent;

			mImageView = FindViewById<ImageView>(Resource.Id.imageView1);

			mButton = FindViewById<Button>(Resource.Id.button1);
			mButton.Click += OnClickEvent;
			mButton.RequestFocus();

			mTextView = FindViewById<TextView>(Resource.Id.textView1);

			mEditText = FindViewById<EditText>(Resource.Id.editText1);

		}

		protected override void OnStop()
		{
			base.OnStop();

			// ここでDisposeはちがう
//			mSlideshowTimer.Dispose();
			StopSlideshow();
		}

		protected override void OnStart()
		{
			base.OnStart();
		}

		public override bool OnTouchEvent(MotionEvent e)
		{
			return base.OnTouchEvent(e);
		}

		/// <summary>
		/// ボタンクリックイベント
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void OnClickEvent(object sender, EventArgs e)
		{
			if(!mSlideshowTimer.Enabled) {
				StartSlideshow();
			}
			else {
				StopSlideshow();
			}
		}


		/// <summary>
		/// スライドショータイマーイベント
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		void OnSlideshowTimerEvent(object sender, EventArgs e)
		{
#if true
			lock(mLock) {

	//			DateTime start = DateTime.Now;

				int threadID = System.Threading.Thread.CurrentThread.ManagedThreadId;

				var Max = mFileNameList.Count;

				// データが無いならそのまま終了
				if(Max <= 0) return;

				if(mCurrentIndex >= Max) {
					mCurrentIndex = 0;
				}

				var index = mCurrentIndex;
				var imagepath = mFileNameList[index];

				// Bitmaps[]用のインデクス作成
				int workindex = mBufferIndex % BufferNum;
				int half = BufferNum/2;
				int releaseindex = (workindex > (half - 1)) ? (workindex - half) : (BufferNum - (half - workindex));

				if(mBitmaps[workindex] != null) {
					mBitmaps[workindex].Recycle();
					mBitmaps[workindex] = null;
				}

				if(GetBitmap(imagepath, out mBitmaps[workindex])){
					RunOnUiThread(() => {
						mTextView.Text = string.Format("{0}/{1}: release {2} : work {3} : {4}", index, Max, releaseindex, workindex, imagepath);
						mImageView.SetImageBitmap(mBitmaps[workindex]);
					});
				}


				if(mBitmaps[releaseindex] != null) {
					mBitmaps[releaseindex].Recycle();
					mBitmaps[releaseindex] = null;
				}


				mCurrentIndex++;
				mBufferIndex++;

		//		DateTime end = DateTime.Now;
		//		TimeSpan ts = end - start;
		//		System.Diagnostics.Debug.WriteLine("Elapsed Time = " + ts.TotalMilliseconds.ToString() + " ThreadID = " + threadID.ToString() + ".");
				
			}
#else
			// 処理を全部UIスレッドにぶん投げる
			RunOnUiThread(
				() => {
					var Max = mFileNameList.Count;

					// データが無いならそのまま終了
					if(Max <= 0) return;

					if(mCurrentIndex >= Max) {
						mCurrentIndex = 0;
					}

					var index = mCurrentIndex;
					var imagepath = mFileNameList[index];

					int workindex = index % BufferNum;
					int half = BufferNum/2;
					int releaseindex = (workindex > (half - 1)) ? (workindex - half) : (BufferNum - (half - workindex));


					if(mBitmaps[releaseindex] != null) {
						mImageView.SetImageDrawable(null);	// bitmapに複数バッファもたせてるなら特にこれは必要ない
						mBitmaps[releaseindex].Recycle();
						mBitmaps[releaseindex] = null;
					}

					if(GetBitmap(imagepath, out mBitmaps[workindex])){
						mTextView.Text = string.Format("{0}/{1}: release {2} : work {3} : {4}", index, Max, releaseindex, workindex, imagepath);
						mImageView.SetImageBitmap(mBitmaps[workindex]);
					}

					mCurrentIndex++;


				}
			);
#endif
		}


		void StartSlideshow()
		{
			double MilliSec = double.Parse(mEditText.Text);
			mSlideshowTimer.Interval = MilliSec * 1000.0;
			mButton.Text = GetString(Resource.String.Stop);
			mSlideshowTimer.Start();
		}

		void StopSlideshow()
		{
			mButton.Text = GetString(Resource.String.Start);
			mSlideshowTimer.Stop();
		}


		/// <summary>
		/// ファイルパスからbitmap取得
		/// </summary>
		/// <param name="path"></param>
		/// <param name="bitmap"></param>
		bool GetBitmap(string path, out Bitmap bitmap)
		{
			bool result = false;
			bitmap = null;

			if(System.IO.File.Exists(path)) {
				bitmap = BitmapFactory.DecodeFile(path);

				if(bitmap != null){
					result = true;
				}
			}

			return result;
		}



	}

}

