using Hqub.MusicBrainz.Entities;
using MusicSearcher.Model;

namespace MusicSearcher.Converter
{
    internal static class MusicRecordingConverter
    {
        public static MusicRecording ToInternal(this Recording recording)
        {
            return new MusicRecording
            {
                Id = recording.Id,
                Title = recording.Title,
            };
        }
    }
}
