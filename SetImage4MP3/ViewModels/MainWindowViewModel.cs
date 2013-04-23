using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using Livet;
using Livet.Commands;
using Livet.Messaging;
using Livet.Messaging.IO;
using TagLib;
using ID3v2 = TagLib.Id3v2;
using IO = System.IO;

namespace SetImage4MP3.ViewModels
{
    public class MainWindowViewModel : ViewModel
    {
        #region MP3Files NotifyChangeProperty
        private IEnumerable<File> _MP3Files;

        public IEnumerable<File> MP3Files
        {
            get
            { return _MP3Files; }
            set
            { 
                if (_MP3Files.SequenceEqual(value))
                    return;
                _MP3Files = value;
                RaisePropertyChanged();
                RemoveFilesCommand.RaiseCanExecuteChanged();
                SaveFilesCommand.RaiseCanExecuteChanged();
            }
        }
        #endregion

        #region ImageSource NotifyChangeProperty
        private string _ImageSource;

        public string ImageSource
        {
            get
            { return _ImageSource; }
            set
            { 
                if (_ImageSource == value)
                    return;
                _ImageSource = value;
                RaisePropertyChanged();
                RemoveImageCommand.RaiseCanExecuteChanged();
            }
        }
        #endregion

        #region RemoveImageCommand
        private ViewModelCommand _RemoveImageCommand;

        public ViewModelCommand RemoveImageCommand
        {
            get
            {
                if (_RemoveImageCommand == null)
                {
                    _RemoveImageCommand = new ViewModelCommand(RemoveImage, CanRemoveImage);
                }
                return _RemoveImageCommand;
            }
        }

        public bool CanRemoveImage()
        {
            return ImageSource != null;
        }

        public void RemoveImage()
        {
            ImageSource = null;
            RemoveImageCommand.RaiseCanExecuteChanged();
        }
        #endregion

        #region RemoveFilesCommand
        private ViewModelCommand _RemoveFilesCommand;

        public ViewModelCommand RemoveFilesCommand
        {
            get
            {
                if (_RemoveFilesCommand == null)
                {
                    _RemoveFilesCommand = new ViewModelCommand(RemoveFiles, CanRemoveFiles);
                }
                return _RemoveFilesCommand;
            }
        }

        public bool CanRemoveFiles()
        {
            return MP3Files != null && MP3Files.Any();
        }

        public void RemoveFiles()
        {
            MP3Files = null;
        }
        #endregion

        #region SaveFilesCommand
        private ViewModelCommand _SaveFilesCommand;

        public ViewModelCommand SaveFilesCommand
        {
            get
            {
                if (_SaveFilesCommand == null)
                {
                    _SaveFilesCommand = new ViewModelCommand(SaveFiles, CanSaveFiles);
                }
                return _SaveFilesCommand;
            }
        }

        public bool CanSaveFiles()
        {
            return MP3Files != null && MP3Files.Any();
        }

        public void SaveFiles()
        {
            var task = MP3Files.ToObservable()
                        .ForEachAsync(x =>
                        {
                            var tag = x.GetTag(TagTypes.Id3v2, true) as ID3v2.Tag;
                            var pic = new Picture(ImageSource);
                            foreach (var p in tag.Pictures)
                            {
                                tag.RemoveFrame(p as ID3v2.Frame);
                            }
                            tag.AddFrame(new ID3v2.AttachedPictureFrame(pic));
                            x.Save();
                        });
            task.ContinueWith(
                t =>
                Messenger.RaiseAsync(new InformationMessage("An error occurred while saving file.", "Error", MessageBoxImage.Error, "Info")),
                TaskContinuationOptions.OnlyOnFaulted);
            task.ContinueWith(
                t =>
                Messenger.RaiseAsync(new InformationMessage("Setting of the image was completed.", "Complete", MessageBoxImage.Information, "Info")),
                TaskContinuationOptions.OnlyOnRanToCompletion);
        }
        #endregion

        public void Initialize()
        {
            
        }

        public void RequestAddFiles(OpeningFileSelectionMessage message)
        {
            if (message.Response != null)
            {
                MP3Files = message.Response.Select(File.Create).Union(MP3Files ?? new List<File>(0));
            }
        }

        public void RequestLoadImage(OpeningFileSelectionMessage message)
        {
            if (message.Response != null)
            {
                ImageSource = message.Response.First();
            }
        }

        public void LoadImageFromClipboard()
        {
            var image = Clipboard.GetImage();
            if (image == null) return;

            var tempFilePath = IO.Path.GetTempFileName();
            var frame = BitmapFrame.Create(image);
            using (var stream = new IO.FileStream(tempFilePath, IO.FileMode.Open, IO.FileAccess.Write))
            {
                var enc = new PngBitmapEncoder();
                enc.Frames.Add(frame);
                enc.Save(stream);
            }
            ImageSource = tempFilePath;
        }
    }
}
