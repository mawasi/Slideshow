using Android.App;
using Android.Widget;
using Android.OS;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Timers;
using System.IO;

using Android.Graphics;

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

		Bitmap			mBitmap = null;




		protected override void OnCreate(Bundle bundle)
		{
			base.OnCreate(bundle);

			// Set our view from the "main" layout resource
			SetContentView (Resource.Layout.Main);

			mCurrentIndex = 0;

			// Downloadsディレクトリ取得
			mDownloads = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads);
#if false
			// Downloadsディレクトリ内のファイル名全取得
			mFileNameList = Directory.GetFiles(mDownloads.AbsolutePath).ToList();
#else
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
					Console.WriteLine(e);
				}
			}
#endif

			mSlideshowTimer = new Timer();
			mSlideshowTimer.Interval = 1000;//5000; // millisec
			mSlideshowTimer.AutoReset = true;	// 自動繰り返し
			mSlideshowTimer.Elapsed += OnSlideshowTimerEvent;

			mImageView = FindViewById<ImageView>(Resource.Id.imageView1);

			mButton = FindViewById<Button>(Resource.Id.button1);
			mButton.Click += OnClickEvent;

			mTextView = FindViewById<TextView>(Resource.Id.textView1);

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
			var Max = mFileNameList.Count;

			if(mCurrentIndex >= Max) {
				mCurrentIndex = 0;
			}

			var imagepath = mFileNameList[mCurrentIndex];
			if(System.IO.File.Exists(imagepath)) {
				if(mBitmap != null) {
					mBitmap.Recycle();
				}

				mBitmap = BitmapFactory.DecodeFile(imagepath);

				if(mBitmap != null){
					RunOnUiThread(() => {
						mTextView.Text = string.Format("{0}/{1} : {2}", mCurrentIndex, Max, imagepath);
						mImageView.SetImageBitmap(mBitmap);
					});
				}

			}

			mCurrentIndex++;
		}

		void StartSlideshow()
		{
			mButton.Text = GetString(Resource.String.Stop);
			mSlideshowTimer.Start();
		}

		void StopSlideshow()
		{
			mButton.Text = GetString(Resource.String.Start);
			mSlideshowTimer.Stop();
		}

	}

}

