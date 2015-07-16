using System.Windows;

namespace S3uploader
{
  /// <summary>
  /// Interaction logic for AddProfile.xaml
  /// </summary>
  public partial class AddProfile : Window
  {
    public AddProfile()
    {
      InitializeComponent();
    }

    private void Done_Click(object sender, RoutedEventArgs e)
    {
      Amazon.Util.ProfileManager.RegisterProfile(EnterProfile.Text, EnterKeyID.Text, EnterSecretKey.Text);
      Close();
    }
  }
}
