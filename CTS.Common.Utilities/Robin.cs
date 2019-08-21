using Microsoft.CognitiveServices.Speech;
using Microsoft.CognitiveServices.Speech.Audio;
using System.Threading.Tasks;

namespace CTS.Common.Utilities
{
    public class Robin
    {
        private readonly SpeechConfig speechConfig = SpeechConfig.FromSubscription("f885654361df407b9bdb621b45dd10c5", "CentralUS");

        private readonly AudioConfig audioConfig = AudioConfig.FromDefaultSpeakerOutput();

        public async Task SpeakAsync(string text)
        {
            using (var synthesizer = new SpeechSynthesizer(speechConfig, audioConfig))
            {
                using (var result = await synthesizer.SpeakTextAsync(text))
                {
                    if (result.Reason == ResultReason.SynthesizingAudioCompleted)
                    {
                        return;
                    }
                }
            }
        }
    }
}

