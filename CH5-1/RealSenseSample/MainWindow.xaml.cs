using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

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
            var config = handAnalyzer.CreateActiveConfiguration();
            config.EnableSegmentationImage( true );

            config.ApplyChanges();
            config.Update();
        }

        // 手のデータを更新する
        private void UpdateHandFrame()
        {
            handData.Update();

            // 画像の初期化
            ImageHand1.Source = ImageHand2.Source = null;

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

                // 手の画像を取得する
                PXCMImage image;
                sts = hand.QuerySegmentationImage( out image );
                if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                    continue;
                }

                // マスク画像を取得する
                PXCMImage.ImageData data;
                sts = image.AcquireAccess(  PXCMImage.Access.ACCESS_READ,
                    PXCMImage.PixelFormat.PIXEL_FORMAT_Y8, out data );
                if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                    continue;
                }

                // マスク画像のサイズはDepthに依存
                // 手は2つまで
                var info = image.QueryInfo();
                var bitmap = BitmapSource.Create( info.width, info.height, 96, 96,
                    PixelFormats.Gray8, null, data.planes[0],
                    data.pitches[0] * info.height, data.pitches[0] );
                if ( i == 0 ) {
                    ImageHand1.Source = bitmap;
                }
                else {
                    ImageHand2.Source = bitmap;
                }

                image.ReleaseAccess( data );
            }
        }

        private void Uninitialize()
        {
            // リソースを解放する
            if ( senseManager != null ) {
                senseManager.Dispose();
                senseManager = null;
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
    }
}
