using System;
using NAudio.CoreAudioApi;

namespace CarShell.Services
{
    public static class AudioService
    {
        public static int GetVolume()
        {
            try
            {
                using var enumerator = new MMDeviceEnumerator();

                using var device = enumerator.GetDefaultAudioEndpoint(
                    DataFlow.Render,
                    Role.Multimedia);

                return (int)Math.Round(
                    device.AudioEndpointVolume.MasterVolumeLevelScalar * 100);
            }
            catch
            {
                return -1;
            }
        }

        public static bool SetVolume(int volume)
        {
            volume = Math.Clamp(volume, 0, 100);

            try
            {
                using var enumerator = new MMDeviceEnumerator();

                using var device = enumerator.GetDefaultAudioEndpoint(
                    DataFlow.Render,
                    Role.Multimedia);

                device.AudioEndpointVolume.MasterVolumeLevelScalar =
                    volume / 100f;

                return true;
            }
            catch
            {
                return false;
            }
        }

        public static bool IsMuted()
        {
            try
            {
                using var enumerator = new MMDeviceEnumerator();

                using var device = enumerator.GetDefaultAudioEndpoint(
                    DataFlow.Render,
                    Role.Multimedia);

                return device.AudioEndpointVolume.Mute;
            }
            catch
            {
                return false;
            }
        }

        public static bool SetMuted(bool muted)
        {
            try
            {
                using var enumerator = new MMDeviceEnumerator();

                using var device = enumerator.GetDefaultAudioEndpoint(
                    DataFlow.Render,
                    Role.Multimedia);

                device.AudioEndpointVolume.Mute = muted;

                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}