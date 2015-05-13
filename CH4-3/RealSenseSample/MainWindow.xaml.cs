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

        const int IR_WIDTH = 640;
        const int IR_HEIGHT = 480;
        const int IR_FPS = 30;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded( object sender, RoutedEventArgs e )
        {
            try {
                Initialize();

                CompositionTarget.Rendering += CompositionTarget_Rendering;
            }
            catch ( Exception ex ) {
                MessageBox.Show( ex.Message );
                Close();
            }
        }

        void CompositionTarget_Rendering( object sender, EventArgs e )
        {
            try {
                UpdateFrame();
            }
            catch ( Exception ex ) {
                MessageBox.Show( ex.Message );
                Close();
            }
        }

        void UpdateFrame()
        {
            // フレームを取得する
            pxcmStatus ret =  senseManager.AcquireFrame( true );
            if ( ret < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                return;
            }

            // フレームデータを取得する
            PXCMCapture.Sample sample = senseManager.QuerySample();
            if ( sample != null ) {
                // 各データを表示する
                UpdateIrImage( sample.ir );
            }

            // フレームを解放する
            senseManager.ReleaseFrame();
        }

        // IR画像を更新する
        private void UpdateIrImage( PXCMImage irFrame )
        {
            if ( irFrame == null ) {
                return;
            }

            // データを取得する
            PXCMImage.ImageData data;
            pxcmStatus ret =  irFrame.AcquireAccess( 
                PXCMImage.Access.ACCESS_READ,
                PXCMImage.PixelFormat.PIXEL_FORMAT_Y8, out data );
            if ( ret < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                throw new Exception( "IR画像の取得に失敗" );
            }

            // Bitmapに変換する
            var info = irFrame.QueryInfo();
            var length = data.pitches[0] * info.height;

            var buffer = data.ToByteArray( 0, length );
            ImageIr.Source = BitmapSource.Create( info.width, info.height, 96, 96,
                PixelFormats.Gray8, null, buffer, data.pitches[0] );

            // データを解放する
            irFrame.ReleaseAccess( data );
        }

        private void Window_Unloaded( object sender, RoutedEventArgs e )
        {
            Uninitialize();
        }

        private void Initialize()
        {
            // SenseManagerを生成する
            senseManager = PXCMSenseManager.CreateInstance();

            // IRストリームを有効にする
            pxcmStatus sts = senseManager.EnableStream( 
                PXCMCapture.StreamType.STREAM_TYPE_IR, 
                IR_WIDTH, IR_HEIGHT, IR_FPS );
            if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                throw new Exception( "IRストリームの有効化に失敗しました" );
            }

            // パイプラインを初期化する
            sts =  senseManager.Init();
            if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                throw new Exception( "初期化に失敗しました" );
            }

            // ミラー表示にする
            senseManager.QueryCaptureManager().QueryDevice().SetMirrorMode(
                PXCMCapture.Device.MirrorMode.MIRROR_MODE_HORIZONTAL );
        }

        private void Uninitialize()
        {
            if ( senseManager != null ) {
                senseManager.Dispose();
                senseManager = null;
            }
        }
    }
}
