using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

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
      this.Close();
    }
  }
}
