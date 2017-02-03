using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using MvvmCross.Core.ViewModels;

namespace FFIList.Core.ViewModels
{
    public class StartViewModel : MvxViewModel
    {

        #region OpenList Command			
        private MvxCommand _openListCommand;
        public ICommand OpenList
        {
            get
            {
                _openListCommand = _openListCommand ?? new MvxCommand(DoOpenList);
                return _openListCommand;
            }
        }
        private void DoOpenList()
        {
            ShowViewModel<ImagesListViewModel>();
        }
        #endregion


        #region OpenSingle Command			
        private MvxCommand _openSingleCommand;
        public ICommand OpenSingle
        {
            get
            {
                _openSingleCommand = _openSingleCommand ?? new MvxCommand(DoOpenSingle);
                return _openSingleCommand;
            }
        }
        private void DoOpenSingle()
        {
            ShowViewModel<SingleImageViewModel>();
        }
        #endregion


    }
}
