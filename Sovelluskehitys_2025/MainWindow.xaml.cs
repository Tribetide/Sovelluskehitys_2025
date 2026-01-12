using ControlzEx.Theming;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Data.Sqlite;
using System;
using System.Data;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Sovelluskehitys_2025.Data;

namespace Sovelluskehitys_2025
{
    public partial class MainWindow : MetroWindow
    {
        private SqliteConnection yhteys = null!;
        private AppService appService = null!;

        public MainWindow()
        {
            InitializeComponent();

            ThemeManager.Current.ChangeTheme(this, "Light.Blue");
        }

        private static void BindDataGrid(DataTable table, DataGrid grid)
        {
            grid.ItemsSource = table.DefaultView;
        }

        private static void BindComboBox(DataTable table, ComboBox combo)
        {
            combo.ItemsSource = table.DefaultView;
            combo.DisplayMemberPath = "nimi";
            combo.SelectedValuePath = "id";
        }

        private void Paivita_Tuotelista()
        {
            BindDataGrid(appService.GetProducts(), tuotelista);
        }

        private void Paivita_Asiakaslista()
        {
            BindDataGrid(appService.GetCustomers(), asiakaslista);
        }

        private void Paivita_Kategorialista()
        {
            BindDataGrid(appService.GetCategories(), kategorialista);
        }

        private void Paivita_TuoteCombos()
        {
            BindComboBox(appService.GetProductOptions(), cb_tuotelista);
            BindComboBox(appService.GetProductOptions(), cb_tuote_tilaus);
        }

        private void Paivita_AsiakasCombo()
        {
            BindComboBox(appService.GetCustomerOptions(), cb_asiakas_tilaus);
        }

        private void Paivita_KategoriaCombos()
        {
            var kategoriat = appService.GetCategoryOptions();
            BindComboBox(kategoriat, cb_kategoria_poisto);

            var valinta = kategoriat.Copy();
            var emptyRow = valinta.NewRow();
            emptyRow["id"] = DBNull.Value;
            emptyRow["nimi"] = "Ei kategoriaa";
            valinta.Rows.InsertAt(emptyRow, 0);
            var viewSource = (CollectionViewSource)FindResource("KategoriaOptions");
            viewSource.Source = valinta;
            BindComboBox(valinta, cb_kategoria_tuote);
            cb_kategoria_tuote.SelectedIndex = 0;
        }

        private long? GetSelectedCategoryId()
        {
            if (cb_kategoria_tuote.SelectedValue == null || cb_kategoria_tuote.SelectedValue is DBNull)
                return null;

            return Convert.ToInt64(cb_kategoria_tuote.SelectedValue);
        }

        private void Paivita_Tilauslistat()
        {
            BindDataGrid(appService.GetOpenOrders(), tilauslista);
        }

        private void Paivita_Toimitetutlistat()
        {
            BindDataGrid(appService.GetDeliveredOrders(), toimitetut_lista);
        }

        private void Paivita_Tilausnakyma()
        {
            Paivita_Tilauslistat();
            Paivita_Toimitetutlistat();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (tekstikentta_1.Text == "" || tekstikentta_2.Text == "" || tekstikentta_3.Text == "")
            {
                MessageBox.Show("Täytä kaikki kentät ennen tallennusta.");
                return;
            }

            if (!decimal.TryParse(tekstikentta_2.Text, NumberStyles.Number, CultureInfo.GetCultureInfo("fi-FI"), out var hinta))
            {
                MessageBox.Show("Hinta ei ole kelvollinen numero.");
                return;
            }
            if (!int.TryParse(tekstikentta_3.Text, out var saldo) || saldo < 0)
            {
                MessageBox.Show("Varastosaldo ei ole kelvollinen.");
                return;
            }

            long? kategoriaId = GetSelectedCategoryId();
            appService.AddProduct(tekstikentta_1.Text, hinta, saldo, kategoriaId);

            Paivita_Tuotelista();
            Paivita_TuoteCombos();

            tekstikentta_1.Clear();
            tekstikentta_2.Clear();
            tekstikentta_3.Clear();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Paivita_Tuotelista();
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (cb_tuotelista.SelectedValue == null)
            {
                MessageBox.Show("Valitse poistettava tuote ensin.");
                return;
            }

            var vahvistus = MessageBox.Show(
                "Haluatko varmasti poistaa tuotteen?",
                "Vahvista poisto",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (vahvistus != MessageBoxResult.Yes)
                return;

            long id = (long)cb_tuotelista.SelectedValue;

            try
            {
                appService.DeleteProduct(id);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Tuotteen poisto epäonnistui: " + ex.Message);
                return;
            }

            Paivita_Tuotelista();
            Paivita_TuoteCombos();
        }

        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Check päällä");
            asiakkaat_tab.IsEnabled = true;
        }

