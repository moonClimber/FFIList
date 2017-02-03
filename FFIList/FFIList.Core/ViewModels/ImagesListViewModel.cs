using System.Collections.Generic;
using System.Windows.Input;
using MvvmCross.Core.ViewModels;

namespace FFIList.Core.ViewModels
{
    public class ImagesListViewModel 
        : MvxViewModel
    {
        public override void Start()
        {
            base.Start();
            var myList = new List<ItemModel>
            {
                new ItemModel("https://randomuser.me/api/portraits/thumb/men/50.jpg", 0),
                new ItemModel("https://randomuser.me/api/portraits/thumb/men/51.jpg", 1),
                new ItemModel("https://randomuser.me/api/portraits/thumb/men/52.jpg", 2),
                new ItemModel("https://randomuser.me/api/portraits/thumb/men/53.jpg", 3),
                new ItemModel("https://randomuser.me/api/portraits/thumb/men/54.jpg", 4),
                new ItemModel("https://randomuser.me/api/portraits/thumb/men/55.jpg", 5),
                new ItemModel("https://randomuser.me/api/portraits/thumb/men/56.jpg", 6),
                new ItemModel("https://randomuser.me/api/portraits/thumb/men/57.jpg", 7),
                new ItemModel("https://randomuser.me/api/portraits/thumb/men/58.jpg", 8),
                new ItemModel("https://randomuser.me/api/portraits/thumb/men/59.jpg", 9),
                new ItemModel("https://randomuser.me/api/portraits/thumb/men/60.jpg", 10),
                new ItemModel("https://randomuser.me/api/portraits/thumb/men/61.jpg", 11),
                new ItemModel("https://randomuser.me/api/portraits/thumb/men/62.jpg", 12),
                new ItemModel("https://randomuser.me/api/portraits/thumb/men/63.jpg", 13),
                new ItemModel("https://randomuser.me/api/portraits/thumb/men/64.jpg", 14),
                new ItemModel("https://randomuser.me/api/portraits/thumb/men/65.jpg", 15),
                new ItemModel("https://randomuser.me/api/portraits/thumb/men/66.jpg", 16),
                new ItemModel("https://randomuser.me/api/portraits/thumb/men/67.jpg", 17),
                new ItemModel("https://randomuser.me/api/portraits/thumb/men/68.jpg", 18),
                new ItemModel("https://randomuser.me/api/portraits/thumb/men/69.jpg", 19),
                new ItemModel("https://randomuser.me/api/portraits/thumb/women/50.jpg",20),
                new ItemModel("https://randomuser.me/api/portraits/thumb/women/51.jpg",21),
                new ItemModel("https://randomuser.me/api/portraits/thumb/women/52.jpg",22),
                new ItemModel("https://randomuser.me/api/portraits/thumb/women/53.jpg",23),
                new ItemModel("https://randomuser.me/api/portraits/thumb/women/54.jpg",24),
                new ItemModel("https://randomuser.me/api/portraits/thumb/women/55.jpg",25),
                new ItemModel("https://randomuser.me/api/portraits/thumb/women/56.jpg",26),
                new ItemModel("https://randomuser.me/api/portraits/thumb/women/57.jpg",27),
                new ItemModel("https://randomuser.me/api/portraits/thumb/women/58.jpg",28),
                new ItemModel("https://randomuser.me/api/portraits/thumb/women/59.jpg",29),
                new ItemModel("https://randomuser.me/api/portraits/thumb/women/60.jpg",30),
                new ItemModel("https://randomuser.me/api/portraits/thumb/women/61.jpg",31),
                new ItemModel("https://randomuser.me/api/portraits/thumb/women/62.jpg",32),
                new ItemModel("https://randomuser.me/api/portraits/thumb/women/63.jpg",33),
                new ItemModel("https://randomuser.me/api/portraits/thumb/women/64.jpg",34),
                new ItemModel("https://randomuser.me/api/portraits/thumb/women/65.jpg",35),
                new ItemModel("https://randomuser.me/api/portraits/thumb/women/66.jpg",36),
                new ItemModel("https://randomuser.me/api/portraits/thumb/women/67.jpg",37),
                new ItemModel("https://randomuser.me/api/portraits/thumb/women/68.jpg",38),
                new ItemModel("https://randomuser.me/api/portraits/thumb/women/69.jpg",39)
            };

            //Items.Add(new ItemModel(@"https://randomuser.me/api/portraits/thumb/women/50.jpg"));
            //Items.Add(new ItemModel(@"https://randomuser.me/api/portraits/thumb/women/51.jpg"));
            //Items.Add(new ItemModel(@"https://randomuser.me/api/portraits/thumb/women/52.jpg"));
            //Items.Add(new ItemModel(@"https://randomuser.me/api/portraits/thumb/women/53.jpg"));
            //Items.Add(new ItemModel(@"https://randomuser.me/api/portraits/thumb/women/54.jpg"));
            //Items.Add(new ItemModel(@"https://randomuser.me/api/portraits/thumb/women/55.jpg"));
            //Items.Add(new ItemModel(@"https://randomuser.me/api/portraits/thumb/women/56.jpg"));
            //Items.Add(new ItemModel(@"https://randomuser.me/api/portraits/thumb/women/57.jpg"));
            //Items.Add(new ItemModel(@"https://randomuser.me/api/portraits/thumb/women/58.jpg"));
            //Items.Add(new ItemModel(@"https://randomuser.me/api/portraits/thumb/women/59.jpg"));
            //Items.Add(new ItemModel(@"https://randomuser.me/api/portraits/thumb/women/60.jpg"));
            //Items.Add(new ItemModel(@"https://randomuser.me/api/portraits/thumb/women/61.jpg"));
            //Items.Add(new ItemModel(@"https://randomuser.me/api/portraits/thumb/women/62.jpg"));
            //Items.Add(new ItemModel(@"https://randomuser.me/api/portraits/thumb/women/63.jpg"));
            //Items.Add(new ItemModel(@"https://randomuser.me/api/portraits/thumb/women/64.jpg"));
            //Items.Add(new ItemModel(@"https://randomuser.me/api/portraits/thumb/women/65.jpg"));
            //Items.Add(new ItemModel(@"https://randomuser.me/api/portraits/thumb/women/66.jpg"));
            //Items.Add(new ItemModel(@"https://randomuser.me/api/portraits/thumb/women/67.jpg"));
            //Items.Add(new ItemModel(@"https://randomuser.me/api/portraits/thumb/women/68.jpg"));
            //Items.Add(new ItemModel(@"https://randomuser.me/api/portraits/thumb/women/69.jpg"));

            Items = myList;
        }

        private IList<ItemModel> _items;
        public IList<ItemModel> Items
        {
            get { return _items; }
            set
            {
                _items = value;
                RaisePropertyChanged(() => Items);
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
