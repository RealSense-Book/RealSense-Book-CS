using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace RealSenseSample
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        PXCMSenseManager senseManager;

        PXCMHandModule handAnalyzer;
        PXCMHandData handData;

        PXCMHandConfiguration handConfig;
        int rightGestureCount = 0;
        int leftGestureCount = 0;

        const int DEPTH_WIDTH = 640;
        const int DEPTH_HEIGHT = 480;
        const int DEPTH_FPS = 30;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded( object sender, RoutedEventArgs e )
        {
            Initialize();

            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        void CompositionTarget_Rendering( object sender, EventArgs e )
        {
            try {
                // フレームを取得する
                pxcmStatus ret =  senseManager.AcquireFrame( false );
                if ( ret < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                    return;
                }

                // フレームデータを取得する
                PXCMCapture.Sample sample = senseManager.QuerySample();
                if ( sample != null ) {
                    // 各データを表示する
                    UpdateDepthImage( sample.depth );
                }

                // 手のデータを更新する
                UpdateHandFrame();

                // フレームを解放する
                senseManager.ReleaseFrame();
            }
            catch ( Exception ex ) {
                MessageBox.Show( ex.Message );
                Close();
            }
        }


        private void Window_Unloaded( object sender, RoutedEventArgs e )
        {
            Uninitialize();
        }

        private void Initialize()
        {
            try {
                // SenseManagerを生成する
                senseManager = PXCMSenseManager.CreateInstance();

                // Depthストリームを有効にする
                var sts = senseManager.EnableStream( PXCMCapture.StreamType.STREAM_TYPE_DEPTH,
                    DEPTH_WIDTH, DEPTH_HEIGHT, DEPTH_FPS );
                if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                    throw new Exception( "Depthストリームの有効化に失敗しました" );
                }

                // 手の検出を有効にする
                sts = senseManager.EnableHand();
                if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                    throw new Exception( "手の検出の有効化に失敗しました" );
                }

                // パイプラインを初期化する
                sts = senseManager.Init();
                if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                    throw new Exception( "初期化に失敗しました" );
                }

                // ミラー表示にする
                senseManager.QueryCaptureManager().QueryDevice().SetMirrorMode(
                    PXCMCapture.Device.MirrorMode.MIRROR_MODE_HORIZONTAL );

                // 手の検出の初期化
                InitializeHandTracking();
            }
            catch ( Exception ex ) {
                MessageBox.Show( ex.Message );
                Close();
            }
        }

        // 手の検出の初期化
        private void InitializeHandTracking()
        {
            // 手の検出器を取得する
            handAnalyzer = senseManager.QueryHand();
            if ( handAnalyzer == null ) {
                throw new Exception( "手の検出器の取得に失敗しました" );
            }

            // 手のデータを作成する
            handData = handAnalyzer.CreateOutput();
            if ( handData == null ) {
                throw new Exception( "手の検出器の作成に失敗しました" );
            }

            // RealSense カメラであれば、プロパティを設定する
            var device = senseManager.QueryCaptureManager().QueryDevice();
            PXCMCapture.DeviceInfo dinfo;
            device.QueryDeviceInfo( out dinfo );
            if ( dinfo.model == PXCMCapture.DeviceModel.DEVICE_MODEL_IVCAM ) {
                device.SetDepthConfidenceThreshold( 1 );
                //device.SetMirrorMode( PXCMCapture.Device.MirrorMode.MIRROR_MODE_DISABLED );
                device.SetIVCAMFilterOption( 6 );
            }

            // 手の検出の設定
            handConfig = handAnalyzer.CreateActiveConfiguration();

            // 登録されているジェスチャーを列挙する
            var num = handConfig.QueryGesturesTotalNumber();
            for ( int i = 0; i < num; i++ ){
                string gestureName;
                var sts = handConfig.QueryGestureNameByIndex( i, out gestureName );
                if ( sts == pxcmStatus.PXCM_STATUS_NO_ERROR ){
                    ComboGesture.Items.Add( gestureName  );
                }
            }

            handConfig.ApplyChanges();
            handConfig.Update();
        }


        // Depth画像を更新する
        private void UpdateDepthImage( PXCMImage depthFrame )
        {
            if ( depthFrame == null ) {
                return;
            }

            // データを取得する
            PXCMImage.ImageData data;
            pxcmStatus ret =  depthFrame.AcquireAccess(
                PXCMImage.Access.ACCESS_READ,
                PXCMImage.PixelFormat.PIXEL_FORMAT_RGB32, out data );
            if ( ret < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                throw new Exception( "Depth画像の取得に失敗" );
            }

            // Bitmapに変換する
            var info = depthFrame.QueryInfo();
            var length = data.pitches[0] * info.height;

            var buffer = data.ToByteArray( 0, length );
            ImageHand.Source = BitmapSource.Create( info.width, info.height, 96, 96,
                PixelFormats.Bgr32, null, buffer, data.pitches[0] );

            // データを解放する
            depthFrame.ReleaseAccess( data );
        }

        // 手のデータを更新する
        private void UpdateHandFrame()
        {
            // 手のデータを更新する
            handData.Update();

            // データを初期化する
            CanvasHandParts.Children.Clear();

            // 検出した手の数を取得する
            var numOfHands = handData.QueryNumberOfHands();
            for ( int i = 0; i < numOfHands; i++ ) {
                // 手を取得する
                PXCMHandData.IHand hand;
                var sts = handData.QueryHandData(
                    PXCMHandData.AccessOrderType.ACCESS_ORDER_BY_ID, i, out hand );
                if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                    continue;
                }

                // 指の関節を列挙する
                for ( int j = 0; j < PXCMHandData.NUMBER_OF_JOINTS; j++ ) {
                    PXCMHandData.JointData jointData;
                    sts = hand.QueryTrackedJoint( (PXCMHandData.JointType)j, out jointData );
                    if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                        continue;
                    }

                    AddEllipse( CanvasHandParts,
                        new Point( jointData.positionImage.x, jointData.positionImage.y ),
                        5, Brushes.Green );
                }
            }

            // 認識したジェスチャーの数を取得する
            var numOfGestures = handData.QueryFiredGesturesNumber();
            for ( int i = 0; i < numOfGestures; i++ ) {
                // 認識したジェスチャーを取得する
                PXCMHandData.GestureData gesture;
                var sts = handData.QueryFiredGestureData( i, out gesture );
                if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                    continue;
                }

                // ジェスチャーをした手を取得する
                PXCMHandData.IHand hand;
                sts = handData.QueryHandDataById( gesture.handId, out hand );
                if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                    continue;
                }

                // どちらの手でジェスチャーしたのか
                var side = hand.QueryBodySide();
                if ( side == PXCMHandData.BodySideType.BODY_SIDE_LEFT ){
                    ++leftGestureCount;
                }
                else {
                    ++rightGestureCount;
                }
            }

            TextLeftGesture.Text = string.Format( "Left gesture : {0}", leftGestureCount );
            TextRightGesture.Text = string.Format( "Right gesture : {0}", rightGestureCount );
        }

        // 円を表示する
        void AddEllipse( Canvas canvas, Point point, int radius, Brush color,
            int thickness = 1 )
        {
            var ellipse = new Ellipse()
            {
                Width = radius,
                Height = radius,
            };

            if ( thickness <= 0 ) {
                ellipse.Fill = color;
            }
            else {
                ellipse.Stroke = color;
                ellipse.StrokeThickness = thickness;
            }

            Canvas.SetLeft( ellipse, point.X );
            Canvas.SetTop( ellipse, point.Y );
            canvas.Children.Add( ellipse );
        }

        private void Uninitialize()
        {
            if ( senseManager != null ) {
                senseManager.Dispose();
                senseManager = null;
            }

            if ( handConfig != null ) {
                handConfig.Dispose();
                handConfig = null;
            }

            if ( handData != null ) {
                handData.Dispose();
                handData = null;
            }

            if ( handAnalyzer != null ) {
                handAnalyzer.Dispose();
                handAnalyzer = null;
            }
        }

        // ジェスチャーが選択されたときに呼ばれるイベント
        private void ComboGesture_SelectionChanged( object sender,
            SelectionChangedEventArgs e )
        {
            var index = ComboGesture.SelectedIndex;
            if ( index == -1 ) {
                return;
            }

            // 選択されたインデックスのジェスチャー名を取得する
            string gestureName;
            var sts = handConfig.QueryGestureNameByIndex( index, out gestureName );
            if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                return;
            }

            // 一度すべてのジェスチャーを切り、選択されたジェスチャーを有効にする
            handConfig.DisableAllGestures();
            handConfig.EnableGesture( gestureName, true );

            handConfig.ApplyChanges();

            // ジェスチャーの検出数を初期化する
            leftGestureCount = rightGestureCount = 0;
        }
    }
}
