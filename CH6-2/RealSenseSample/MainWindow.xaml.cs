using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;//ADD
using System.Windows.Controls;//ADD

namespace RealSenseSample
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {

        private PXCMSenseManager senseManager;
        private PXCMFaceData faceData;
        private PXCMEmotion emotionDet;
        private System.Timers.Timer Timer = new System.Timers.Timer(1000);
        
        private const int COLOR_WIDTH = 640;
        private const int COLOR_HEIGHT = 480;
        private const int COLOR_FPS = 30;
        

        private static string[] EmotionLabels = { "ANGER", "CONTEMPT", "DISGUST", "FEAR", "JOY", "SADNESS", "SURPRISE" };
        private static string[] SentimentLabels = { "NEGATIVE", "POSITIVE", "NEUTRAL" };
        private static int NUM_PRIMARY_EMOTIONS = EmotionLabels.Length;
        private static int NUM_SENTIMENT_EMOTIONS = SentimentLabels.Length;

        const int DETECTION_MAXFACES = 2;    //顔を検出できる最大人数を設定
        const int EXPRESSION_MAXFACES = 2;   //顔の表出情報を取得できる最大人数を設定
        const int EMOTION_MAXFACES = 2;   //追加：表情を取得できる最大人数を設定
        Rectangle[] rect;       //描画用の長方形を用意する
        TextBlock[,] expression_tb;         //表出情報の値を表示するTextBlockを用意する
        TextBlock[,] emotion_tb;         //表情の値を表示するTextBlockを用意する


        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Initialize();
            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            try
            {
                updateFrame();                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
                MessageBox.Show("CompositionTarget_Rendering:" + ex.Message);
                Close();
            }
        }

        //フレーム全体の更新処理
        private void updateFrame()
        {
            // フレームを取得する
            if (this.senseManager.AcquireFrame(true) >= pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                updateFaceFrame();

                // フレームを解放する
                this.senseManager.ReleaseFrame();
            }


        }

        //顔のフレームの更新処理
        private void updateFaceFrame()
        {
            // フレームデータを取得する
            PXCMCapture.Sample sample = senseManager.QuerySample();
            if (sample != null)
            {
                UpdateColorImage(sample.color);
            }
            
            this.emotionDet = this.senseManager.QueryEmotion();
            if (this.emotionDet != null)
            {
                //SenceManagerモジュールの顔のデータを更新する
                this.faceData.Update();

                //それぞれの顔ごとに情報取得および描画処理を行う
                for (int index = 0; index <= this.faceData.QueryNumberOfDetectedFaces() - 1; index++)
                {
                    var face = this.faceData.QueryFaceByIndex(index);
                    if (face != null)
                    {
                        // ここで、顔の位置を取得:Colorで取得する
                        var detection = face.QueryDetection();
                        if (detection != null)
                        {
                            PXCMRectI32 faceRect;
                            detection.QueryBoundingRect(out faceRect);

                            //顔の位置に合わせて長方形を変更
                            TranslateTransform transform = new TranslateTransform(faceRect.x, faceRect.y);
                            rect[index].Width = faceRect.w;
                            rect[index].Height = faceRect.h;
                            rect[index].Stroke = Brushes.Blue;
                            rect[index].StrokeThickness = 3;
                            rect[index].RenderTransform = transform;



                            //顔のデータか表出情報のデータの情報を得る
                            var expressionData = face.QueryExpressions();
                            if (expressionData != null)
                            {
                                PXCMFaceData.ExpressionsData.FaceExpressionResult expressionResult;
                                //顔の位置に合わせて姿勢情報を表示
                                expression_tb[index, 0].RenderTransform = new TranslateTransform(transform.X, transform.Y + faceRect.h + 15);
                                expression_tb[index, 1].RenderTransform = new TranslateTransform(transform.X, transform.Y + faceRect.h + 30);
                                expression_tb[index, 2].RenderTransform = new TranslateTransform(transform.X, transform.Y + faceRect.h + 45);

                                //口の開き具合
                                if (expressionData.QueryExpression(PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_MOUTH_OPEN, out expressionResult))
                                {
                                    expression_tb[index, 0].Text = "MOUTH_OPEN:" + expressionResult.intensity;
                                }

                                //舌の出し具合
                                if (expressionData.QueryExpression(PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_TONGUE_OUT, out expressionResult))
                                {
                                    expression_tb[index, 1].Text = "TONGUE_OUT:" + expressionResult.intensity;
                                }

                                //笑顔の度合
                                if (expressionData.QueryExpression(PXCMFaceData.ExpressionsData.FaceExpression.EXPRESSION_SMILE, out expressionResult))
                                {
                                    expression_tb[index, 2].Text = "SMILE:" + expressionResult.intensity;
                                }

                                //ここまでが表出情報検出の機能
                                //////////////////////////////////////

                                //////////////////////////////////////
                                //追加：ここからが表情(Emotion)認識
                                //追加：感情のデータを得る
                                PXCMEmotion.EmotionData[] datas = new PXCMEmotion.EmotionData[NUM_PRIMARY_EMOTIONS+NUM_SENTIMENT_EMOTIONS];
                                emotionDet.QueryAllEmotionData(index, out datas);

                                //追加：表情(PRIMARY)を推定する
                                int maxscoreE = -3;
                                float maxscoreI = 0;
                                int idx_outstanding_emotion = -1;		//最終的に決定される表情の値
                                
                                for (int emotionIndex = 0; emotionIndex <= NUM_PRIMARY_EMOTIONS-1; emotionIndex++)
                                {
                                    if (datas != null) {
                                        if (datas[emotionIndex].evidence >= maxscoreE
                                        && datas[emotionIndex].intensity >= maxscoreI)
                                        {
                                            //二つの値を、ともに最も大きい場合の値へ更新
                                            maxscoreE = datas[emotionIndex].evidence;//表情の形跡(evidence)を比較
                                            maxscoreI = datas[emotionIndex].intensity;//表情の強さ(intensity)を比較
                                            //primaryData = datas[emotionIndex];
                                            idx_outstanding_emotion = emotionIndex;
                                        }
                                    }
                                    
                                }

                                if (idx_outstanding_emotion != -1)
                                {
                                    emotion_tb[index, 0].RenderTransform = new TranslateTransform(faceRect.x, faceRect.y - 30);
                                    emotion_tb[index, 0].Text = "Emotion_PRIMARY:" + EmotionLabels[idx_outstanding_emotion];
                                }

                                //表情の強さ(intensity)がある値以上の時、感情があると判断
                                if (maxscoreI > 0.4)
                                {
                                    //追加：感情(Sentiment)を推定する
                                    //表情(PRIMARY)の推定と同様なので、コメントは省略
                                    //PXCMEmotion.EmotionData primarySent = null;
                                    int idx_sentiment_emotion = -1;
                                    int s_maxscoreE = -3;
                                    float s_maxscoreI = 0.0f;
                                    for (int sentimentIndex = 0; sentimentIndex < NUM_SENTIMENT_EMOTIONS; sentimentIndex++)
                                    {
                                        if (datas != null)
                                        {
                                            if (datas[sentimentIndex].evidence > s_maxscoreE && datas[sentimentIndex].intensity > s_maxscoreI)
                                            {
                                                s_maxscoreE = datas[NUM_PRIMARY_EMOTIONS + sentimentIndex].evidence;
                                                s_maxscoreI = datas[NUM_PRIMARY_EMOTIONS + sentimentIndex].intensity;
                                                //primarySent = datas[sentimentIndex];
                                                idx_sentiment_emotion = sentimentIndex;
                                            }
                                        }
                                    }
                                    if (idx_sentiment_emotion != -1)
                                    {
                                        emotion_tb[index, 1].RenderTransform = new TranslateTransform(faceRect.x, faceRect.y - 60);
                                        emotion_tb[index, 1].Text = "Emo_SENTIMENT:" + EmotionLabels[idx_sentiment_emotion];
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }



        private void UpdateColorImage(PXCMImage colorFrame)
        {
            // データを取得する
            PXCMImage.ImageData data;

            PXCMImage.ImageInfo info = colorFrame.QueryInfo();
            Width = info.width;
            Height = info.height;

            pxcmStatus ret = colorFrame.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_RGB24, out data);
            if (ret < pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                return;
            }

            // Bitmapに変換する
            var buffer = data.ToByteArray(0, info.width * info.height * 3);
            ImageColor.Source = BitmapSource.Create(info.width, info.height, 96, 96, PixelFormats.Bgr24, null, buffer, info.width * 3);

            // データを解放する
            colorFrame.ReleaseAccess(data);
        }

        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            Uninitialize();
        }

        private void Initialize()
        {
            try
            {

                // SenseManagerを生成する
                senseManager = PXCMSenseManager.CreateInstance();
                if (senseManager == null)
                {
                    throw new Exception("SenseManagerの生成に失敗しました");
                }

                //注意：表情検出を行う場合は、カラーストリームを有効化しない
                /*
                pxcmStatus sts = senseManager.EnableStream(PXCMCapture.StreamType.STREAM_TYPE_COLOR, COLOR_WIDTH, COLOR_HEIGHT, COLOR_FPS);
                if (sts < pxcmStatus.PXCM_STATUS_NO_ERROR)
                {
                    throw new Exception("カラーストリームの取得に失敗しました");
                }
                */

                //顔の初期化
                if (InitializeFace() >= pxcmStatus.PXCM_STATUS_NO_ERROR)
                {
                    this.Timer.Start();
                }

                //描画用の長方形の初期化
                rect = new Rectangle[DETECTION_MAXFACES];
                for (int i = 0; i < DETECTION_MAXFACES; i++)
                {
                    rect[i] = new Rectangle();
                    TranslateTransform transform = new TranslateTransform(0, 0);
                    rect[i].Width = 10;
                    rect[i].Height = 10;
                    rect[i].Stroke = Brushes.Blue;
                    rect[i].StrokeThickness = 3;
                    rect[i].RenderTransform = transform;
                    CanvasForRect.Children.Add(rect[i]);
                }

                //追加：姿勢表示のための初期化
                expression_tb = new TextBlock[EXPRESSION_MAXFACES,3];
                for (int i = 0; i < EXPRESSION_MAXFACES;i++)
                {
                    for (int j = 0; j < 3; j++) {
                        expression_tb[i, j] = new TextBlock();
                        expression_tb[i, j].Width = 200;
                        expression_tb[i, j].Height = 27;
                        expression_tb[i, j].Foreground = new SolidColorBrush(Colors.Red);
                        expression_tb[i, j].FontSize = 20;
                        CanvasPoint.Children.Add(expression_tb[i, j]);
                    }
                }

                //追加：表情表示のための初期化
                emotion_tb = new TextBlock[EMOTION_MAXFACES, 2];
                for (int i = 0; i < EMOTION_MAXFACES; i++)
                {
                    for (int j = 0; j < 2; j++)
                    {
                        emotion_tb[i, j] = new TextBlock();
                        emotion_tb[i, j].Width = 200;
                        emotion_tb[i, j].Height = 27;
                        emotion_tb[i, j].Foreground = new SolidColorBrush(Colors.Red);
                        emotion_tb[i, j].FontSize = 14;
                        CanvasPoint.Children.Add(emotion_tb[i, j]);
                    }
                }

                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
                MessageBox.Show("Init:" + ex.Message);
                Close();
            }
        }

        private pxcmStatus InitializeFace()
        {
            pxcmStatus result = pxcmStatus.PXCM_STATUS_NO_ERROR;
            // 顔検出を有効にする
            result = this.senseManager.EnableFace();
            if (result < pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                MessageBox.Show("Face Stream Enabled Error.");
            }
            else
            {
                // 追加：表情検出を有効にする
                result = this.senseManager.EnableEmotion();
                if (result < pxcmStatus.PXCM_STATUS_NO_ERROR)
                {
                    MessageBox.Show("Face Stream Enabled Error.");
                }
                else
                {
                    //顔検出器を生成する
                    var faceModule = this.senseManager.QueryFace();

                    //顔検出のプロパティを取得
                    var config = faceModule.CreateActiveConfiguration();
                    config.SetTrackingMode(PXCMFaceConfiguration.TrackingModeType.FACE_MODE_COLOR_PLUS_DEPTH);
                    config.ApplyChanges();

                    // パイプラインを初期化する
                    result = this.senseManager.Init();
                    if (result < pxcmStatus.PXCM_STATUS_NO_ERROR)
                    {
                         MessageBox.Show("Initialize Error.");
                    }
                    else
                    {
                        // ミラー表示にする
                        this.senseManager.QueryCaptureManager().QueryDevice().SetMirrorMode(
                            PXCMCapture.Device.MirrorMode.MIRROR_MODE_HORIZONTAL);

                        // 顔検出器の設定
                        var device = this.senseManager.QueryCaptureManager().QueryDevice();
                        PXCMCapture.DeviceInfo info = null;
                        device.QueryDeviceInfo(out info);
                        if (info.model == PXCMCapture.DeviceModel.DEVICE_MODEL_IVCAM)
                        {
                            device.SetDepthConfidenceThreshold(1);
                            device.SetIVCAMFilterOption(6);
                            device.SetIVCAMMotionRangeTradeOff(21);
                        }

                        config.detection.isEnabled = true;
                        //ここからは表出情報(Expression)の設定を参照してください
                        config.QueryExpressions().Enable();
                        config.QueryExpressions().EnableAllExpressions();
                        config.QueryExpressions().properties.maxTrackedFaces = EXPRESSION_MAXFACES;
                        config.ApplyChanges();
                        config.Update();
                        this.faceData = faceModule.CreateOutput();
                    }
                }
            }
            return result;
        }



        private void Uninitialize()
        {
            if (senseManager != null)
            {
                senseManager.Dispose();
                senseManager = null;
            }
        }
    }
}
