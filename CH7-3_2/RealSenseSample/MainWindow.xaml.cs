using System;
using System.Threading;
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

        PXCMAudioSource audioSource;
        PXCMSpeechRecognition recognition;

        // UIスレッドに戻すためのコンテキスト
        SynchronizationContext context = SynchronizationContext.Current;

        const int COLOR_WIDTH = 640;
        const int COLOR_HEIGHT = 480;
        const int COLOR_FPS = 30;

        //const int COLOR_WIDTH = 1920;
        //const int COLOR_HEIGHT = 1080;
        //const int COLOR_FPS = 30;

        public interface SpeechRecognitionHandler
        {

        }

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
            pxcmStatus ret =  colorFrame.AcquireAccess( PXCMImage.Access.ACCESS_READ,
                PXCMImage.PixelFormat.PIXEL_FORMAT_RGB24, out data );
            if ( ret < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                return;
            }

            // Bitmapに変換する
            var buffer = data.ToByteArray( 0, COLOR_WIDTH * COLOR_HEIGHT * 3 );
            ImageColor.Source = BitmapSource.Create( COLOR_WIDTH, COLOR_HEIGHT, 96, 96,
                PixelFormats.Bgr24, null, buffer, COLOR_WIDTH * 3 );

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

                // 音声認識を初期化する
                InitializeSpeechRecognition();
            }
            catch ( Exception ex ) {
                MessageBox.Show( ex.Message );
                Close();
            }
        }

        // 音声認識を初期化する
        private void InitializeSpeechRecognition()
        {
            pxcmStatus sts;
            var session = senseManager.QuerySession();

            // 音声入力デバイスを作成する
            audioSource = session.CreateAudioSource();
            if ( audioSource == null ){
                throw new Exception( "音声入力デバイスの作成に失敗しました" );
            }

            // 音声入力デバイスを列挙する
            TextDesc.Text = "";
            TextDesc.Text += "音声入力デバイス\n";

            PXCMAudioSource.DeviceInfo device = null;

            audioSource.ScanDevices();
            for ( int i = 0;; ++i ) {
                PXCMAudioSource.DeviceInfo dinfo;
                sts = audioSource.QueryDeviceInfo( i, out dinfo );
                if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                    break;
                }

                // 音声入力デバイス名を表示する
                TextDesc.Text += "\t" + dinfo.name + "\n";

                // 最初のデバイスを使う
                if ( i == 0 ){
                    device = dinfo;
                }
            }

            // 音声入力デバイスを設定する
            sts = audioSource.SetDevice( device );
            if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                throw new Exception( "音声入力デバイスの設定に失敗しました" );
            }


            // 音声認識エンジンを列挙する
            TextDesc.Text += "音声認識エンジン\n";

            PXCMSession.ImplDesc inDesc = new PXCMSession.ImplDesc();
            PXCMSession.ImplDesc outDesc = null;
            PXCMSession.ImplDesc desc = null;
            inDesc.cuids[0] = PXCMSpeechRecognition.CUID;

            for ( int i = 0; ; ++i ) {
                // 音声認識エンジンを取得する
                sts = session.QueryImpl( inDesc, i, out outDesc );
                if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                    break;
                }

                // 音声認識エンジン名称を表示する
                TextDesc.Text += "\t" + outDesc.friendlyName + "\n";

                // 最初の音声認識エンジンを使う
                if( i== 0 ){
                    desc = outDesc;
                }
            }

            // 音声認識エンジンオブジェクトを作成する
            sts = session.CreateImpl<PXCMSpeechRecognition>( desc, out recognition );
            if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                throw new Exception( "音声認識エンジンオブジェクトの作成に失敗しました" );
            }

            // 対応言語を列挙する
            PXCMSpeechRecognition.ProfileInfo profile = null;

            for ( int j = 0;; ++j ) {
                // 音声認識エンジンが持っているプロファイルを取得する
                PXCMSpeechRecognition.ProfileInfo pinfo;
                sts = recognition.QueryProfile( j, out pinfo );
                if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                    break;
                }

                // 対応言語を表示する
                TextDesc.Text += "\t\t" + LanguageToString( pinfo.language ) + "\n";

                // 英語のエンジンを使う(日本語対応時には日本語に変更する)
                if ( pinfo.language == PXCMSpeechRecognition.LanguageType.LANGUAGE_US_ENGLISH ){
                    profile = pinfo;
                }
            }

            if ( profile == null ){
                throw new Exception( "選択した音声認識エンジンが見つかりませんでした" );
            }

            // 使用する言語を設定する
            sts = recognition.SetProfile( profile );
            if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                throw new Exception( "音声認識エンジンオブジェクトの設定に失敗しました" );
            }

            // コマンドモードを設定する
            SetCommandMode();

            // 音声認識の通知ハンドラを作成する
            PXCMSpeechRecognition.Handler handler = new PXCMSpeechRecognition.Handler();
            handler.onRecognition = OnRecognition;

            // 音声認識を開始する
            sts = recognition.StartRec( audioSource, handler );
            if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                throw new Exception( "音声認識の開始に失敗しました" );
            }
        }

        void SetCommandMode()
        {
            int grammar = 1;

            // 認識させたいコマンド
            string[] commands = new string[] {
                "Hello",
                "Good",
                "Bad",
            };

            // 認識させたいコマンドを解析する
            var sts = recognition.BuildGrammarFromStringList( grammar, commands, null );
            if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                throw new Exception( "コマンドの解析に失敗しました" );
            }

            // 認識させたいコマンドを登録する
            sts = recognition.SetGrammar( grammar );
            if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                throw new Exception( "コマンドの設定に失敗しました" );
            }
        }

        // 音声認識した語の通知ハンドラ
        void OnRecognition( PXCMSpeechRecognition.RecognitionData data )
        {
            // UIスレッドに同期的に処理を戻す
            context.Post( state =>
            {
                ListRecognition.Items.Add("Command");

                // コマンドモードの時はラベルに登録したコマンドのインデックスが設定される
                for ( int i = 0; i < PXCMSpeechRecognition.NBEST_SIZE; i++ ) {
                    if ( data.scores[i].label < 0 || data.scores[i].confidence == 0 ) {
                        continue;
                    }

                    // 認識した語が信頼性の高い順に設定される
                    ListRecognition.Items.Add( string.Format( "{0}, {1}, {2}",
                        data.scores[i].label, data.scores[i].confidence,
                        data.scores[i].sentence ) );
                }
            }, null );
        }

        // voice_recognition.cs サンプルより
        private string LanguageToString( PXCMSpeechRecognition.LanguageType language )
        {
            switch ( language ) {
            case PXCMSpeechRecognition.LanguageType.LANGUAGE_US_ENGLISH:
                return "US English";
            case PXCMSpeechRecognition.LanguageType.LANGUAGE_GB_ENGLISH:
                return "British English";
            case PXCMSpeechRecognition.LanguageType.LANGUAGE_DE_GERMAN:
                return "Deutsch";
            case PXCMSpeechRecognition.LanguageType.LANGUAGE_IT_ITALIAN:
                return "Italiano";
            case PXCMSpeechRecognition.LanguageType.LANGUAGE_BR_PORTUGUESE:
                return "Português";
            case PXCMSpeechRecognition.LanguageType.LANGUAGE_CN_CHINESE:
                return "中文";
            case PXCMSpeechRecognition.LanguageType.LANGUAGE_FR_FRENCH:
                return "Français";
            case PXCMSpeechRecognition.LanguageType.LANGUAGE_JP_JAPANESE:
                return "日本語";
            case PXCMSpeechRecognition.LanguageType.LANGUAGE_US_SPANISH:
                return "US Español";
            case PXCMSpeechRecognition.LanguageType.LANGUAGE_LA_SPANISH:
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
        }
    }
}
