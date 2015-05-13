using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;//ADD
using System.Windows.Controls;//ADD
using System.Windows.Input;//ADD

namespace RealSenseSample
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        PXCMSenseManager senceManager;
        PXCMFaceData faceData;
        Rectangle[] rect;       //描画用の長方形を用意する
        TextBlock[] faceID_tb;         //顔の識別用IDを表示するTextBlockを用意する
        PXCMFaceData.RecognitionData rdata;
        const int DETECTION_MAXFACES = 2;    //顔を検出できる最大人数を設定
        const int RECOGNITION_MAXFACES = 2;    //特徴位置を検出できる最大人数を設定

        const int COLOR_WIDTH = 640;
        const int COLOR_HEIGHT = 480;
        const int COLOR_FPS = 30;

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
            pxcmStatus ret = senceManager.AcquireFrame(false);
            if (ret < pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                return;
            }

            //顔のデータを更新する
            updateFaceFrame();

            // フレームを解放する
            senceManager.ReleaseFrame();

        }

        //顔のフレームの更新処理
        private void updateFaceFrame()
        {
            // フレームデータを取得する
            PXCMCapture.Sample sample = senceManager.QuerySample();
            UpdateColorImage(sample.color);

            //SenceManagerモジュールの顔のデータを更新する
            faceData.Update();

            //検出した顔の数を取得する
            int numFaces = faceData.QueryNumberOfDetectedFaces();

            if (senceManager != null)
            {

                //それぞれの顔ごとに情報取得および描画処理を行う
                for (int i = 0; i < numFaces; ++i)
                {
                    //顔の情報を取得する
                    PXCMFaceData.Face face = faceData.QueryFaceByIndex(i);

                    // 顔の位置を取得:Depthで取得する
                    var detection = face.QueryDetection();
                    int face_x = 0;
                    int face_y = 0;

                    if (detection != null)
                    {
                        PXCMRectI32 faceRect;
                        detection.QueryBoundingRect(out faceRect);

                        //追加：顔の位置を格納するための変数を用意する
                        face_x = faceRect.x;
                        face_y = faceRect.y;

                        //顔の位置に合わせて長方形を変更
                        TranslateTransform transform = new TranslateTransform(faceRect.x, faceRect.y);
                        rect[i].Width = faceRect.w;
                        rect[i].Height = faceRect.h;
                        rect[i].Stroke = Brushes.Blue;
                        rect[i].StrokeThickness = 3;
                        rect[i].RenderTransform = transform;
                    }


                    //追加：顔識別の結果を格納するための変数を用意する
                    rdata = face.QueryRecognition();

                    if (rdata.IsRegistered()){
                        //追加：識別したIDかどうかを確認する
                        int uid = rdata.QueryUserID();
                        if (uid != -1) {
                            {
                                faceID_tb[i].Text = "Recognition:" + uid;
                                faceID_tb[i].RenderTransform = new TranslateTransform(face_x, face_y-30);
                            }
                        }else{
                            {
                                faceID_tb[i].Text = "Recognition:" + "NO";
                                faceID_tb[i].RenderTransform = new TranslateTransform(face_x, face_y-30);
                            }
                        }
                    }
                }
            }
        }

        //追加メソッド
        private void OnKeyDownHandler(object sender, KeyEventArgs e)
        {
            //追加：顔を登録する
            if (e.Key == Key.R)
            {
                int id = rdata.RegisterUser();
                noticeInfo.Text = "Registed:"+id.ToString();

            }

            //追加：顔の識別を解除する
            if (e.Key == Key.U)
            {
                rdata.UnregisterUser();
                noticeInfo.Text = "UnRegisted!";
            
            }
        }

        private void UpdateColorImage(PXCMImage colorFrame)
        {
            // データを取得する
            PXCMImage.ImageData data;
            pxcmStatus ret = colorFrame.AcquireAccess(PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_RGB24, out data);
            if (ret < pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                return;
            }

            // Bitmapに変換する
            var buffer = data.ToByteArray(0, COLOR_WIDTH * COLOR_HEIGHT * 3);
            ImageColor.Source = BitmapSource.Create(COLOR_WIDTH, COLOR_HEIGHT, 96, 96, PixelFormats.Bgr24, null, buffer, COLOR_WIDTH * 3);

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
                senceManager = PXCMSenseManager.CreateInstance();
                if (senceManager == null)
                {
                    throw new Exception("SenseManagerの生成に失敗しました");
                }

                // カラーストリームを有効にする
                pxcmStatus sts = senceManager.EnableStream(PXCMCapture.StreamType.STREAM_TYPE_COLOR, COLOR_WIDTH, COLOR_HEIGHT, COLOR_FPS);
                if (sts < pxcmStatus.PXCM_STATUS_NO_ERROR)
                {
                    throw new Exception("カラーストリームの取得に失敗しました");
                }

                InitializeFace();

                //描画用の長方形の初期化
                rect = new Rectangle[DETECTION_MAXFACES];
                for (int i = 0; i < DETECTION_MAXFACES; i++)
                {
                    rect[i] = new Rectangle();
                    TranslateTransform transform = new TranslateTransform(COLOR_WIDTH, COLOR_HEIGHT);
                    rect[i].Width = 10;
                    rect[i].Height = 10;
                    rect[i].Stroke = Brushes.Blue;
                    rect[i].StrokeThickness = 3;
                    rect[i].RenderTransform = transform;
                    CanvasForRect.Children.Add(rect[i]);
                }

                //追加：顔識別のIDの表示のための初期化
                faceID_tb = new TextBlock[RECOGNITION_MAXFACES];
                for (int i = 0; i < RECOGNITION_MAXFACES; i++)
                {
                    faceID_tb[i] = new TextBlock();
                    faceID_tb[i].Width = 200;
                    faceID_tb[i].Height = 24;
                    faceID_tb[i].Foreground = new SolidColorBrush(Colors.Red);
                    faceID_tb[i].FontSize = 24;
                    CanvasPoint.Children.Add(faceID_tb[i]);
                }

                
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.StackTrace);
                MessageBox.Show("Init:" + ex.Message);
                Close();
            }
        }

        private void InitializeFace() {
            // 顔検出を有効にする
            var sts = senceManager.EnableFace();
            if (sts < pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                throw new Exception("顔検出の有効化に失敗しました");
            }

            //顔検出器を生成する
            var faceModule = senceManager.QueryFace();

            //顔検出のプロパティを取得
            PXCMFaceConfiguration config = faceModule.CreateActiveConfiguration();
            config.SetTrackingMode(PXCMFaceConfiguration.TrackingModeType.FACE_MODE_COLOR_PLUS_DEPTH);

            config.ApplyChanges();

            //追加：顔識別のプロパティを取得
            PXCMFaceConfiguration.RecognitionConfiguration rcfg = config.QueryRecognition();

            //追加：顔識別を有効化
            rcfg.Enable();

            //追加：顔識別用データベースの用意
            PXCMFaceConfiguration.RecognitionConfiguration.RecognitionStorageDesc desc = new PXCMFaceConfiguration.RecognitionConfiguration.RecognitionStorageDesc();
            desc.maxUsers = 10;
            rcfg.CreateStorage("MyDB", out desc);
            rcfg.UseStorage("MyDB");

            //追加：顔識別の登録の設定を行う
            rcfg.SetRegistrationMode(PXCMFaceConfiguration.RecognitionConfiguration.RecognitionRegistrationMode.REGISTRATION_MODE_CONTINUOUS);

            // パイプラインを初期化する
            pxcmStatus ret = senceManager.Init();
            if (ret < pxcmStatus.PXCM_STATUS_NO_ERROR)
            {
                throw new Exception("初期化に失敗しました");
            }

            // デバイス情報の取得
            PXCMCapture.Device device = senceManager.QueryCaptureManager().QueryDevice();
            if (device == null)
            {
                throw new Exception("deviceの生成に失敗しました");
            }


            // ミラー表示にする
            device.SetMirrorMode(PXCMCapture.Device.MirrorMode.MIRROR_MODE_HORIZONTAL);

            PXCMCapture.DeviceInfo deviceInfo;
            device.QueryDeviceInfo(out deviceInfo);
            if (deviceInfo.model == PXCMCapture.DeviceModel.DEVICE_MODEL_IVCAM)
            {
                device.SetDepthConfidenceThreshold(1);
                device.SetIVCAMFilterOption(6);
                device.SetIVCAMMotionRangeTradeOff(21);
            }


            config.detection.isEnabled = true;
            config.detection.maxTrackedFaces = DETECTION_MAXFACES;
            config.QueryRecognition().Enable();
            config.ApplyChanges();

            faceData = faceModule.CreateOutput();
        
        }


        private void Uninitialize()
        {
            if (senceManager != null)
            {
                senceManager.Dispose();
                senceManager = null;
            }
        }
    }
}
