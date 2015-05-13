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
        PXCM3DSeg segmentation;

        // ピクセルデータバッファ
        byte[] imageBuffer = new byte[COLOR_WIDTH * COLOR_HEIGHT * BYTE_PER_PIXEL];

        // ビットマップ
        WriteableBitmap imageBitmap = new WriteableBitmap(
            COLOR_WIDTH, COLOR_HEIGHT, 96, 96, PixelFormats.Bgra32, null );

        // ビットマップの矩形
        Int32Rect imageRect = new Int32Rect( 0, 0, COLOR_WIDTH, COLOR_HEIGHT );

        // ピクセルあたりのバイト数
        const int BYTE_PER_PIXEL = 4;

        const int COLOR_WIDTH = 640;
        const int COLOR_HEIGHT = 480;
        const int COLOR_FPS = 30;

        //const int COLOR_WIDTH = 1920;
        //const int COLOR_HEIGHT = 1080;
        //const int COLOR_FPS = 30;

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Window_Loaded( object sender, RoutedEventArgs e )
        {
            Initialize();

            // ビットマップをImageに関連付ける
            ImageColor.Source = imageBitmap;

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

                // セグメンテーションデータを取得する
                var image = segmentation.AcquireSegmentedImage();
                UpdateSegmentationImage( image );

                // フレームを解放する
                senseManager.ReleaseFrame();
            }
            catch ( Exception ex ) {
                MessageBox.Show( ex.Message );
                Close();
            }
        }

        private void UpdateSegmentationImage( PXCMImage segmentationImage )
        {
            if ( segmentationImage == null ) {
                return;
            }

            // データを取得する
            PXCMImage.ImageData data;
            pxcmStatus ret =  segmentationImage.AcquireAccess( PXCMImage.Access.ACCESS_READ,
                PXCMImage.PixelFormat.PIXEL_FORMAT_RGB32, out data );
            if ( ret < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                return;
            }


            // ピクセルデータを初期化する
            Array.Clear( imageBuffer, 0, imageBuffer.Length );

            // セグメンテーション画像をバイト列に変換する
            var info = segmentationImage.QueryInfo();
            var buffer = data.ToByteArray( 0, data.pitches[0] * info.height );

            for ( int i = 0; i < (info.height * info.width); ++i ) {
                var index = i * BYTE_PER_PIXEL;

                // α値が0でない場合には有効な場所として色をコピーする
                if ( buffer[index + 3] != 0 ) {
                    imageBuffer[index + 0] = buffer[index + 0];
                    imageBuffer[index + 1] = buffer[index + 1];
                    imageBuffer[index + 2] = buffer[index + 2];
                    imageBuffer[index + 3] = 255;
                }
                // α値が0の場合は、ピクセルデータのα値を0にする
                else {
                    imageBuffer[index + 3] = 0;
                }
            }

            // ピクセルデータを更新する
            imageBitmap.WritePixels( imageRect, imageBuffer, data.pitches[0], 0 );

            // データを解放する
            segmentationImage.ReleaseAccess( data );
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

                // カラーストリームを有効にする
                var sts = senseManager.EnableStream( PXCMCapture.StreamType.STREAM_TYPE_COLOR,
                    COLOR_WIDTH, COLOR_HEIGHT, COLOR_FPS );
                if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                    throw new Exception( "カラーストリームの有効化にしました" );
                }

                // セグメンテーションを有効にする
                sts = senseManager.Enable3DSeg();
                if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                    throw new Exception( "セグメンテーションの有効化にしました" );
                }

                // パイプラインを初期化する
                sts =  senseManager.Init();
                if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                    throw new Exception( "初期化に失敗しました" );
                }

                // ミラー表示にする
                senseManager.QueryCaptureManager().QueryDevice().SetMirrorMode(
                    PXCMCapture.Device.MirrorMode.MIRROR_MODE_HORIZONTAL );

                // セグメンテーションオブジェクトを取得する
                segmentation = senseManager.Query3DSeg();
                if ( segmentation == null ) {
                    throw new Exception( "セグメンテーションの取得に失敗しました" );
                }
            }
            catch ( Exception ex ) {
                MessageBox.Show( ex.Message );
                Close();
            }
        }

        private void Uninitialize()
        {
            if ( senseManager != null ) {
                senseManager.Dispose();
                senseManager = null;
            }

            if ( segmentation != null ) {
                segmentation.Dispose();
                segmentation = null;
            }
        }
    }
}
