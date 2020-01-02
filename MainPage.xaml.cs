using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Windows.UI.Notifications;
using Microsoft.Toolkit.Uwp.Notifications;
using Microsoft.QueryStringDotNET;
using System.Threading.Tasks;
using Windows.Media.Audio;
using Windows.Media.Capture;
using Windows.Media.Render;
using Windows.Media;
using System.Runtime.InteropServices;

// 空白ページの項目テンプレートについては、https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x411 を参照してください

namespace ToastTest
{
    /// <summary>
    /// それ自体で使用できる空白ページまたはフレーム内に移動できる空白ページ。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        public MainPage()
        {
            this.InitializeComponent();
        }

        private void StartButton_Tapped(object sender, TappedRoutedEventArgs e)
        {
            Toast();
        }

        void Toast()
        {
            string title = "Title";
            string content = "Content";
            string image = "https://picsum.photos/360/202?image=883";
            string logo = "";
            var toastContent = new ToastContent()
            {
                Visual = new ToastVisual()
                {
                    BindingGeneric = new ToastBindingGeneric()
                    {
                        Children =
                    {
                        new AdaptiveText() { Text = title },
                        new AdaptiveText() { Text = content },
                        new AdaptiveImage() { Source = image },
                    },
                        AppLogoOverride = new ToastGenericAppLogo()
                        {
                            Source = logo,
                            HintCrop = ToastGenericAppLogoCrop.Circle
                        }
                    },
                }
            };

            var tileContent = new TileContent
            {
                Visual = new TileVisual
                {
                    TileMedium = new TileBinding
                    {
                        Content = new TileBindingContentAdaptive
                        {
                            Children =
                            {
                                new AdaptiveText() { Text = title },
                                new AdaptiveText() { Text = content },
                                new AdaptiveImage() { Source = image },
                            }
                        }
                    }
                }
            };

            //send toast
            var toastNotification = new ToastNotification(toastContent.GetXml());
            ToastNotificationManager.CreateToastNotifier().Show(toastNotification);

            //send tile 
            var tileNotification = new TileNotification(tileContent.GetXml());
            TileUpdateManager.CreateTileUpdaterForApplication().Update(tileNotification);

            //send badge
            //var badgeContent = new BadgeNumericContent()
            //{
            //    Number = 10,
            //};
            //var badgeNotification = new BadgeNotification(badgeContent.GetXml());
            //BadgeUpdateManager.CreateBadgeUpdaterForApplication().Update(badgeNotification);
        }

        //https://docs.microsoft.com/ja-jp/windows/uwp/audio-video-camera/audio-graphs
        //http://ja.voidcc.com/question/p-aqzjrtka-dn.html
        //https://github.com/Microsoft/Windows-universal-samples/blob/714722f53c3ae270f634f54cb2740411f6b11032/Samples/AudioCreation/cs/CustomEffect/CustomEffect.cs
        AudioGraph audioGraph;
        AudioDeviceInputNode deviceInputNode;
        AudioDeviceOutputNode deviceOutputNode;
        AudioFrameOutputNode frameOutputNode;

        async Task<AudioGraph> InitAudioGraph()
        {
            var settings = new AudioGraphSettings(AudioRenderCategory.Media);
            //settings.DesiredSamplesPerQuantum = 0;
            //settings.DesiredRenderDeviceAudioProcessing = AudioProcessing.Default;
            //settings.QuantumSizeSelectionMode = QuantumSizeSelectionMode.ClosestToDesired;
            var result = await AudioGraph.CreateAsync(settings);
            if (result.Status != AudioGraphCreationStatus.Success)
            {
                //error
                return null;
            }

            return result.Graph;
        }

        async Task<AudioDeviceInputNode> CreateDeviceInputNode()
        {
            var result = await audioGraph.CreateDeviceInputNodeAsync(MediaCategory.Media);

            if (result.Status != AudioDeviceNodeCreationStatus.Success)
            {
                //error
                return null;
            }

            return result.DeviceInputNode;
        }

        List<double> data = new List<double>();
        [ComImport]
        [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        unsafe interface IMemoryBufferByteAccess
        {
            void GetBuffer(out byte* buffer, out uint capacity);
        }

        void test()
        {

            audioGraph.QuantumProcessed += AudioGraph_QuantumProcessed;
        }

        private void AudioGraph_QuantumProcessed(AudioGraph sender, object args)
        {
            var frame = frameOutputNode.GetFrame();
            ProcessFrameOutput(frame);
        }

        unsafe void ProcessFrameOutput(AudioFrame frame)
        {
            using (AudioBuffer buffer = frame.LockBuffer(AudioBufferAccessMode.Write))
            using (IMemoryBufferReference reference = buffer.CreateReference())
            {
                byte* dataInBytes;
                uint capacityInBytes;
                float* dataInFloat;

                // Get the buffer from the AudioFrame
                ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacityInBytes);

                dataInFloat = (float*)dataInBytes;

                data.Clear();

                for (int i = 0; i <= 32; i++)
                {
                    data.Add(dataInFloat[i]);
                }
            }
        }

        /// <summary>
        /// Returns a low-pass filter of the data
        /// </summary>
        /// <param name="data">Data to filter</param>
        /// <param name="cutoff_freq">The frequency below which data will be preserved</param>
        float[] lowPassFilter(ref float[] data, float cutoff_freq, int sample_rate, float quality_factor = 1.0f)
        {
            // Calculate filter parameters
            float O = (float)(2.0 * Math.PI * cutoff_freq / sample_rate);
            float C = quality_factor / O;
            float L = 1 / quality_factor / O;

            // Loop through and apply the filter
            float[] output = new float[data.Length];
            float V = 0, I = 0, T;
            for (int s = 0; s < data.Length; s++)
            {
                T = (I - V) / C;
                I += (data[s] * O - V) / L;
                V += T;
                output[s] = V / O;
            }

            return output;
        }

        //async Task<AudioDeviceOutputNode> CreateDeviceOutputNode()
        //{
        //    return null;
        //}
    }
}
