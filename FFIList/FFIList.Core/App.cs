using FFIList.Core.ViewModels;
using MvvmCross.Platform.IoC;

namespace FFIList.Core
{
    public class App : MvvmCross.Core.ViewModels.MvxApplication
    {
        public override void Initialize()
        {
            CreatableTypes()
                .EndingWith("Service")
                .AsInterfaces()
                .RegisterAsLazySingleton();

            RegisterAppStart<StartViewModel>();
        }
    }
}
