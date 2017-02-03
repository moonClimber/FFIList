using MvvmCross.Core.ViewModels;

namespace FFIList.Core.ViewModels
{
public class ItemModel : MvxViewModel
    {
        public ItemModel()
        {
            
        }

        public ItemModel(string url, int idx):this()
        {
            ImageUrl = url;
            ImageIndex = idx;
        }

        private int _index;
        public int ImageIndex
        {
            get { return _index; }
            set
            {
                _index = value;
                RaisePropertyChanged(() => ImageIndex);
            }
        }



        private string _imageUrl;
        public string ImageUrl
        {
            get { return _imageUrl; }
            set
            {
                _imageUrl = value;
                RaisePropertyChanged(() => ImageUrl);
            }
        }


        //public string ImageUrl { get; set; }
    }
}
