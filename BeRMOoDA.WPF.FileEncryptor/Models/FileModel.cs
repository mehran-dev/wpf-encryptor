using System.Windows;
using System.Windows.Media;

namespace BeRMOoDA.WPF.FileEncryptor.Models
{
    public class FileModel : DependencyObject
    {
        public string Name { get; set; }
        public double Size { get; set; }
        public static DependencyProperty StatusProperty = DependencyProperty.Register("Status", typeof(Brush), typeof(FileModel));

        delegate void _SetterDelegate(DependencyProperty dp, Brush brush);

        public Brush Status
        {
            get
            {
                return (Brush)GetValue(StatusProperty);
            }

            set
            {
                this.Dispatcher.Invoke(new _SetterDelegate(SetValue), StatusProperty, value);
            }
        }

        public FileModel()
        {
            Name = "N/A";
            Size = 0;
            Status = Brushes.Transparent;
        }
    }
}
