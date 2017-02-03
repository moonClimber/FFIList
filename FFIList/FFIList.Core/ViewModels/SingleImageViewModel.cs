using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using MvvmCross.Core.ViewModels;

namespace FFIList.Core.ViewModels
{
    public class SingleImageViewModel:MvxViewModel
    {
        public override void Start()
        {
            base.Start();
            Image = new ItemModel("https://randomuser.me/api/portraits/thumb/men/1.jpg",1);
        }

        private ItemModel _image;
        public ItemModel Image
        {
            get { return _image; }
            set
            {
                _image = value;
                RaisePropertyChanged(() => Image);
            }
        }


        #region GoBack Command			
        private MvxCommand _goBackCommand;
        public ICommand GoBack
        {
            get
            {
                _goBackCommand = _goBackCommand ?? new MvxCommand(DoGoBack);
                return _goBackCommand;
            }
        }
        private void DoGoBack()
        {
            Close(this);
        }
        #endregion


    }
}
