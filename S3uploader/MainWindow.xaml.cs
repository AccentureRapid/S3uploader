using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;

using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;

namespace S3uploader
{
  static class S3Client
  {
    private static IAmazonS3 _client;
    private static AWSCredentials _credentials;
    private static string _profile;
    private static RegionEndpoint _awsEndpoint = RegionEndpoint.EUWest1;

    public static string Profile
    {
      get { return _profile; }
      set
      {
        _profile = value;
        _credentials = new StoredProfileAWSCredentials(_profile);
        _client = new AmazonS3Client(_credentials, _awsEndpoint);
      }
    }

    public static IAmazonS3 Client
    {
      get { return _client; }
    }

    public static RegionEndpoint Region
    {
      get { return _awsEndpoint; }
      set
      {
        _awsEndpoint = value;
        _client = new AmazonS3Client(_credentials, _awsEndpoint);
        
      }
    }

    public static string BucketName { get; set; }
  }

  static class EnableUpload
  {
    private static bool _gotBucket = false;
    private static bool _gotFiles = false;

    public static bool BucketState(bool GotBucket)
    {
      _gotBucket = GotBucket;
      return _gotBucket && _gotFiles;
    }
    public static bool FilesState(bool GotFiles)
    {
      _gotFiles = GotFiles;
      return _gotBucket && _gotFiles;
    }
  }

  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    CancellationTokenSource cts;
    
    public MainWindow()
    {
      InitializeComponent();
      PopulateListProfiles();
    }

    public void PopulateListRegions()
    {
      ListRegions.Items.Clear();
      foreach (RegionEndpoint ep in RegionEndpoint.EnumerableAllRegions)
      {
        ListRegions.Items.Add(ep.SystemName);
      }
      ListRegions.IsEnabled = true;
      ListRegions.SelectedItem = S3Client.Region.SystemName;
    }

    public void PopulateListProfiles()
    {
      int profileCount = 0;
      ListProfiles.Items.Clear();
      foreach (String p in Amazon.Util.ProfileManager.ListProfileNames())
      {
        profileCount++;
        ListProfiles.Items.Add(p);
      }
      if (profileCount > 0)
      {
        ListProfiles.IsEnabled = true;
      }
    }

    public void PopulateListBuckets()
    {
      ListBuckets.Items.Clear();
      EnableUpload.BucketState(false);
      ButtonUpload.IsEnabled = false;
      ListBuckets.IsEnabled = false;
      if (S3Client.Client != null)
      {
        ListBucketsResponse response = S3Client.Client.ListBuckets();
        foreach (S3Bucket bucket in response.Buckets)
        {
          ListBuckets.Items.Add(bucket.BucketName);
        }
      }
      if (ListBuckets.Items.Count > 0)
      {
        ListBuckets.IsEnabled = true;
      }
    }

    private void FileListBox_Drop(object sender, DragEventArgs e)
    {
      if (!e.Data.GetDataPresent(DataFormats.FileDrop))
      {
        return;
      }
      string[] fileList = (string[])e.Data.GetData(DataFormats.FileDrop);
      foreach (string filePath in fileList)
      {
        FileListBox.Items.Add(filePath);
      }
      if (FileListBox.Items.Count > 0)
      {
        if (EnableUpload.FilesState(true))
        {
          ButtonUpload.IsEnabled = true;
        }
      }
    }

    private void FileListBox_DragOver(object sender, DragEventArgs e)
    {
      if (e.Data.GetDataPresent(DataFormats.FileDrop))
      {
        e.Effects = DragDropEffects.Copy;
      }
      else
      {
        e.Effects = DragDropEffects.None;
      }
    }

    private void AddEditFileList_OnClick(object sender, RoutedEventArgs e)
    {
      if (!Clipboard.ContainsFileDropList())
      {
        return;
      }
      var filelist = Clipboard.GetFileDropList();
      foreach (var filePath in filelist)
      {
        FileListBox.Items.Add(filePath);
      }
      if (FileListBox.Items.Count > 0)
      {
        if (EnableUpload.FilesState(true))
        {
          ButtonUpload.IsEnabled = true;
        }
      } 
    }

    private void ClearEditFileList_OnClick(object sender, RoutedEventArgs e)
    {
      FileListBox.Items.Clear();
      EnableUpload.FilesState(false);
      ButtonUpload.IsEnabled = false;
    }

    private void RemoveEditFileList_OnClick(object sender, RoutedEventArgs e)
    {
      while (FileListBox.SelectedItems.Count > 0)
      {
        FileListBox.Items.Remove(FileListBox.SelectedItem);
      }
      if (FileListBox.Items.Count == 0)
      {
        EnableUpload.FilesState(false);
        ButtonUpload.IsEnabled = false;
      }
    }

    private void Add_Click(object sender, RoutedEventArgs e)
    {
      Microsoft.Win32.OpenFileDialog d = new Microsoft.Win32.OpenFileDialog();
      if (d.ShowDialog() == true)
      {
        FileListBox.Items.Add(d.FileName);
      }
      if (FileListBox.Items.Count > 0)
      {
        if (EnableUpload.FilesState(true))
        {
          ButtonUpload.IsEnabled = true;
        }
      }
    }

