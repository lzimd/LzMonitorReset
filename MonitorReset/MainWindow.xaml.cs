using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

namespace MonitorReset
{
    public partial class MainWindow : Window
    {
        private double OriginX;
        private double OriginY;
        private int Scale;

        private List<string> ProcRegisterNames = new();

        public MainWindow()
        {
            InitializeComponent();
            canvas_main.Loaded += (s, e) =>
            {
                CanvasFlush();
                ListWindowFlush();
            };
        }

        private void Button_Ignore_Click(object sender, RoutedEventArgs e)
        {
            if (this.list_process.SelectedItem is ListBoxItem item)
            {
                var dat = item.DataContext as LibMR.Proc.ProcWindow;
                string currName = $"window_{dat.Hwnd}";
                if (this.canvas_main.FindName("can" + currName) is Rectangle canvItem)
                {
                    Panel.SetZIndex(canvItem, -5);
                    canvItem.Stroke = new SolidColorBrush(Colors.Gray);
                    canvItem.Fill = null;
                }
            }

            if (this.canvas_main.Resources.Contains("focus_item"))
            {
                this.canvas_main.Resources.Remove("focus_item");
            }
        }

        private void CanvasPreset()
        {
            canvas_main.Children.Clear();
            foreach (var name in ProcRegisterNames)
            {
                canvas_main.UnregisterName("can" + name);
                list_process.UnregisterName("lst" + name);
            }
            ProcRegisterNames.Clear();
            canvas_main.Resources.Remove("focus_item");

            Button btnFlush = new();
            btnFlush.Content = "刷新";
            btnFlush.Click += (s, e) =>
            {
                CanvasFlush();
                ListWindowFlush();
            };
            Canvas.SetLeft(btnFlush, 10);
            Canvas.SetTop(btnFlush, 10);

            Button btnReset = new();
            btnReset.Content = "移至主显";
            btnReset.Click += (s, e) =>
            {
                if (this.list_process.SelectedItem is ListBoxItem item)
                {
                    LibMR.Gdi.MoveWindowToPrimary(item.DataContext as LibMR.Proc.ProcWindow);
                }
            };
            Canvas.SetLeft(btnReset, 10);
            Canvas.SetTop(btnReset, 40);

            Button btnIgnore = new();
            btnIgnore.Click += Button_Ignore_Click;
            btnIgnore.Content = "暂时忽略";
            Canvas.SetLeft(btnIgnore, 10);
            Canvas.SetTop(btnIgnore, 70);

            canvas_main.Children.Add(btnFlush);
            canvas_main.Children.Add(btnIgnore);
            canvas_main.Children.Add(btnReset);
        }

        private void Proc_Focus(LibMR.Proc.ProcWindow procWindow, string channel)
        {
            // text
            this.text_info.FontFamily = new("Global Monospace");
            this.text_info.Text = $"window  : [ 0x{procWindow.Hwnd:X8} ]\n"
                                + $"process : [ {procWindow.ProcId,5} ]\n"
                                + $"thread  : [ {procWindow.ThreadId,5} ]\n\n"
                                + $"top     : {procWindow.Window.top,5}\n"
                                + $"bottom  : {procWindow.Window.bottom,5}\n\n"
                                + $"left    : {procWindow.Window.left,5}\n"
                                + $"right   : {procWindow.Window.right,5}\n";

            string currName = $"window_{procWindow.Hwnd}";

            if (channel != "listbox")
            {
                // listbox
                if (this.list_process.SelectedItem is ListBoxItem prevListItem)
                {
                    prevListItem.IsSelected = false;
                }
                if (this.list_process.FindName("lst" + currName) is ListBoxItem currListItem)
                {
                    currListItem.IsSelected = true;
                }
            }

            if (channel != "canvas")
            {
                // canvas
                if (this.canvas_main.TryFindResource("focus_item") is Rectangle prevCanvItem)
                {
                    Panel.SetZIndex(prevCanvItem, 10);
                    prevCanvItem.Stroke = new SolidColorBrush(Colors.Green);
                }

                if (this.canvas_main.FindName("can" + currName) is Rectangle canvItem)
                {
                    Panel.SetZIndex(canvItem, 99);
                    canvItem.Stroke = new SolidColorBrush(Colors.Red);

                    if (this.canvas_main.Resources.Contains("focus_item"))
                    {
                        this.canvas_main.Resources.Remove("focus_item");
                    }
                    this.canvas_main.Resources.Add("focus_item", canvItem);
                }
            }

        }

