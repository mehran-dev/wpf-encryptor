using System.Collections.ObjectModel;

namespace BeRMOoDA.WPF.FileEncryptor.ViewModels
{
    class MainWindowViewModel : ViewModelBase
    {
          ObservableCollection<ViewModelBase> _viewModels;

            public MainWindowViewModel()
            {
               // EmployeeListViewModel viewModel = new EmployeeListViewModel(_employeeRepository);
                MainContentViewModel viewModel = new MainContentViewModel();
                this.ViewModels.Add(viewModel);
            }

            public ObservableCollection<ViewModelBase> ViewModels
            {
                get
                {
                    if (_viewModels == null)
                    {
                        _viewModels = new ObservableCollection<ViewModelBase>();
                    }
                    return _viewModels;
                }
            }
    }
}
