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

        PXCMTracker tracker;
        int targetId;

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
                PXCMCapture.Sample sample = senseManager.QueryTrackerSample();
                if ( sample != null ) {
                    UpdateColorImage( sample.color );
                }

                // オブジェクト追跡の更新
                UpdateObjectTraking();

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

                // カラーストリームを有効にする
                var sts = senseManager.EnableStream( PXCMCapture.StreamType.STREAM_TYPE_COLOR,
                    COLOR_WIDTH, COLOR_HEIGHT, COLOR_FPS );
                if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                    throw new Exception( "カラーストリームの有効化に失敗しました" );
                }

                // オブジェクトトラッカーを有効にする
                sts = senseManager.EnableTracker();
                if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                    throw new Exception( "オブジェクトトラッカーの有効化に失敗しました" );
                }

                // パイプラインを初期化する
                sts = senseManager.Init();
                if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                    throw new Exception( "初期化に失敗しました" );
                }

                // オブジェクトトラッカー関連の初期化
                InitializeObjectTracking();
            }
            catch ( Exception ex ) {
                MessageBox.Show( ex.Message );
                Close();
            }
        }

        // オブジェクトトラッカー関連の初期化
        private void InitializeObjectTracking()
        {
            // オブジェクトトラッカーを取得する
            tracker = senseManager.QueryTracker();
            if ( tracker == null ) {
                throw new Exception( "オブジェクトトラッカーの取得に失敗しました" );
            }

            // 追跡する画像を設定
            var sts = tracker.Set2DTrackFromFile( @"targetEarth.jpg", out targetId );
            if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                    throw new Exception( "追跡する画像の設定に失敗しました" );
            }
        }


        private void UpdateObjectTraking()
        {
            // キャンバスをクリアする
            CanvasPoint.Children.Clear();

            // 追跡しているオブジェクトを取得する
            PXCMTracker.TrackingValues trackData;
            var sts = tracker.QueryTrackingValues( targetId, out trackData );
            if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
               return;
            }

            // 追跡していたら表示する
            if ( PXCMTracker.IsTracking( trackData.state ) ) {
                ShowTrackingValue( trackData );
            }
        }

        private void ShowTrackingValue( PXCMTracker.TrackingValues arrData )
        {
            // for the middleware being a left hand coordinate system.
            float depthnum = 630;

            //correction values for translation and movement dependent on z values
            float depthcorrectionfactor = (arrData.translation.z / depthnum);
            arrData.translation.x = arrData.translation.x / depthcorrectionfactor;
            arrData.translation.y = arrData.translation.y / depthcorrectionfactor;

            var translation = arrData.translation;

            // オブジェクトのカメラ座標(画面の中心が原点)
            arrData.translation.y = -arrData.translation.y;
            arrData.translation.x += COLOR_WIDTH / 2;
            arrData.translation.y += COLOR_HEIGHT / 2;

            // 中心点を表示する
            AddEllipse( CanvasPoint, 
                new Point( arrData.translation.x, arrData.translation.y ),
                5, Brushes.Blue, -1 );

            //3x1 points for scaling
            PXCMPoint3DF32 trans1;
            PXCMPoint3DF32 trans2;
            PXCMPoint3DF32 trans3;

            //changes the size of the arrows
            int scalefactor = 150000; //150000
            float height = scalefactor / translation.z;
            float width = scalefactor / translation.z;
            float depth = scalefactor / translation.z;

            //creates the point vectors
            trans1.x = width;
            trans2.x = 0;
            trans3.x = 0;
            trans1.y = 0;
            trans2.y = height;
            trans3.y = 0;
            trans1.z = 0;
            trans2.z = 0;
            trans3.z = depth;

            PXCMPoint4DF32 rot = arrData.rotation;

            //put into 3x1 vector point for matrix multiplication purposes
            PXCMPoint3DF32 q;
            q.x = rot.x;
            q.y = rot.y;
            q.z = rot.z;
            float s = rot.w;

            // 画像の検出角度(向き)を求める
            double heading;
            double attitude;
            double bank;

            float sqw = s*s;
            float sqx = q.x*q.x;
            float sqy = q.y*q.y;
            float sqz = q.z*q.z;
            float unit = sqx + sqy + sqz + sqw;
            float check = q.x*q.y + q.z*s;
            float rad2deg = 180 / (float)Math.PI;

            heading = Math.Atan2( 2 * q.y*s - 2 * q.x*q.z, sqx - sqy - sqz + sqw ) * rad2deg;
            attitude = Math.Asin( 2.0*check / unit ) * rad2deg;
            bank = Math.Atan2( 2 * q.x*s - 2 * q.y*q.z, -sqx + sqy - sqz + sqw ) * rad2deg;


            // 向きの方向に伸ばした点(向きの線の終点)を計算する
            PXCMPoint3DF32 prime1;
            PXCMPoint3DF32 prime2;
            PXCMPoint3DF32 prime3;
            float[][] rotmat = new float[3][];
            rotmat[0] = new float[3];
            rotmat[1] = new float[3];
            rotmat[2] = new float[3];

            //rotation matrix using quaternion values
            rotmat[0][0] = (1 - (2 * (q.y*q.y)) - (2 * (q.z*q.z)));
            rotmat[0][1] = ((2 * q.x*q.y) - (2 * s*q.z));
            rotmat[0][2] = ((2 * q.x*q.z) + (2 * s*q.y));
            rotmat[1][0] = ((2 * q.x*q.y) + (2 * s*q.z));
            rotmat[1][1] = (1 - (2 * q.x*q.x) - (2 * q.z*q.z));
            rotmat[1][2] = ((2 * q.y*q.z) - (2 * s*q.x));
            rotmat[2][0] = ((2 * q.x*q.z) - (2 * s*q.y));
            rotmat[2][1] = ((2 * q.y*q.z) + (2 * s*q.x));
            rotmat[2][2] = (1 - (2 * q.x*q.x) - (2 * q.y*q.y));

            //rotation for x point	
            prime1.x = (rotmat[0][0] * trans1.x) +
                       (rotmat[0][1] * trans1.y) +
                       (rotmat[0][2] * trans1.z);
            prime1.y = (rotmat[1][0] * trans1.x) +
                       (rotmat[1][1] * trans1.y) +
                       (rotmat[1][2] * trans1.z);
            prime1.z = (rotmat[2][0] * trans1.x) +
                       (rotmat[1][2] * trans1.y) +
                       (rotmat[2][2] * trans1.z);

            //rotation for y point
            prime2.x = (rotmat[0][0] * trans2.x) +
                       (rotmat[0][1] * trans2.y) +
                       (rotmat[0][2] * trans2.z);
            prime2.y = (rotmat[1][0] * trans2.x) +
                       (rotmat[1][1] * trans2.y) +
                       (rotmat[1][2] * trans2.z);
            prime2.z = (rotmat[2][0] * trans2.x) +
                       (rotmat[1][2] * trans2.y) +
                       (rotmat[2][2] * trans2.z);

            //rotation for z point
            prime3.x = (rotmat[0][0] * trans3.x) +
                       (rotmat[0][1] * trans3.y) +
                       (rotmat[0][2] * trans3.z);
            prime3.y = (rotmat[1][0] * trans3.x) +
                       (rotmat[1][1] * trans3.y) +
                       (rotmat[1][2] * trans3.z);
            prime3.z = (rotmat[2][0] * trans3.x) +
                       (rotmat[1][2] * trans3.y) +
                       (rotmat[2][2] * trans3.z);

            // 各座標の向きを表示する
            AddLine( CanvasPoint, new Point( arrData.translation.x, arrData.translation.y ),
                new Point( arrData.translation.x + prime1.x, arrData.translation.y - prime1.y ),
                Brushes.Red );
            AddLine( CanvasPoint, new Point( arrData.translation.x, arrData.translation.y ),
                new Point( arrData.translation.x + prime2.x, arrData.translation.y - prime2.y ),
                Brushes.Green );
            AddLine( CanvasPoint, new Point( arrData.translation.x, arrData.translation.y ),
                new Point( arrData.translation.x + prime3.x, arrData.translation.y - prime3.y ),
                Brushes.Blue );
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

        void AddLine( Canvas canvas, Point point1, Point point2, Brush color, int thickness = 1 )
        {
            if ( double.IsNaN( point1.X ) || double.IsNaN( point1.Y )||
                 double.IsNaN( point2.X ) || double.IsNaN( point2.Y ) ) {
                     return;
            }

            var line = new Line()
            {
                X1 = point1.X,
                Y1 = point1.Y,
                X2 = point2.X,
                Y2 = point2.Y,
                StrokeThickness = thickness,
                Stroke = color,
            };

            canvas.Children.Add( line );
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
            ImageColor.Source = BitmapSource.Create( COLOR_WIDTH, COLOR_HEIGHT,
                96, 96, PixelFormats.Bgr24, null, buffer, COLOR_WIDTH * 3 );

            // データを解放する
            colorFrame.ReleaseAccess( data );
        }

        private void Uninitialize()
        {
            if ( senseManager != null ) {
                senseManager.Dispose();
                senseManager = null;
            }

            if ( tracker != null ) {
                tracker.Dispose();
                tracker = null;
            }
        }
    }
}