    private void AddDir_Click(object sender, RoutedEventArgs e)
    {
      System.Windows.Forms.FolderBrowserDialog d = new System.Windows.Forms.FolderBrowserDialog();
      System.Windows.Forms.DialogResult result = d.ShowDialog();
      if (result == System.Windows.Forms.DialogResult.OK)
      {
        string[] fileList = Directory.GetFiles(d.SelectedPath);
        foreach (string filePath in fileList)
        {
          FileListBox.Items.Add(filePath);
        }
      }
      if (FileListBox.Items.Count > 0)
      {
        if (EnableUpload.FilesState(true))
        {
          ButtonUpload.IsEnabled = true;
        }
      }
    }

    private void Remove_Click(object sender, RoutedEventArgs e)
    {
      while (FileListBox.SelectedItems.Count > 0)
      {
        FileListBox.Items.Remove(FileListBox.SelectedItem);
      }
      if (FileListBox.Items.Count == 0)
      {
        EnableUpload.FilesState(false);
        ButtonUpload.IsEnabled = false;
      }
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
      FileListBox.Items.Clear();
      EnableUpload.FilesState(false);
      ButtonUpload.IsEnabled = false;
    }

    private void MenuExit_Click(object sender, RoutedEventArgs e)
    {
      Environment.Exit(0);
    }

    private void AddProfile_Click(object sender, RoutedEventArgs e)
    {
      AddProfile AddNewProfile = new AddProfile();
      AddNewProfile.ShowDialog();
    }

    private void ViewProfile_Click(object sender, RoutedEventArgs e)
    {
      string pName = "Profile";
      string messageText = "AWS key";
      MessageBoxResult result = MessageBox.Show(messageText, pName); 
    }

    private void ListProfiles_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      string selected = ListProfiles.SelectedItem.ToString();
      S3Client.Profile = selected;
      PopulateListBuckets();
      PopulateListRegions();
      ViewProfile.IsEnabled = true;
    }

    private async void ButtonUpload_Click(object sender, RoutedEventArgs e)
    {
      cts = new CancellationTokenSource();
      FileListBox.SelectedItems.Clear();
      while (FileListBox.Items.Count > 0)
      {
        //List<string> filePaths = new List<string>();
        FileListBox.SelectedIndex = 0;
        ButtonCancelUpload.IsEnabled = true;
        UploadProgress.Visibility = Visibility.Visible;
        UploadProgress.Value = 0;
        string path = FileListBox.SelectedItem.ToString();
        string extension = System.IO.Path.GetExtension(path);
        string filename = System.IO.Path.GetFileName(path);
        string contentType = AmazonS3Util.MimeTypeFromExtension(extension);
        //cts = new CancellationTokenSource();
        //filePaths.Add(fp.ToString());
        FilePathTextBlock.Text = filename;

        try
        {
          using (FileStream fs = File.OpenRead(path))
          {
            PutObjectRequest streamRequest = new PutObjectRequest()
            {
              BucketName = S3Client.BucketName,
              Key = filename,
              InputStream = fs
            };
            streamRequest.ContentType = contentType;
            streamRequest.CannedACL = S3CannedACL.PublicRead;
            streamRequest.ReadWriteTimeout = TimeSpan.FromHours(1);
            streamRequest.StreamTransferProgress += OnUploadProgress;
            await S3Client.Client.PutObjectAsync(streamRequest, cts.Token);
            FileListBox.Items.Remove(FileListBox.SelectedItem);
            //MessageBox.Show(filename + " uploaded");
          }
        }
        catch (OperationCanceledException)
        {
          MessageBox.Show(filename + " Upload Cancelled");
          break;
        }
        catch (AmazonS3Exception amazonS3Exception)
        {
          MessageBox.Show(amazonS3Exception.Message);
          break;
        }
        catch (Exception)
        {
          MessageBox.Show("Other exception");
          break;
        }
        finally
        {
          FilePathTextBlock.Text = "";
          UploadProgress.Value = 0;
          UploadProgress.Visibility = Visibility.Hidden;
          ButtonCancelUpload.IsEnabled = false;
        }
      }
    }

    public void OnUploadProgress(object sender, StreamTransferProgressArgs args)
    {
      if (args.PercentDone >= 0 && args.PercentDone <= 100)
      {
        UploadProgress.Dispatcher.Invoke(DispatcherPriority.Normal, new Action(() =>
        {
          UploadProgress.Value = args.PercentDone;
        }));
      }
    }

    private void ListBuckets_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (ListBuckets.SelectedItem != null)
      {
        S3Client.BucketName = ListBuckets.SelectedItem.ToString();
        if (EnableUpload.BucketState(true))
        {
          ButtonUpload.IsEnabled = true;
        }
      }
    }

    private void ButtonCancelUpload_Click(object sender, RoutedEventArgs e)
    {
      if (cts != null)
        cts.Cancel();
    }

    private void ListRegions_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (ListRegions.SelectedItem != null)
      {
        S3Client.Region = RegionEndpoint.GetBySystemName(ListRegions.SelectedItem.ToString());
      }
    }
  }
}
