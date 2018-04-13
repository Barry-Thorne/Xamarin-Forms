﻿using System;
using System.IO;
using System.Threading.Tasks;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Storage;

namespace SimpleAudioRecorder.UWP
{
    internal class AudioRecorder : IAudioRecorder
    {
        MediaCapture mediaCapture;

        public bool CanRecordAudio => true;

        string audioFilePath;

        public async Task RecordAsync()
        {
            if (mediaCapture != null)
                throw new InvalidOperationException("Recording already in progress");

            try
            {
                var captureSettings = new MediaCaptureInitializationSettings()
                {
                    StreamingCaptureMode = StreamingCaptureMode.Audio
                };

                await InitMediaCapture(captureSettings);
            }
            catch (Exception ex)
            {
                DeleteMediaCapture();

                if (ex.InnerException != null && ex.InnerException.GetType() == typeof(UnauthorizedAccessException))
                {
                    throw ex.InnerException;
                }
                throw;
            }

            var localFolder = ApplicationData.Current.LocalFolder;
            var fileName = Path.GetRandomFileName();

            var fileOnDisk = await localFolder.CreateFileAsync(fileName);

            try
            {
               await mediaCapture.StartRecordToStorageFileAsync(MediaEncodingProfile.CreateWav(AudioEncodingQuality.Auto), fileOnDisk);
             //   await mediaCapture.StartRecordToStorageFileAsync(MediaEncodingProfile.CreateMp3(AudioEncodingQuality.Auto), fileOnDisk);
            }
            catch
            {
                DeleteMediaCapture();
                throw;
            }
         
            audioFilePath = fileOnDisk.Path;
        }

        async Task InitMediaCapture (MediaCaptureInitializationSettings settings)
        {
            mediaCapture = new MediaCapture();

            await mediaCapture.InitializeAsync(settings);

            mediaCapture.RecordLimitationExceeded += (MediaCapture sender) =>
            {
                DeleteMediaCapture();
                throw new Exception("Record Limitation Exceeded");
            };

            mediaCapture.Failed += (MediaCapture sender, MediaCaptureFailedEventArgs errorEventArgs) =>
            {
                DeleteMediaCapture();
                throw new Exception(string.Format("Code: {0}. {1}", errorEventArgs.Code, errorEventArgs.Message));
            };
        }

        public async Task<AudioRecording> StopAsync()
        {
            if (mediaCapture == null)
                throw new InvalidOperationException("No recording in progress");

            await mediaCapture.StopRecordAsync();

            mediaCapture.Dispose();
            mediaCapture = null;

            return GetRecording();
        }

        AudioRecording GetRecording()
        {
            if (File.Exists(audioFilePath))
                return new AudioRecording(audioFilePath);

            return null;
        }

        void DeleteMediaCapture ()
        {
            try
            {
                mediaCapture?.Dispose();
            }
            catch
            {
                //ignore
            }

            try
            {
                if(!string.IsNullOrWhiteSpace(audioFilePath) && File.Exists(audioFilePath))
                    File.Delete(audioFilePath);
            }
            catch 
            {
                //ignore
            }

            audioFilePath = string.Empty;
            mediaCapture = null;
        }
    }
}