        private void valinta_boksi_Unchecked(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Check pois");
            asiakkaat_tab.IsEnabled = false;
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            if (asiakas_nimi.Text == "" || asiakas_osoite.Text == "" || asiakas_puhelin.Text == "")
            {
                MessageBox.Show("Täytä kaikki kentät ennen tallennusta.");
                return;
            }

            appService.AddCustomer(asiakas_nimi.Text, asiakas_osoite.Text, asiakas_puhelin.Text);

            Paivita_Asiakaslista();
            Paivita_AsiakasCombo();

            asiakas_nimi.Clear();
            asiakas_osoite.Clear();
            asiakas_puhelin.Clear();
        }

        private void Lisaa_Kategoria_Click(object sender, RoutedEventArgs e)
        {
            if (kategoria_nimi.Text == "")
            {
                MessageBox.Show("Täytä nimi ennen tallennusta.");
                return;
            }

            string? kuvaus = string.IsNullOrWhiteSpace(kategoria_kuvaus.Text) ? null : kategoria_kuvaus.Text;
            appService.AddCategory(kategoria_nimi.Text, kuvaus);

            Paivita_Kategorialista();
            Paivita_KategoriaCombos();
            Paivita_Tuotelista();

            kategoria_nimi.Clear();
            kategoria_kuvaus.Clear();
        }

