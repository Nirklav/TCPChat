using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace UI.Dialogs
{
    /// <summary>
    /// Логика взаимодействия для CreateRoomDialog.xaml
    /// </summary>
    public partial class CreateRoomDialog : Window
    {
        public string RoomName { get; set; }

        public CreateRoomDialog()
        {
            InitializeComponent();
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            RoomName = RoomNameTextBox.Text;

            if (string.IsNullOrEmpty(RoomName))
            {
                MessageBox.Show(this, "Проверьте правильность заполнения всех полей.", "TCP Chat");
                return;
            }

            DialogResult = true;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
