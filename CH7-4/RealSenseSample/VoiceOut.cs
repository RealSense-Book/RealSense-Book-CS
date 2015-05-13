/*******************************************************************************

INTEL CORPORATION PROPRIETARY INFORMATION
This software is supplied under the terms of a license agreement or nondisclosure
agreement with Intel Corporation and may not be copied or disclosed except in
accordance with the terms of that agreement
Copyright(c) 2011-2013 Intel Corporation. All Rights Reserved.

*******************************************************************************/
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Media;

namespace voice_synthesis.cs 
{
    public class VoiceOut
    {
        private MemoryStream fs;
        private BinaryWriter bw;

        public VoiceOut(PXCMAudio.AudioInfo ainfo)
        {
            fs = new MemoryStream();
            bw = new BinaryWriter(fs);

            bw.Write((int)0x46464952);  // chunkIdRiff:'FFIR'
            bw.Write((int)0);           // chunkDataSizeRiff
            bw.Write((int)0x45564157);  // riffType:'EVAW'
            bw.Write((int)0x20746d66);  // chunkIdFmt:' tmf'
            bw.Write((int)0x12);        // chunkDataSizeFmt
            bw.Write((short)1);         // compressionCode
            bw.Write((short)ainfo.nchannels);  // numberOfChannels
            bw.Write((int)ainfo.sampleRate);   // sampleRate
            bw.Write((int)(ainfo.sampleRate * 2 * ainfo.nchannels));        // averageBytesPerSecond
            bw.Write((short)(ainfo.nchannels * 2));   // blockAlign
            bw.Write((short)16);        // significantBitsPerSample
            bw.Write((short)0);         // extraFormatSize
            bw.Write((int)0x61746164);  // chunkIdData:'atad'
            bw.Write((int)0);           // chunkIdSizeData
        }

        public bool RenderAudio(PXCMAudio audio)
        {
            PXCMAudio.AudioData adata;
            pxcmStatus sts = audio.AcquireAccess(PXCMAudio.Access.ACCESS_READ, PXCMAudio.AudioFormat.AUDIO_FORMAT_PCM, out adata);
            if (sts < pxcmStatus.PXCM_STATUS_NO_ERROR) return false;
            bw.Write(adata.ToByteArray());
            audio.ReleaseAccess(adata);
            return true;
        }

        public void Close()
        {
            long pos = bw.Seek(0, SeekOrigin.Current);
            bw.Seek(0x2a, SeekOrigin.Begin); // chunkDataSizeData
            bw.Write((int)(pos - 46));
            bw.Seek(0x04, SeekOrigin.Begin); // chunkDataSizeRiff
            bw.Write((int)(pos - 8));

            bw.Seek(0, SeekOrigin.Begin);
            SoundPlayer sp = new SoundPlayer(fs);
            sp.PlaySync();
            sp.Dispose();

            bw.Close();
            fs.Close();
        }
    }
}


