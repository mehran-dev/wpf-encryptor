using System.Windows;
using BeRMOoDA.WPF.FileEncrypter;

namespace BeRMOoDA.WPF.FileEncryptor
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.SourceInitialized += new System.EventHandler(MainWindow_SourceInitialized);
            this.MouseDown += new System.Windows.Input.MouseButtonEventHandler(MainWindow_MouseDown);
        }

        void MainWindow_MouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            this.DragMove();
        }

        void MainWindow_SourceInitialized(object sender, System.EventArgs e)
        {
            AeroGlassProvider.ExtendGlassFrame(this, new Thickness(-1));
        }
    }
}
