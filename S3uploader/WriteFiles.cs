using System;
using System.Threading;
using System.Windows.Threading;
using System.Windows;
using System.IO;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Util;

namespace S3uploader
{
  public partial class MainWindow
  {

    private async void ButtonUpload_Click(object sender, RoutedEventArgs e)
    {
      cts = new CancellationTokenSource();
      FileListBox.SelectedItems.Clear();
      while (FileListBox.Items.Count > 0)
      {
        FileListBox.SelectedIndex = 0;
        ButtonCancelUpload.IsEnabled = true;
        UploadProgress.Visibility = Visibility.Visible;
        UploadProgress.Value = 0;
        string path = FileListBox.SelectedItem.ToString();
        string extension = Path.GetExtension(path);
        string filename = Path.GetFileName(path);
        string contentType = AmazonS3Util.MimeTypeFromExtension(extension);
        FilePathTextBlock.Text = filename;

        try
        {
          using (FileStream fs = File.OpenRead(path))
          {
            var streamRequest = new PutObjectRequest
            {
              BucketName = S3Client.BucketName,
              Key = filename,
              InputStream = fs,
              ContentType = contentType,
              CannedACL = S3CannedACL.PublicRead,
              ReadWriteTimeout = TimeSpan.FromHours(1)
            };
            streamRequest.StreamTransferProgress += OnUploadProgress;
            await S3Client.Client.PutObjectAsync(streamRequest, cts.Token);
            FileListBox.Items.Remove(FileListBox.SelectedItem);
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

    private void ButtonCancelUpload_Click(object sender, RoutedEventArgs e)
    {
      if (cts != null)
        cts.Cancel();
    }
  }
}
