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
        PXCM3DScan scanner;

        PXCM3DScan.ReconstructionOption reconstructionOption =
            PXCM3DScan.ReconstructionOption.NO_RECONSTRUCTION_OPTIONS;
        PXCM3DScan.FileFormat fileFormat = PXCM3DScan.FileFormat.OBJ;

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
                UpdateColorImage( scanner.AcquirePreviewImage() );

                // フレームを解放する
                senseManager.ReleaseFrame();
            }
            catch ( Exception ex ) {
                MessageBox.Show( ex.Message );
                Close();
            }
        }

        private void UpdateColorImage( PXCMImage colorFrame )
        {
            // データを取得する
            PXCMImage.ImageData data;
            pxcmStatus ret =  colorFrame.AcquireAccess( PXCMImage.Access.ACCESS_READ,
                PXCMImage.PixelFormat.PIXEL_FORMAT_RGB24, out data );
            if ( ret < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                return;
            }

            // Bitmapに変換する
            var buffer = data.ToByteArray(
                0, colorFrame.info.width * colorFrame.info.height * 3 );
            ImageColor.Source = BitmapSource.Create(
                colorFrame.info.width, colorFrame.info.height, 96, 96,
                PixelFormats.Bgr24, null, buffer, colorFrame.info.width * 3 );

            ImageColor.Width = colorFrame.info.width;
            ImageColor.Height = colorFrame.info.height;

            // データを解放する
            colorFrame.ReleaseAccess( data );
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
                if ( senseManager  == null ) {
                    throw new Exception( "SenseManagerの生成に失敗しました" );
                }

                // 3Dスキャンを有効にする
                var sts = senseManager.Enable3DScan();
                if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                    throw new Exception( "初期化に失敗しました" );
                }

                // パイプラインを初期化する
                sts = senseManager.Init();
                if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                    throw new Exception( "初期化に失敗しました" );
                }

                // ミラー表示にする
                senseManager.QueryCaptureManager().QueryDevice().SetMirrorMode(
                    PXCMCapture.Device.MirrorMode.MIRROR_MODE_HORIZONTAL );

                // 3Dスキャンの初期化
                Initialize3dScan();
            }
            catch ( Exception ex ) {
                MessageBox.Show( ex.Message );
                Close();
            }
        }

        private void Uninitialize()
        {
            if ( scanner != null ) {
                scanner.Dispose();
                scanner = null;
            }

            if ( senseManager != null ) {
                senseManager.Dispose();
                senseManager = null;
            }
        }

        // 3Dスキャンの初期化処理
        private void Initialize3dScan()
        {
            // スキャナーを取得する
            scanner = senseManager.Query3DScan();
            if ( scanner == null ) {
                throw new Exception( "スキャナーの取得に失敗しました" );
            }

            // ターゲットオプションの設定
            SetTargetingOption( 
                PXCM3DScan.TargetingOption.NO_TARGETING_OPTIONS );

            // スキャンモードの設定
            SetScanMode( PXCM3DScan.Mode.TARGETING );

            // モデル作成オプションの表示
            ShowReconstructionOption();
            ShowModelFormat();
        }

        // キーボード処理
        private void Window_KeyDown( object sender, System.Windows.Input.KeyEventArgs e )
        {
            if ( e.Key == System.Windows.Input.Key.T ) {
                // ターゲットオプションを変更する
                var option = scanner.QueryTargetingOptions();
                if ( option == PXCM3DScan.TargetingOption.NO_TARGETING_OPTIONS ){
                    SetTargetingOption(
                        PXCM3DScan.TargetingOption.OBJECT_ON_PLANAR_SURFACE_DETECTION );
                }
                else{
                    SetTargetingOption(
                        PXCM3DScan.TargetingOption.NO_TARGETING_OPTIONS );
                }
            }
            else if ( e.Key == System.Windows.Input.Key.S ) {
                // スキャンモードを変更する
                var scanMode = scanner.QueryMode();
                if ( scanMode == PXCM3DScan.Mode.TARGETING ){
                    SetScanMode( PXCM3DScan.Mode.SCANNING );
                }
                else{
                    SetScanMode( PXCM3DScan.Mode.TARGETING );
                }
            }
            else if ( e.Key == System.Windows.Input.Key.O ) {
                // モデル作成オプションを変更する
                ChangeReconstructionOption();
            }
            else if ( e.Key == System.Windows.Input.Key.F ) {
                // モデル作成フォーマットを変更する
                ChangeModelFormat();
            }
            else if ( e.Key == System.Windows.Input.Key.R ) {
                // モデルを作成する
                Reconstruct();
            }
        }

        // ターゲットオプションを設定する
        private void SetTargetingOption( PXCM3DScan.TargetingOption targetingOption )
        {
            TextMode.Text = "TargetingOption " + targetingOption.ToString();
            var sts = scanner.SetTargetingOptions( targetingOption );
            if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ){
                throw new Exception( "ターゲットオプションの設定に失敗しました" );
            }
        }

        // スキャンモードを設定する
        private void SetScanMode( PXCM3DScan.Mode scanMode )
        {
            TextMode.Text = "ScanMode " + scanMode.ToString();
            var sts = scanner.SetMode( scanMode );
            if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ){
                throw new Exception( "スキャンモードの設定に失敗しました" );
            }
        }

        // モデル作成オプションを変更する
        private void ChangeReconstructionOption()
        {
            if ( reconstructionOption ==
                    PXCM3DScan.ReconstructionOption.NO_RECONSTRUCTION_OPTIONS ){
                reconstructionOption = PXCM3DScan.ReconstructionOption.SOLIDIFICATION;
            }
            else {
                reconstructionOption = PXCM3DScan.ReconstructionOption.NO_RECONSTRUCTION_OPTIONS;
            }

            ShowReconstructionOption();
        }

        // モデル作成オプションを表示する
        private void ShowReconstructionOption()
        {
            TextReconstructOption.Text =
                "Reconstruction Option : " + reconstructionOption.ToString();
        }

        // モデルフォーマットを変更する
        private void ChangeModelFormat()
        {
            if ( fileFormat == PXCM3DScan.FileFormat.OBJ ){
                fileFormat = PXCM3DScan.FileFormat.STL;
            }
            else if ( fileFormat == PXCM3DScan.FileFormat.STL ){
                fileFormat = PXCM3DScan.FileFormat.PLY;
            }
            else {
                fileFormat = PXCM3DScan.FileFormat.OBJ;
            }

            ShowModelFormat();
        }

        // モデルフォーマットを表示する
        private void ShowModelFormat()
        {
            TextModelFormat.Text =
                "Model Format : " + PXCM3DScan.FileFormatToString( fileFormat );
        }

        // モデルを作成する
        private void Reconstruct()
        {
            // スキャン中以外はモデルを作成しない
            var scanMode = scanner.QueryMode();
            if ( scanMode != PXCM3DScan.Mode.SCANNING ){
                return;
            }

            // ファイル名を作成する
            var time = DateTime.Now.ToString("hhmmss", 
                System.Globalization.CultureInfo.CurrentUICulture.DateTimeFormat);
            var fileName = string.Format( "model-{0}.{1}",
                time, PXCM3DScan.FileFormatToString( fileFormat ) );

            // 3Dモデルを作成する
            scanner.Reconstruct( fileFormat, fileName, reconstructionOption );

            TextOutputFile.Text = fileName;
        }
    }
}
