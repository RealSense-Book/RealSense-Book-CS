using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace SensorCoffee.Models
{
    public class TResult
    {
        public int Face { get; set; }
        public int Score { get; set; }
        public int Sentiment { get; set; }
        public int SScore { get; set; }
    }

    public class RSModel : INotifyPropertyChanged
    {
        private const int COLOR_WIDTH = 640;
        private const int COLOR_HEIGHT = 480;
        private const int COLOR_FPS = 30;
        private PXCMSenseManager SenseManager;
        private PXCMFaceData FaceData;
        private DispatcherTimer Timer = new DispatcherTimer();

        private static string[] EmotionLabels = { "ANGER", "CONTEMPT", "DISGUST", "FEAR", "JOY", "SADNESS", "SURPRISE" };
        private static string[] SentimentLabels = { "NEGATIVE", "POSITIVE", "NEUTRAL" };
        private static int NUM_PRIMARY_EMOTIONS = EmotionLabels.Length;
        private static int NUM_SENTIMENT_EMOTIONS = SentimentLabels.Length;

        private ImageSource _ColorImageElement;
        public ImageSource ColorImageElement
        {
            get { return this._ColorImageElement; }
            set
            {
                this._ColorImageElement = value;
                OnPropertyChanged();
            }
        }

        private string _Message;
        public string Message
        {
            get { return this._Message; }
            set
            {
                this._Message = value;
                OnPropertyChanged();
            }
        }

        private bool _IsResult = false;
        public bool IsResult
        {
            get { return this._IsResult; }
            set 
            {
                this._IsResult = value;
                OnPropertyChanged();
            }
        }

        private TResult _Result = null;
        public TResult Result
        {
            get { return this._Result; }
            set
            {
                this._Result = value;
                OnPropertyChanged();
                this.IsResult = (this._Result != null);
            }
        }

        public RSModel()
        {
            this.Timer.Interval = new TimeSpan(0, 0, 0, 0, 30);
            this.Timer.Tick += Timer_Tick;
        }

        public void RSStart()
        {
            try
            {
                this.SenseManager = PXCMSenseManager.CreateInstance();
                /* */
                if (InitializeFace() >= pxcmStatus.PXCM_STATUS_NO_ERROR)
                {
                    this.Timer.Start();
                }
            }
            catch (Exception ex)
            {
                this.Message = ex.Message;
            }
        }

        private pxcmStatus InitializeFace()
        {
            pxcmStatus result = pxcmStatus.PXCM_STATUS_NO_ERROR;
            // 顔検出を有効にする
            result = this.SenseManager.EnableFace();
            if (result < pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                this.Message = "Face Stream Enabled Error.";
            }
            else
            {
                // 追加：表情検出を有効にする
                result = this.SenseManager.EnableEmotion();
                if (result < pxcmStatus.PXCM_STATUS_NO_ERROR)
                {
                    this.Message = "Face Stream Enabled Error.";
                }
                else
                {
                    // パイプラインを初期化する
                    result = this.SenseManager.Init();
                    if (result < pxcmStatus.PXCM_STATUS_NO_ERROR)
                    {
                        this.Message = "Initialize Error.";
                    }
                    else
                    {
                        // ミラー表示にする
                        this.SenseManager.QueryCaptureManager().QueryDevice().SetMirrorMode(
                            PXCMCapture.Device.MirrorMode.MIRROR_MODE_HORIZONTAL);

                        //顔検出器を生成する
                        var faceModule = this.SenseManager.QueryFace();

                        // 顔検出器の設定
                        var device = this.SenseManager.QueryCaptureManager().QueryDevice();
                        PXCMCapture.DeviceInfo info = null;
                        device.QueryDeviceInfo(out info);
                        if (info.model == PXCMCapture.DeviceModel.DEVICE_MODEL_IVCAM)
                        {
                            device.SetDepthConfidenceThreshold(1);
                            device.SetIVCAMFilterOption(6);
                            device.SetIVCAMMotionRangeTradeOff(21);
                        }

                        //顔検出モードをカラー画像モードに設定
                        var config = faceModule.CreateActiveConfiguration();
                        config.SetTrackingMode(PXCMFaceConfiguration.TrackingModeType.FACE_MODE_COLOR);
                        config.detection.isEnabled = true;
                        config.ApplyChanges();
                        config.Update();
                        this.FaceData = faceModule.CreateOutput();
                    }
                }
            }
            return result;
        }

        public void RSStop()
        {
            this.Timer.Stop();
            this.SenseManager.Dispose();
            this.Result = null;
            this.ColorImageElement = null;
        }

        private void Timer_Tick(object sender, EventArgs e)
        {
            this.Timer.Stop();
            UpdateFrame();
            this.Timer.Start();
        }

        private void UpdateFrame()
        {
            // フレームを取得する
            if (this.SenseManager.AcquireFrame(false) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                // フレームデータを取得する
                var sample = this.SenseManager.QuerySample();
                if (sample != null)
                {
                    // 各データを表示する
                    UpdateColorImage(sample.color);
                }
                UpdateFaceFrame();

                // フレームを解放する
                this.SenseManager.ReleaseFrame();
            }
        }

        private void UpdateColorImage(PXCMImage colorFrame)
        {
            if (colorFrame != null)
            {
                PXCMImage.ImageData data = null;
                var ret = colorFrame.AcquireAccess(
                    PXCMImage.Access.ACCESS_READ,
                    PXCMImage.PixelFormat.PIXEL_FORMAT_RGB24,
                    out data);
                if (ret >= pxcmStatus.PXCM_STATUS_NO_ERROR)
                {
                    var info = colorFrame.QueryInfo();
                    var length = data.pitches[0] * info.height;
                    var buffer = data.ToByteArray(0, length);
                    this.ColorImageElement = BitmapSource.Create(
                        info.width,
                        info.height,
                        96,
                        96,
                        PixelFormats.Bgr24,
                        null,
                        buffer,
                        data.pitches[0]);
                    colorFrame.ReleaseAccess(data);
                }
            }
        }

        private void UpdateFaceFrame()
        {
            var emotionDet = this.SenseManager.QueryEmotion();
            if (emotionDet != null)
            {
                //SenceManagerモジュールの顔のデータを更新する
                this.FaceData.Update();

                //それぞれの顔ごとに情報取得および描画処理を行う
                for (int index = 0; index <= this.FaceData.QueryNumberOfDetectedFaces() - 1; index++)
                {
                    var face = this.FaceData.QueryFaceByIndex(index);
                    if (face != null)
                    {
                        //追加：ここからが表情(Emotion)認識
                        //追加：感情のデータを得る
                        PXCMEmotion.EmotionData[] datas;
                        emotionDet.QueryAllEmotionData(index, out datas);
                        //追加：表情(PRIMARY)を推定する
                        if (datas != null)
                        {
                            int primaryDataInxex = int.MinValue;
                            float maxscoreI = 0;
                            for (var emotionIndex = 0; emotionIndex <= NUM_PRIMARY_EMOTIONS - 1; emotionIndex++)
                            {
                                if (datas[emotionIndex].intensity > maxscoreI)
                                {
                                    maxscoreI = datas[emotionIndex].intensity;
                                    primaryDataInxex = emotionIndex;
                                }
                            }
                            if (primaryDataInxex >=0)
                            {
                                this.Result = new TResult
                                {
                                    Face = primaryDataInxex,
                                    Score = (int)Math.Truncate((maxscoreI * 100))
                                };
                            }
                        }

                        //表情の強さが取得できていたら感情値も設定する
                        if (Result != null)
                        {
                            //追加：感情(Sentiment)を推定する
                            //表情(PRIMARY)の推定と同様なので、コメントは省略
                            int primaryDataInxex = int.MinValue;
                            float maxscoreI = 0;
                            for (var sentimentIndex = NUM_PRIMARY_EMOTIONS; 
                                sentimentIndex <= NUM_PRIMARY_EMOTIONS + NUM_SENTIMENT_EMOTIONS - 1; 
                                sentimentIndex++)
                            {
                                if (datas[sentimentIndex].intensity > maxscoreI)
                                {
                                    maxscoreI = datas[sentimentIndex].intensity;
                                    primaryDataInxex = sentimentIndex - NUM_PRIMARY_EMOTIONS;
                                }
                            }
                            if (primaryDataInxex >= 0)
                            {
                                this.Result.Sentiment = primaryDataInxex;
                                this.Result.SScore = (int)Math.Truncate((maxscoreI * 100));
                            }
                        }
                    }
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            var eventHandler = this.PropertyChanged;
            if (eventHandler != null)
            {
                eventHandler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
