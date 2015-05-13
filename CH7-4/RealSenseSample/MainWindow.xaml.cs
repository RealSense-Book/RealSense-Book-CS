using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using voice_synthesis.cs;

namespace RealSenseSample
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        PXCMSenseManager senseManager;

        PXCMSpeechSynthesis synthesis;
        PXCMSpeechSynthesis.ProfileInfo profile = null;


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
                UpdateColorImage( sample.color );

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
            pxcmStatus ret =  colorFrame.AcquireAccess( PXCMImage.Access.ACCESS_READ, PXCMImage.PixelFormat.PIXEL_FORMAT_RGB24, out data );
            if ( ret < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                return;
            }

            // Bitmapに変換する
            var buffer = data.ToByteArray( 0, COLOR_WIDTH * COLOR_HEIGHT * 3 );
            ImageColor.Source = BitmapSource.Create( COLOR_WIDTH, COLOR_HEIGHT, 96, 96, PixelFormats.Bgr24, null, buffer, COLOR_WIDTH * 3 );

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

                // カラーストリームを有効にする
                senseManager.EnableStream( PXCMCapture.StreamType.STREAM_TYPE_COLOR,
                    COLOR_WIDTH, COLOR_HEIGHT, COLOR_FPS );

                // パイプラインを初期化する
                pxcmStatus ret =  senseManager.Init();
                if ( ret < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                    throw new Exception( "初期化に失敗しました" );
                }

                // ミラー表示にする
                senseManager.QueryCaptureManager().QueryDevice().SetMirrorMode(
                    PXCMCapture.Device.MirrorMode.MIRROR_MODE_HORIZONTAL );

                // 音声合成の初期化
                InitializeSpeechSynthesis();
            }
            catch ( Exception ex ) {
                MessageBox.Show( ex.Message );
                Close();
            }
        }

        // 音声合成を初期化する
        private void InitializeSpeechSynthesis()
        {
            pxcmStatus sts;
            var session = senseManager.QuerySession();

            // 音声合成エンジンを列挙する
            TextDesc.Text += "音声合成エンジン\n";

            PXCMSession.ImplDesc inDesc = new PXCMSession.ImplDesc();
            PXCMSession.ImplDesc outDesc = null;
            PXCMSession.ImplDesc desc = null;
            inDesc.cuids[0] = PXCMSpeechSynthesis.CUID;

            for ( int i = 0; ; ++i ) {
                // 音声合成エンジンを取得する
                sts = session.QueryImpl( inDesc, i, out outDesc );
                if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                    break;
                }

                // 音声合成エンジン名称を表示する
                TextDesc.Text += "\t" + outDesc.friendlyName + "\n";

                // 最初の音声合成エンジンを使う
                if ( i== 0 ) {
                    desc = outDesc;
                }
            }

            // 音声合成エンジンオブジェクトを作成する
            sts = session.CreateImpl<PXCMSpeechSynthesis>( desc, out synthesis );
            if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                throw new Exception( "音声合成エンジンオブジェクトの作成に失敗しました" );
            }

            // 対応言語を列挙する
            for ( int j = 0; ; ++j ) {
                // 音声合成エンジンが持っているプロファイルを取得する
                PXCMSpeechSynthesis.ProfileInfo pinfo;
                sts = synthesis.QueryProfile( j, out pinfo );
                if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                    break;
                }

                // 対応言語を表示する
                TextDesc.Text += "\t\t" + LanguageToString( pinfo.language ) + "\n";

                // 英語のエンジンを使う(日本語対応時には日本語に変更する)
                if ( pinfo.language == PXCMSpeechSynthesis.LanguageType.LANGUAGE_US_ENGLISH ) {
                    profile = pinfo;
                }
            }

            if ( profile == null ) {
                throw new Exception( "選択した音声合成エンジンが見つかりませんでした" );
            }

            // 音声合成時のパラメーターを設定する
            profile.volume = 80;
            profile.pitch = 100;
            profile.rate = 100;

            // 使用する言語を設定する
            sts = synthesis.SetProfile( profile );
            if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                throw new Exception( "音声合成エンジンオブジェクトの設定に失敗しました" );
            }
        }

        // voice_recognition.cs サンプルより
        private string LanguageToString( PXCMSpeechSynthesis.LanguageType language )
        {
            switch ( language ) {
            case PXCMSpeechSynthesis.LanguageType.LANGUAGE_US_ENGLISH:
                return "US English";
            case PXCMSpeechSynthesis.LanguageType.LANGUAGE_GB_ENGLISH:
                return "British English";
            case PXCMSpeechSynthesis.LanguageType.LANGUAGE_DE_GERMAN:
                return "Deutsch";
            case PXCMSpeechSynthesis.LanguageType.LANGUAGE_IT_ITALIAN:
                return "Italiano";
            case PXCMSpeechSynthesis.LanguageType.LANGUAGE_BR_PORTUGUESE:
                return "Português";
            case PXCMSpeechSynthesis.LanguageType.LANGUAGE_CN_CHINESE:
                return "中文";
            case PXCMSpeechSynthesis.LanguageType.LANGUAGE_FR_FRENCH:
                return "Français";
            case PXCMSpeechSynthesis.LanguageType.LANGUAGE_JP_JAPANESE:
                return "日本語";
            case PXCMSpeechSynthesis.LanguageType.LANGUAGE_US_SPANISH:
                return "US Español";
            case PXCMSpeechSynthesis.LanguageType.LANGUAGE_LA_SPANISH:
                return "LA Español";
            }
            return "Unknown";
        }

        private void Uninitialize()
        {
            if ( senseManager != null ) {
                senseManager.Dispose();
                senseManager = null;
            }

            if ( synthesis != null ) {
                synthesis.Dispose();
                synthesis = null;
            }
        }

        private void ButtonSpeechSynthesis_Click( object sender, RoutedEventArgs e )
        {
            var sts=synthesis.BuildSentence( 1, TextSentence.Text );
            if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                return;
            }

            // 音声合成した結果を出力する
            VoiceOut vo = new VoiceOut( profile.outputs );
            int bufferNum = synthesis.QueryBufferNum( 1 );
            for ( int i = 0; i < bufferNum; ++i ) {
                PXCMAudio sample = synthesis.QueryBuffer( 1, i );
                vo.RenderAudio( sample );
            }
            vo.Close();
        }
    }
}
