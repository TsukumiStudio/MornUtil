using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace MornSoundProcessor
{
    internal static class MornSoundProcessorUtil
    {
        internal static AudioClip ConvertClip(AudioClip clip)
        {
            var instance = MornSoundProcessorSettings.instance;
            if (instance.UseCutBeginningSilence)
            {
                clip = CutBeginningSilence(clip, instance.BeginningOffsetSample, instance.BeginningAmplitude);
            }

            if (instance.UseCutEndingSilence)
            {
                clip = CutEndingSilence(clip, instance.EndingOffsetSample, instance.EndingAmplitude);
            }

            if (instance.UseNormalizeAmplitude)
            {
                clip = NormalizeAmplitude(clip, instance.NormalizeAmplitude);
            }

            return clip;
        }

        internal static AudioClip SaveClip(AudioClip clip)
        {
            var instance = MornSoundProcessorSettings.instance;
            var dirs = instance.UnderAssetsFolderName.Split('/');
            var combinePath = "Assets";
            foreach (var dir in dirs)
            {
                if (AssetDatabase.IsValidFolder($"{combinePath}/{dir}") == false)
                {
                    AssetDatabase.CreateFolder(combinePath, dir);
                    Debug.Log($"フォルダー {combinePath}/{dir} を作成しました");
                }

                combinePath += $"/{dir}";
            }

            var path = $"Assets/{instance.UnderAssetsFolderName}/{clip.name}_Converted.wav";
            SaveAudioClipToWav(clip, path);
            AssetDatabase.Refresh(ImportAssetOptions.Default);
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        internal static AudioClip NormalizeAmplitude(AudioClip clip, float maxAmplitude)
        {
            var samples = clip.samples;
            var frequency = clip.frequency;
            var channels = clip.channels;
            var data = new float[samples * channels];
            clip.GetData(data, 0);
            var max = Mathf.Max(Mathf.Abs(data.Min()), Mathf.Abs(data.Max()));
            var rate = maxAmplitude / max;
            for (var i = 0; i < data.Length; i++)
            {
                data[i] *= rate;
            }

            var normalizeClip = AudioClip.Create(clip.name, samples, channels, frequency,
                clip.loadType == AudioClipLoadType.Streaming);
            normalizeClip.SetData(data, 0);
            return normalizeClip;
        }

        internal static AudioClip CutBeginningSilence(AudioClip clip, int beginOffsetSample, float beginAmplitude)
        {
            var samples = clip.samples;
            var frequency = clip.frequency;
            var channels = clip.channels;
            var data = new float[samples * channels];
            clip.GetData(data, 0);
            var startIndex = GetSoundBeginningIndex(data, beginAmplitude, channels);
            startIndex = Mathf.Max(startIndex - beginOffsetSample * channels, 0);
            var newSamples = samples - startIndex / channels;
            var cutClip = AudioClip.Create(clip.name, newSamples, channels, frequency,
                clip.loadType == AudioClipLoadType.Streaming);
            var cachedArray = new float[newSamples * channels];
            for (var i = 0; i < newSamples * channels; i++)
            {
                cachedArray[i] = data[startIndex + i];
            }

            cutClip.SetData(cachedArray, 0);
            return cutClip;
        }

        internal static AudioClip CutEndingSilence(AudioClip clip, int endOffsetSample, float endAmplitude)
        {
            var samples = clip.samples;
            var frequency = clip.frequency;
            var channels = clip.channels;
            var data = new float[samples * channels];
            clip.GetData(data, 0);
            var endIndex = GetSoundEndingIndex(data, endAmplitude, channels);
            endIndex = Mathf.Min(endIndex + endOffsetSample * channels, samples * channels);
            var newSamples = endIndex / channels;
            var cutClip = AudioClip.Create(clip.name, newSamples, channels, frequency,
                clip.loadType == AudioClipLoadType.Streaming);
            var cachedArray = new float[newSamples * channels];
            for (var i = 0; i < newSamples * channels; i++)
            {
                cachedArray[i] = data[i];
            }

            cutClip.SetData(cachedArray, 0);
            return cutClip;
        }

        private static int GetSoundBeginningIndex(IReadOnlyList<float> list, float minAmplitude, int channels)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (Mathf.Abs(list[i]) > minAmplitude)
                {
                    return i - i % channels;
                }
            }

            return 0;
        }

        private static int GetSoundEndingIndex(IReadOnlyList<float> list, float minAmplitude, int channels)
        {
            for (var i = list.Count - 1; i >= 0; i--)
            {
                if (Mathf.Abs(list[i]) > minAmplitude)
                {
                    return i - i % channels;
                }
            }

            return list.Count - 1 - (list.Count - 1) % channels;
        }

        private static void SaveAudioClipToWav(AudioClip clip, string path)
        {
            var samples = clip.samples;
            var frequency = clip.frequency;
            var channels = clip.channels;
            var data = new float[samples * channels];
            clip.GetData(data, 0);

            using var fileStream = new FileStream(path, FileMode.Create, FileAccess.Write);
            WriteWavHeader(fileStream, samples, (short)channels, frequency);
            foreach (var value in data)
            {
                WriteShortLittleEndian(fileStream,
                    value > 0 ? (short)(short.MaxValue * value) : (short)(short.MinValue * -value));
            }
        }

        private static void WriteWavHeader(Stream stream, int samples, short channels, int frequency)
        {
            var fileSize = 2 * samples * channels + 44;
            stream.Seek(0, SeekOrigin.Begin);

            stream.WriteByte((byte)'R');
            stream.WriteByte((byte)'I');
            stream.WriteByte((byte)'F');
            stream.WriteByte((byte)'F');

            WriteIntLittleEndian(stream, fileSize - 8);

            stream.WriteByte((byte)'W');
            stream.WriteByte((byte)'A');
            stream.WriteByte((byte)'V');
            stream.WriteByte((byte)'E');

            stream.WriteByte((byte)'f');
            stream.WriteByte((byte)'m');
            stream.WriteByte((byte)'t');
            stream.WriteByte((byte)' ');

            WriteIntLittleEndian(stream, 16);

            WriteShortLittleEndian(stream, 1);

            WriteShortLittleEndian(stream, channels);

            WriteIntLittleEndian(stream, frequency);

            WriteIntLittleEndian(stream, frequency * 2 * channels);

            WriteShortLittleEndian(stream, (short)(2 * channels));

            WriteShortLittleEndian(stream, 16);

            stream.WriteByte((byte)'d');
            stream.WriteByte((byte)'a');
            stream.WriteByte((byte)'t');
            stream.WriteByte((byte)'a');

            WriteIntLittleEndian(stream, 2 * samples * channels);
        }

        private static void WriteIntLittleEndian(Stream stream, int value)
        {
            stream.WriteByte((byte)value);
            stream.WriteByte((byte)(value >> 8));
            stream.WriteByte((byte)(value >> 16));
            stream.WriteByte((byte)(value >> 24));
        }

        private static void WriteShortLittleEndian(Stream stream, short value)
        {
            stream.WriteByte((byte)value);
            stream.WriteByte((byte)(value >> 8));
        }
    }
}