        private void CanvasFlush()
        {
            CanvasPreset();

            Canvas canv = this.canvas_main;
            canv.Margin = new Thickness(5, 5, 5, 5);
            canv.Background = new SolidColorBrush(Colors.LightGray);

            var virtualScreen = LibMR.Gdi.GetVirtualScreen();
            var screenMode = virtualScreen.Width < virtualScreen.Height;

            {
                Scale = screenMode
                    ? (int)(virtualScreen.Width / (canv.ActualWidth / 3 * 2))
                    : (int)(virtualScreen.Height / (canv.ActualHeight / 3 * 2));

                var rWidth = virtualScreen.Width / Scale;
                var rHeight = virtualScreen.Height / Scale;

                OriginX = (canv.ActualWidth - rWidth) / 2;
                OriginY = (canv.ActualHeight - rHeight) / 2;

                Rectangle r = new();
                r.Stroke = new SolidColorBrush(Colors.Gray);
                r.Width = rWidth;
                r.Height = rHeight;

                Canvas.SetLeft(r, OriginX);
                Canvas.SetTop(r, OriginY);
                Panel.SetZIndex(r, 1);
                canv.Children.Add(r);
            }

            var displays = LibMR.Gdi.GetDisplays();
            {
                // handle multi monitors
                // ; OriginX, OriginY
                int minX = virtualScreen.Width, minY = virtualScreen.Height;
                foreach (var display in displays)
                {
                    minX = Math.Min(minX, display.Monitor.left);
                    minY = Math.Min(minY, display.Monitor.top);
                }
                OriginX = OriginX - minX / Scale;
                OriginY = OriginY - minY / Scale;
            }



            foreach (var display in displays)
            {
                Rectangle r = new();
                r.Stroke = new SolidColorBrush(Colors.Gray);
                r.Width = (display.Monitor.right - display.Monitor.left) / Scale;
                r.Height = (display.Monitor.bottom - display.Monitor.top) / Scale;

                Canvas.SetLeft(r, OriginX + display.Monitor.left / Scale);
                Canvas.SetTop(r, OriginY + display.Monitor.top / Scale);
                Panel.SetZIndex(r, 2);

                canv.Children.Add(r);
            }

        }

        private void ListWindowFlush()
        {
            this.list_process.Items.Clear();

            var procs = LibMR.Proc.GetProcWindowCollection();

            foreach (var proc in procs)
            {
                string resName = string.Format("window_{0}", proc.Hwnd);

                ListBoxItem item = new();
                item.Name = resName;
                item.Content = string.Format("[ {0,5} ] {1}", proc.ProcId, proc.Name);
                item.FontFamily = new("monospace");
                item.DataContext = proc;
                item.Selected += (sender, _) =>
                {
                    var item = sender as ListBoxItem;
                    Proc_Focus(item.DataContext as LibMR.Proc.ProcWindow, null);
                };

                Rectangle r = new();
                r.Name = resName;
                r.DataContext = proc;
                r.Stroke = new SolidColorBrush(Colors.Green);
                r.Width = (proc.Window.right - proc.Window.left) / Scale;
                r.Height = (proc.Window.bottom - proc.Window.top) / Scale;
                r.MouseDown += (sender, _) =>
                {
                    var item = sender as Rectangle;
                    Proc_Focus(item.DataContext as LibMR.Proc.ProcWindow, "canvas");
                };
                r.Fill = new SolidColorBrush(Colors.LightBlue);

                Canvas.SetLeft(r, OriginX + proc.Window.left / Scale);
                Canvas.SetTop(r, OriginY + proc.Window.top / Scale);
                Panel.SetZIndex(r, 10);

                this.canvas_main.RegisterName("can" + resName, r);
                this.list_process.RegisterName("lst" + resName, item);

                this.list_process.Items.Add(item);
                this.canvas_main.Children.Add(r);

                ProcRegisterNames.Add(resName);
            }
        }
    }
}
