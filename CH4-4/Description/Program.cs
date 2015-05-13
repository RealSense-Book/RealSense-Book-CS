using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Description
{
    class Program
    {
        static PXCMSenseManager senseManager;

        static void Main( string[] args )
        {
            try {
                // SenseManagerを生成する
                senseManager = PXCMSenseManager.CreateInstance();
                if ( senseManager == null ) {
                    throw new Exception( "SenseManagerの生成に失敗しました" );
                }

                // 何かしら有効にしないとInitが失敗するので適当に有効にする
                senseManager.EnableFace();


                // パイプラインを初期化する
                var sts = senseManager.Init();
                if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                    throw new Exception( "パイプラインの初期化に失敗しました" );
                }

                // 使用可能なデバイスを列挙する
                enumDevice();

            }
            catch ( Exception ex ) {
                Console.WriteLine( ex.Message );
            }
        }

        static void enumDevice()
        {
            // セッションを取得する
            var session = senseManager.QuerySession();
            if ( session == null ) {
                throw new Exception( "セッションの取得に失敗しました" );
            }

            // 取得するグループを設定する
            PXCMSession.ImplDesc mdesc = new PXCMSession.ImplDesc();
            mdesc.group = PXCMSession.ImplGroup.IMPL_GROUP_SENSOR;
            mdesc.subgroup = PXCMSession.ImplSubgroup.IMPL_SUBGROUP_VIDEO_CAPTURE;

            for ( int i = 0; ; ++i ) {
                // センサーグループを取得する
                PXCMSession.ImplDesc desc1;
                var sts = session.QueryImpl( mdesc, i, out desc1 );
                if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                    break;
                }

                // センサーグループ名を表示する
                Console.WriteLine( desc1.friendlyName );

                // キャプチャーオブジェクトを作成する
                PXCMCapture capture = null;
                sts = session.CreateImpl<PXCMCapture>( desc1, out capture );
                if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                    continue;
                }

                // デバイスを列挙する
                enumDevice( capture );

                // キャプチャーオブジェクトを解放する
                capture.Dispose();
            }
        }

        static void enumDevice( PXCMCapture capture )
        {
            for ( int i = 0; ; ++i ) {
                // デバイス情報を取得する
                PXCMCapture.DeviceInfo dinfo;
                var sts = capture.QueryDeviceInfo( i, out dinfo );
                if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                    break;
                }

                // デバイス名を表示する
                Console.WriteLine( "\t" + dinfo.name );

                // デバイスを取得する
                var device = capture.CreateDevice( i );

                for ( int s = 0; s < PXCMCapture.STREAM_LIMIT; ++s ) {
                    // ストリーム種別を取得する
                    PXCMCapture.StreamType type = PXCMCapture.StreamTypeFromIndex( s );
                    if ( (dinfo.streams & type) == 0 ) {
                        continue;
                    }

                    // ストリーム名を取得する
                    var name = PXCMCapture.StreamTypeToString( type );
                    Console.WriteLine( "\t\t" + name );

                    // ストリームのフォーマットを取得する
                    int nprofiles = device.QueryStreamProfileSetNum( type );
                    for ( int p = 0; p<nprofiles; ++p ) {
                        PXCMCapture.Device.StreamProfileSet profiles;
                        sts = device.QueryStreamProfileSet( type, p, out profiles );
                        if ( sts < pxcmStatus.PXCM_STATUS_NO_ERROR ) {
                            break;
                        }

                        // ストリームのフォーマットを表示する
                        Console.WriteLine( "\t\t\t" + ProfileToString( profiles[type] ) );
                    }
                }
            }

            Console.WriteLine( "" );
        }

        // プロファイル情報を文字列に変換する
        // raw_streams.csサンプルより
        static string ProfileToString( PXCMCapture.Device.StreamProfile pinfo )
        {
            string line = "Unknown ";
            if ( Enum.IsDefined( typeof( PXCMImage.PixelFormat ), pinfo.imageInfo.format ) ){
                line = pinfo.imageInfo.format.ToString().Substring( 13 )+ " " + 
                       pinfo.imageInfo.width + "x" + pinfo.imageInfo.height + "x";
            }
            else {
                line += pinfo.imageInfo.width + "x" + pinfo.imageInfo.height + "x";
            }

            if ( pinfo.frameRate.min != pinfo.frameRate.max ) {
                line += (float)pinfo.frameRate.min + "-" + (float)pinfo.frameRate.max;
            }
            else {
                float fps = (pinfo.frameRate.min != 0) ? pinfo.frameRate.min : pinfo.frameRate.max;
                line += fps;
            }

            return line;
        }
    }
}
