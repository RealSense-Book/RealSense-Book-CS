using System;
using System.Collections.Generic;
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

        PXCMBlobModule blobModule = null;
        PXCMBlobData blobData = null;

        // ピクセルデータバッファ
        byte[] imageBuffer = new byte[DEPTH_WIDTH * DEPTH_HEIGHT * BYTE_PER_PIXEL];

        // ビットマップ
        WriteableBitmap imageBitmap = new WriteableBitmap(
            DEPTH_WIDTH, DEPTH_HEIGHT, 96, 96, PixelFormats.Gray8, null );

        // ビットマップの矩形
        Int32Rect imageRect = new Int32Rect( 0, 0, DEPTH_WIDTH, DEPTH_HEIGHT );

        // ピクセルあたりのバイト数
        const int BYTE_PER_PIXEL = 1;

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

            // ビットマップをImageに関連付ける
            ImageHand.Source = imageBitmap;

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
                var sample = senseManager.QuerySample();
                if ( sample != null ) {
                    // 各データを表示する
                    UpdateBlobImage( sample.depth );
                }

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

                // Blobを有効にする
                var sts = senseManager.EnableBlob();
                if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                    throw new Exception( "Blobの有効化に失敗しました" );
                }

                // パイプラインを初期化する
                sts = senseManager.Init();
                if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                    throw new Exception( "初期化に失敗しました" );
                }

                // ミラー表示にする
                senseManager.QueryCaptureManager().QueryDevice().SetMirrorMode(
                    PXCMCapture.Device.MirrorMode.MIRROR_MODE_HORIZONTAL );

                // Blobを初期化する
                InitializeBlob();
            }
            catch ( Exception ex ) {
                MessageBox.Show( ex.Message );
                Close();
            }
        }

        private void InitializeBlob()
        {
            // Blobを取得する
            blobModule = senseManager.QueryBlob();
            blobData = blobModule.CreateOutput();

            var blobConfig = blobModule.CreateActiveConfiguration();

            blobConfig.SetContourSmoothing( 1.0f );
            blobConfig.SetSegmentationSmoothing( 1.0f );
            blobConfig.SetMaxBlobs( 4 );
            blobConfig.SetMaxDistance( 500.0f );
            blobConfig.EnableContourExtraction( true );
            blobConfig.EnableSegmentationImage( true );
            blobConfig.ApplyChanges();
        }


        private void UpdateBlobImage( PXCMImage depthFrame )
        {
            if ( depthFrame == null ){
                return;
            }

            // Blobを更新する
            var sts = blobData.Update();
            if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                return;
            }

            // Blobのための画像オブジェクトを作成する
            var depthInfo = depthFrame.QueryInfo();
            depthInfo.format = PXCMImage.PixelFormat.PIXEL_FORMAT_Y8;

            var session = senseManager.QuerySession();
            var blobImage = session.CreateImage( depthInfo );

            // 表示用画像を初期化する
            Array.Clear( imageBuffer, 0, imageBuffer.Length );
            CanvasHandParts.Children.Clear();

            // Blobを取得する
            int numOfBlobs = blobData.QueryNumberOfBlobs();
            for ( int i = 0; i < numOfBlobs; ++i ) {
                // Blobデータを取得する
                PXCMBlobData.IBlob blob;
                sts = blobData.QueryBlobByAccessOrder( i, 
                    PXCMBlobData.AccessOrderType.ACCESS_ORDER_NEAR_TO_FAR, out blob);
                if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                    continue;
                }

                sts = blob.QuerySegmentationImage( out blobImage );
                if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                    continue;
                }

                // Blob画像を取得する
                PXCMImage.ImageData data;
                sts = blobImage.AcquireAccess( PXCMImage.Access.ACCESS_READ,
                    depthInfo.format, out data );
                if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                    continue;
                }

                // データをコピーする
                var buffer = data.ToByteArray( 0, data.pitches[0] * depthInfo.height );
                for ( int j = 0; j < depthInfo.height * depthInfo.width; ++j ){
                    if ( buffer[j] != 0 ) {
                        imageBuffer[j] = (byte)((i + 1) * 64);
                    }
                }

                // Blob画像を解放する
                blobImage.ReleaseAccess( data );

                // Blobの輪郭を表示する
                UpdateContoursImage( blob, i );
            }

            // Blob画像オブジェクトを解放する
            blobImage.Dispose();

            // ピクセルデータを更新する
            imageBitmap.WritePixels( imageRect, imageBuffer,
                DEPTH_WIDTH * BYTE_PER_PIXEL, 0 );
        }

        
        private void UpdateContoursImage( PXCMBlobData.IBlob blob, int index )
        {
            // 輪郭を表示する
            var numOfContours = blob.QueryNumberOfContours();
            for ( int i = 0; i < numOfContours; ++i ) {
                // 輪郭の点の数を取得する
                var size = blob.QueryContourSize( i );
                if ( size <= 0 ) {
                    continue;
                }

                // 輪郭の点を取得する
                PXCMPointI32[] points;
                var sts = blob.QueryContourPoints( i, out points );
                if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                    continue;
                }

                // 輪郭の点を描画する
                drawContour( points, index );
            }
        }

        // 輪郭の点を描画する
        void drawContour( PXCMPointI32[] points, int index )
        {
            var polygon = new Polygon()
            {
                Stroke = (index == 0) ? Brushes.DarkGray : Brushes.LightGray,
                StrokeThickness = 5,
            };

            // 点と点を線で結ぶ
            foreach ( var point in points ){
                polygon.Points.Add( new Point( point.x, point.y ) );
            }

            CanvasHandParts.Children.Add( polygon );
        }

        private void Uninitialize()
        {
            if ( senseManager != null ) {
                senseManager.Dispose();
                senseManager = null;
            }

            if ( blobModule != null ) {
                blobModule.Dispose();
                blobModule = null;
            }

            if ( blobData != null ) {
                blobData.Dispose();
                blobData = null;
            }
        }
    }
}
