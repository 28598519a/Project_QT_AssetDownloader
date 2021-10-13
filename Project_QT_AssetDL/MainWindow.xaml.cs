using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace Project_QT_AssetDL
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 同時下載的線程池上限
        /// </summary>
        int pool = 50;

        private async void Btn_download_list_Click(object sender, RoutedEventArgs e)
        {
            List<Task> tasks = new List<Task>();
            if (cb_devices.SelectedIndex == 1)
            {
                var result = System.Windows.MessageBox.Show("Choosing Android lacks some assets, continue downloading?", "Warning" , MessageBoxButton.YesNo);
                if (result == MessageBoxResult.No) return;

                tasks.Add(DownLoadFile("https://us.nkrpg.com/api/system/assets", Path.Combine(App.Root, "assets.json"), cb_isCover.IsChecked == true ? true : false));
                tasks.Add(DownLoadFile("https://us.nkrpg.com/api/auth/assets/option", Path.Combine(App.Root, "option.json"), cb_isCover.IsChecked == true ? true : false));
                tasks.Add(DownLoadFile("https://us.nkrpg.com/api/auth/assets/secret", Path.Combine(App.Root, "secret.json"), cb_isCover.IsChecked == true ? true : false));
            }
            else
            {
                tasks.Add(DownLoadFile("https://us.nkrpg.com/api/system/assets", Path.Combine(App.Root, "assets.json"), cb_isCover.IsChecked == true ? true : false));
            }
            await Task.WhenAll(tasks);
            tasks.Clear();
            System.Windows.MessageBox.Show($"Download finsh，total {App.glocount} files", "Finish");
            App.glocount = 0;
            
        }

        private async void Btn_download_Click(object sender, RoutedEventArgs e)
        {
            List<Tuple<string, string>> DicPath = new List<Tuple<string, string>>();

            Microsoft.Win32.OpenFileDialog openFileDialog = new Microsoft.Win32.OpenFileDialog();
            openFileDialog.InitialDirectory = App.Root;
            openFileDialog.Filter = "assets.json|*.json";

            if (!openFileDialog.ShowDialog() == true)
                return;

            JObject AssetList = JObject.Parse(File.ReadAllText(openFileDialog.FileName));

            /*
            openFileDialog.InitialDirectory = Path.GetDirectoryName(openFileDialog.FileName);
            openFileDialog.Filter = "secret.json|*.json";
            openFileDialog.FileName = String.Empty;

            if (!openFileDialog.ShowDialog() == true)
                return;

            JObject SecretList = JObject.Parse(File.ReadAllText(openFileDialog.FileName));
            */

            JObject OptionList = new JObject();
            if (cb_devices.SelectedIndex == 1)
            {
                openFileDialog.InitialDirectory = Path.GetDirectoryName(openFileDialog.FileName);
                openFileDialog.Filter = "option.json|*.json";
                openFileDialog.FileName = String.Empty;

                if (!openFileDialog.ShowDialog() == true)
                    return;

                OptionList = JObject.Parse(File.ReadAllText(openFileDialog.FileName));
            }

            string resWebRoot = AssetList["response"]["download_path"].ToString();
            JArray fileList = JArray.Parse(AssetList["response"]["cms_upload"]["upload_files"].ToString());

            foreach(JToken ja in fileList)
            {
                /* path開頭有"/"，不能用Path.Combine否則會被誤認為絕對路徑，因此直接先去掉
                 * https://stackoverflow.com/questions/53102/why-does-path-combine-not-properly-concatenate-filenames-that-start-with-path-di
                 */
                string path = ja[0].ToString().TrimStart('/');
                string url = ja[1].ToString();

                
                DicPath.Add(new Tuple<string, string>($"{resWebRoot}{url}", $"{Path.Combine(App.Root, "Project QT", "cms_upload", path)}"));
            }

            JArray fileList1 = JArray.Parse(JObject.Parse(AssetList["response"]["asset"][0].ToString())["asset_patchs"].ToString());

            foreach (JToken ja in fileList1)
            {
                string name = ja[0].ToString();
                string url = ja[1].ToString();
                string path = ja[4].ToString().TrimStart('/');
                
                DicPath.Add(new Tuple<string, string>($"{resWebRoot}/{url}", $"{Path.Combine(App.Root, "Project QT", "asset", path, name)}"));
            }

            /*
            JArray SecretfileList = JArray.Parse(JObject.Parse(SecretList["response"]["assets"][0].ToString())["asset_patchs"].ToString());
            List<string> SecretfileListFinal = new List<string>();
            List<Task<string>> tasksecret = new List<Task<string>>();
            int count_s = 0;
            int quantity_s = SecretfileList.Count;

            foreach (JToken ja in SecretfileList)
            {
                string name = ja[0].ToString();
                tasksecret.Add(SecretPost("https://us.nkrpg.com/api/system/assets/check", name, "WVG00000*****", "b1b23a7743ddef333ae64297f51bfb71c46*****"));
                count_s++;
                if ((count_s % pool).Equals(0) || quantity_s == count_s)
                {
                    string[] t = await Task.WhenAll(tasksecret);
                    tasksecret.Clear();
                    try
                    {
                        foreach(string s in t)
                        {
                            if (!String.IsNullOrEmpty(s))
                                SecretfileListFinal.Add(JObject.Parse(s)["response"]["asset_patchs"].ToString());
                        }
                    }
                    catch { }
                }
            }

            foreach (JToken ja in SecretfileListFinal)
            {
                string name = ja[0].ToString();
                string url = ja[1].ToString();
                string path = ja[4].ToString().TrimStart('/');

                DicPath.Add(new Tuple<string, string>($"{resWebRoot}/{url}", $"{Path.Combine(App.Root, "Project QT", "asset", path, name)}"));
            }
            */

            // Android base asset.json is incomplete, need option.json to get all assets
            if (OptionList.Count != 0 && cb_devices.SelectedIndex == 1)
            {
                JArray OptionFileList = JArray.Parse(JObject.Parse(OptionList["response"]["assets"][0].ToString())["asset_patchs"].ToString());

                foreach (JToken ja in OptionFileList)
                {
                    string name = ja[0].ToString();
                    string url = ja[1].ToString();
                    string path = ja[4].ToString().TrimStart('/');

                    DicPath.Add(new Tuple<string, string>($"{resWebRoot}/{url}", $"{Path.Combine(App.Root, "Project QT", "asset", path, name)}"));
                }
            }

            int count = 0;
            int quantity = DicPath.Count;
            List<Task> tasks = new List<Task>();
            
            foreach (Tuple<string, string> pair in DicPath)
            {
                string url = pair.Item1;
                string path = pair.Item2;

                tasks.Add(DownLoadFile(url, path, cb_isCover.IsChecked == true ? true : false));
                count++;

                // 阻塞線程，等待現有工作完成再給新工作
                if ((count % pool).Equals(0) || quantity == count)
                {
                    // await is better than Task.Wait()
                    await Task.WhenAll(tasks);
                    tasks.Clear();
                }

                // 用await將線程讓給UI更新
                lb_counter.Content = $"Processed : {count} / {quantity}";
                await Task.Delay(1);
            }
            
            // 針對Android版本的包做解壓
            foreach (JToken ja in fileList1)
            {
                string path = ja[0].ToString();

                if (cb_devices.SelectedIndex == 1 && path.Contains(".zip"))
                {
                    string zipPath = $"{Path.Combine(App.Root, "Project QT", "asset", path)}";
                    string extractPath = $"{Path.Combine(App.Root, "Project QT", "asset", "tmp")}";
                    ZipFile.ExtractToDirectory(zipPath, extractPath);
                    File.Delete(zipPath);

                    // 先處理zip中的同名檔案，再處理覆蓋問題
                    string checkPath = $"{Path.Combine(App.Root, "Project QT", "asset", "tmp_check")}";
                    MoveDirectory(extractPath, checkPath, false, true);
                    string assetPath = $"{Path.Combine(App.Root, "Project QT", "asset")}";
                    MoveDirectory(checkPath, assetPath, cb_isCover.IsChecked == true ? true : false);
                }
            }

            if (cb_Debug.IsChecked == true)
            {
                using (StreamWriter outputFile = new StreamWriter("404.log", false))
                {
                    foreach (string s in App.log)
                        outputFile.WriteLine(s);
                }
            }

            System.Windows.MessageBox.Show($"Download finsh，total {App.glocount} files", "Finish");
            lb_counter.Content = String.Empty;
        }

        /// <summary>
        /// 從指定的網址下載檔案
        /// </summary>
        public async Task<Task> DownLoadFile(string downPath, string savePath, bool overWrite)
        {
            bool create = false;
            if (!Directory.Exists(Path.GetDirectoryName(savePath)))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                create = true;
            }

            if (File.Exists(savePath) && overWrite == false)
                return Task.FromResult(0);

            App.glocount++;

            using (WebClient wc = new WebClient())
            {
                try
                {
                    // Check if use web base api request
                    if (cb_devices.SelectedIndex == 0)
                    {
                        wc.Headers.Add("X-QOOKIA-DEVICE-TYPE", "web");
                    }
                    await wc.DownloadFileTaskAsync(downPath, savePath);
                }
                catch (Exception ex)
                {
                    App.glocount--;

                    if (create == true)
                        Directory.Delete(Path.GetDirectoryName(savePath));

                    if (cb_Debug.IsChecked == true)
                        App.log.Add(downPath + Environment.NewLine + savePath + Environment.NewLine);

                    // 沒有的資源直接跳過，避免報錯。
                    //System.Windows.MessageBox.Show(ex.Message.ToString() + Environment.NewLine + downPath + Environment.NewLine + savePath);
                }
            }
            return Task.FromResult(0);
        }

        /// <summary>
        /// 將secret.json中的資源請求出下載的Url位置 (需要Login且該帳號必須擁有該資源)
        /// </summary>
        public async Task<string> SecretPost(string UrlPath, string filename, string USER, string DIGEST)
        {
            string pet_id = String.Empty;
            string cg_id = String.Empty;
            try
            {
                //Test 1039_Event_CG5.unity3d, OK, return androidsecret/1039_Event_CG5_210518130201.unity3d
                pet_id = Regex.Match(filename, "[0-9]+_Event").Value;
                cg_id = Regex.Match(filename, "_CG[0-9]+").Value;
            }
            catch { }
            using (WebClient wc = new WebClient())
            {
                try
                {
                    // Check if use android
                    if (cb_devices.SelectedIndex == 1)
                    {
                        wc.Headers.Add("X-QOOKIA-DIGEST", DIGEST);
                        wc.Headers.Add("X-QOOKIA-USER", USER);
                        wc.Headers.Add("X-QOOKIA-SERVER-PREFIX", "WVG");
                        // 12 0 E 12 15 ("pet_id", petId);("cg_id", cgId);{0}_Event_CG{1} 12 16 ("pet_id", petId.First(null));PetBG{0}. 40 0 E 41 0 ("event_id", eventId);Event_{0}/{("tier", level); 48 0 ("event_id", eventId);("pose_id", chapterId);SexualDating_{0}_{1}
                        System.Collections.Specialized.NameValueCollection myNameValueCollection = new System.Collections.Specialized.NameValueCollection();
                        myNameValueCollection.Add("asset_name", filename);
                        myNameValueCollection.Add("id", "12");
                        myNameValueCollection.Add("sub_id", "15");
                        myNameValueCollection.Add("param", "{" + $"\"pet_id\":\"{pet_id}\",\"cg_id\":\"{cg_id}\"" + "}");
                        /*
                        myNameValueCollection.Add("id", "12");
                        myNameValueCollection.Add("sub_id", "0");
                        myNameValueCollection.Add("param", "");
                        
                        myNameValueCollection.Add("id", "40");
                        myNameValueCollection.Add("sub_id", "0");
                        myNameValueCollection.Add("param", "");
                        
                        myNameValueCollection.Add("id", "41");
                        myNameValueCollection.Add("sub_id", "0");
                        myNameValueCollection.Add("param", "{\"event_id\":\"5\",\"tier\":\"30\"}");
                        
                        myNameValueCollection.Add("id", "48");
                        myNameValueCollection.Add("sub_id", "0");
                        myNameValueCollection.Add("param", "{\"event_id\":\"\",\"pose_id\":\"\"}");
                        */
                        byte[] result = await wc.UploadValuesTaskAsync(UrlPath, myNameValueCollection);
                        string resultJson = System.Text.Encoding.UTF8.GetString(result);
                        //System.Windows.MessageBox.Show(resultJson);
                        return resultJson;
                    }
                }
                catch (Exception ex)
                {
                    // 沒有的資源直接跳過，避免報錯。
                    //System.Windows.MessageBox.Show(ex.Message.ToString() + Environment.NewLine + downPath + Environment.NewLine + savePath);
                }
            }
            return String.Empty;
        }

        /// <summary>
        /// 移動資料夾
        /// </summary>
        /// <param name="source">來源資料夾</param>
        /// <param name="target">目的資料夾</param>
        /// <param name="overwriteFiles">是否覆寫</param>
        /// <param name="spec">自動重命名 (For Project QT Only)</param>
        public static void MoveDirectory(string source, string target, bool overwriteFiles, bool spec = false)
        {
            string sourcePath = source.TrimEnd('\\', ' ');
            string targetPath = target.TrimEnd('\\', ' ');
            if (Directory.Exists(sourcePath))
            {
                var files = Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories).GroupBy(s => Path.GetDirectoryName(s));
                foreach (var folder in files)
                {
                    string targetFolder = folder.Key.Replace(sourcePath, targetPath);
                    if (!Directory.Exists(targetFolder))
                        Directory.CreateDirectory(targetFolder);
                    foreach (string file in folder)
                    {
                        string targetFile = Path.Combine(targetFolder, Path.GetFileName(file));

                        if (File.Exists(targetFile))
                        {
                            if (overwriteFiles == true)
                            {
                                File.Delete(targetFile);
                                File.Move(file, targetFile);
                            }
                            else if (spec == true)
                            {
                                // 如果解壓多個包時內容有重複就自動重新命名 (此為特化功能，僅用於這個專案)
                                for (int i = 1; i < 100; i++)
                                {
                                    targetFile = Path.Combine(targetFolder, $"{Path.GetFileNameWithoutExtension(file)}_{i}");
                                    if (!File.Exists(targetFile)) break;
                                }
                                File.Move(file, targetFile);
                            }
                        }
                        else
                        {
                            File.Move(file, targetFile);
                        }
                    }
                }
                Directory.Delete(source, true);
            }
        }

        private void btn_qtx2png_Click(object sender, RoutedEventArgs e)
        {
            string selectPath = String.Empty;

            OpenFolderDialog openFolderDialog = new OpenFolderDialog();
            while (true)
            {
                var result = System.Windows.MessageBox.Show("The selected folder should be a child folder in the program execution directory", "Notice", MessageBoxButton.OKCancel);

                if (result == MessageBoxResult.Cancel)
                    return;

                openFolderDialog.InitialFolder = App.Root;
                if (openFolderDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    selectPath = openFolderDialog.Folder;
                    if (Directory.Exists(selectPath) && selectPath.Contains(App.Root))
                        break;
                }
            }

            List<string> fileList = Directory.GetFiles(selectPath, "*.qtx",  SearchOption.AllDirectories).ToList();

            foreach (string file in fileList)
            {
                string fileName = Path.GetFileNameWithoutExtension(file);
                string filefolder = Path.GetDirectoryName(file);
                string filepath = Path.Combine(filefolder, fileName);

                byte[] bytes = File.ReadAllBytes(file);
                char[] array = new char[16];
                for (int i = 0; i < 16; i++)
                {
                    array[i] = (char)bytes[i];
                }
                int num = 12;
                byte[] array2 = new byte[num];
                for (int j = 0; j < num; j++)
                {
                    array2[j] = bytes[j + 16];
                }
                qtxFile.Decrypt(ref array2, 0, num, array);
                if (qtxFile.IsKTXFile(array2, 0))
                {
                    int nSizeToDecrypt = 64;
                    qtxFile.Decrypt(ref bytes, 16, nSizeToDecrypt, array);
                    File.WriteAllBytes($"{filepath}.ktx", qtxFile.GetKTXFile(bytes, 16));
                    qtxFile.KTX2PNG($"{filepath}.ktx");
                    File.Delete(file);
                    File.Delete($"{filepath}.ktx");
                }
                else
                {
                    int nSizeToDecrypt = 128;
                    qtxFile.Decrypt(ref bytes, 16, nSizeToDecrypt, array);
                    File.WriteAllBytes($"{filepath}.png", qtxFile.GetKTXFile(bytes, 16));
                }
            }
            lb_counter_qtx.Content = $"Converted {fileList.Count} files";
        }
    }
}
