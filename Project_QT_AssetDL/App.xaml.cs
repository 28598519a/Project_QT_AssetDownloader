using System;
using System.Collections.Generic;
using System.Windows;

namespace Project_QT_AssetDL
{
    /// <summary>
    /// App.xaml 的互動邏輯
    /// </summary>
    public partial class App : Application
    {
        public static string Root = Environment.CurrentDirectory;
        public static int glocount = 0;
        public static List<string> log = new List<string>();
    }
}