        private void Poista_Kategoria_Click(object sender, RoutedEventArgs e)
        {
            if (cb_kategoria_poisto.SelectedValue == null)
            {
                MessageBox.Show("Valitse poistettava kategoria ensin.");
                return;
            }

            var vahvistus = MessageBox.Show(
                "Haluatko varmasti poistaa kategorian?",
                "Vahvista poisto",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (vahvistus != MessageBoxResult.Yes)
                return;

            long id = Convert.ToInt64(cb_kategoria_poisto.SelectedValue);
            try
            {
                appService.DeleteCategory(id);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Kategorian poisto epäonnistui: " + ex.Message);
                return;
            }

            Paivita_Kategorialista();
            Paivita_KategoriaCombos();
            Paivita_Tuotelista();
        }

        private void Avaa_Menu_Click(object sender, RoutedEventArgs e)
        {

            var dialog = new Microsoft.Win32.OpenFileDialog();
            dialog.DefaultExt = ".cpp";
            dialog.Filter = "C++ Files (*.cpp)|*.cpp|All Files (*.*)|*.*";
            bool? result = dialog.ShowDialog();
            if (result == true)
            {
                string filename = dialog.FileName;
                this.ShowModalMessageExternal("Valittu tiedosto", filename);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                DbInit.EnsureDatabase();
                yhteys = DbInit.OpenConnection();
                appService = new AppService(yhteys);
                tilaviesti.Text = "Tietokantayhteys avattu onnistuneesti.";

                Paivita_Kategorialista();
                Paivita_Tuotelista();
                Paivita_Asiakaslista();

                Paivita_Tilausnakyma();

                Paivita_TuoteCombos();
                Paivita_AsiakasCombo();
                Paivita_KategoriaCombos();
            }
            catch (Exception ex)
            {
                tilaviesti.Text = "Tietokantayhteyden avaus epäonnistui. " + ex.Message;
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            yhteys?.Close();
            yhteys?.Dispose();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        private void tilaukset_tab_GotFocus(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click_4(object sender, RoutedEventArgs e)
        {
            if (cb_tuote_tilaus.SelectedValue == null || cb_asiakas_tilaus.SelectedValue == null)
            {
                MessageBox.Show("Valitse tuote ja asiakas ensin.");
                return;
            }

            long tuoteId = (long)cb_tuote_tilaus.SelectedValue;
            long asiakasId = (long)cb_asiakas_tilaus.SelectedValue;

            if (!int.TryParse(tb_tilausMaara.Text, out int maara) || maara <= 0)
            {
                MessageBox.Show("Anna kelvollinen määrä (positiivinen kokonaisluku).");
                return;
            }

            try
            {
                appService.CreateOrder(asiakasId, tuoteId, maara);
                Paivita_Tilausnakyma();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Tilauksen tallennus epäonnistui: " + ex.Message);
            }
        }

        private void Toimita_Tilaus_Click(object sender, RoutedEventArgs e)
        {
            DataRowView rivi = (DataRowView)((Button)e.Source).DataContext;
            long tilausId = Convert.ToInt64(rivi["id"]);

            try
            {
                appService.SetDelivered(tilausId, true);
                Paivita_Tilausnakyma();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Tilauksen toimitus epäonnistui: " + ex.Message);
            }
        }

        private void Peruuta_Tilaus_Click(object sender, RoutedEventArgs e)
        {
            DataRowView rivi = (DataRowView)((Button)e.Source).DataContext;
            long tilausId = Convert.ToInt64(rivi["id"]);

            try
            {
                appService.SetDelivered(tilausId, false);
                Paivita_Tilausnakyma();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Tilauksen peruutus epäonnistui: " + ex.Message);
            }
        }

        private void Poista_Tilaus_Click(object sender, RoutedEventArgs e)
        {
            DataRowView rivi = (DataRowView)((Button)e.Source).DataContext;
            long tilausId = Convert.ToInt64(rivi["id"]);

            var vahvistus = MessageBox.Show(
                "Haluatko varmasti poistaa tilauksen?",
                "Vahvista poisto",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);
            if (vahvistus != MessageBoxResult.Yes)
                return;

            try
            {
                appService.DeleteOrder(tilausId);
                Paivita_Tilausnakyma();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Tilauksen poisto epäonnistui: " + ex.Message);
            }
        }

        private void Tilauslista_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit)
                return;

            if (e.Row.Item is not DataRowView rivi)
                return;

            Dispatcher.BeginInvoke(new Action(() => Paivita_TilausMaara(rivi)), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void Paivita_TilausMaara(DataRowView rivi)
        {
            if (rivi.Row.RowState != DataRowState.Modified)
                return;

            int uusiMaara = Convert.ToInt32(rivi["maara"]);
            int vanhaMaara = Convert.ToInt32(rivi.Row["maara", DataRowVersion.Original]);

            if (uusiMaara <= 0)
            {
                MessageBox.Show("Anna kelvollinen määrä (positiivinen kokonaisluku).");
                Paivita_Tilausnakyma();
                return;
            }

            long riviId = Convert.ToInt64(rivi["rivi_id"]);
            long tuoteId = Convert.ToInt64(rivi["tuote_id"]);

            try
            {
                appService.UpdateOrderQuantity(riviId, tuoteId, uusiMaara, vanhaMaara);
                rivi.Row.AcceptChanges();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Tilauksen määrän päivitys epäonnistui: " + ex.Message);
                Paivita_Tilausnakyma();
            }
        }

        private void Tuotelista_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit)
                return;

            if (e.Row.Item is not DataRowView rivi)
                return;

            Dispatcher.BeginInvoke(new Action(() => Paivita_TuoteKategoria(rivi)), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void Paivita_TuoteKategoria(DataRowView rivi)
        {
            if (rivi.Row.RowState != DataRowState.Modified)
                return;

            long tuoteId = Convert.ToInt64(rivi["id"]);
            long? kategoriaId = null;
            if (!rivi.Row.IsNull("kategoria_id"))
                kategoriaId = Convert.ToInt64(rivi["kategoria_id"]);

            try
            {
                appService.UpdateProductCategory(tuoteId, kategoriaId);
                rivi.Row.AcceptChanges();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Tuotteen kategorian päivitys epäonnistui: " + ex.Message);
                Paivita_Tuotelista();
            }
        }

        private void Tuotelista_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (string.Equals(e.PropertyName, "varastosaldo", StringComparison.OrdinalIgnoreCase))
                e.Column.IsReadOnly = true;
        }
    }
}
