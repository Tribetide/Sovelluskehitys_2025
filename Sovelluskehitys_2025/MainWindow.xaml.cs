using ControlzEx.Theming;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
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
using Sovelluskehitys_2025.Models;

namespace Sovelluskehitys_2025
{
    // Pääikkunan käyttöliittymälogiikka ja tapahtumakäsittelijät.
    public partial class MainWindow : MetroWindow
    {
        // Tietokantayhteys ja palvelukerros käyttöliittymätoiminnoille.
        private SqliteConnection yhteys = null!;
        private AppService appService = null!;
        // Muistinvarainen ostoskori nykyiselle tilaukselle (Tilaukset-välilehti).
        private readonly DataTable ostoskori = new DataTable("ostoskori");

        public MainWindow()
        {
            InitializeComponent();

            ThemeManager.Current.ChangeTheme(this, "Light.Purple");
            Alusta_Ostoskori();
        }

        // DataTable -> käyttöliittymä -apumetodit.
        private static void BindDataGrid(DataTable table, DataGrid grid)
        {
            grid.ItemsSource = table.DefaultView;
        }

        // ComboBox-sidonnan apu (id/nimi).
        private static void BindComboBox(DataTable table, ComboBox combo)
        {
            combo.ItemsSource = table.DefaultView;
            combo.DisplayMemberPath = "nimi";
            combo.SelectedValuePath = "id";
        }

        // Käyttöliittymän päivitysmetodit.
        private void Paivita_Tuotelista()
        {
            BindDataGrid(appService.GetProducts(), tuotelista);
        }

        private void Paivita_TopTuotteet()
        {
            if (FindName("top_tuotteet_lista") is DataGrid grid)
                BindDataGrid(appService.GetTopProducts(3), grid);
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

            // Lisää "Ei kategoriaa" -vaihtoehto tuote-Comboboxiin.
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
            // Tulkitse tyhjä valinta nulliksi.
            if (cb_kategoria_tuote.SelectedValue == null || cb_kategoria_tuote.SelectedValue is DBNull)
                return null;

            return Convert.ToInt64(cb_kategoria_tuote.SelectedValue);
        }

        private void Paivita_Tilauslistat()
        {
            tilauslista.ItemsSource = appService.GetOpenOrdersHierarchical();
        }

        private void Paivita_Toimitetutlistat()
        {
            toimitetut_lista.ItemsSource = appService.GetDeliveredOrdersHierarchical();
        }

        // Päivitä kaikki tilausnäkymät (avoimet, toimitetut, raportti).
        private void Paivita_Tilausnakyma()
        {
            Paivita_Tilauslistat();
            Paivita_Toimitetutlistat();
            Paivita_TopTuotteet();
        }

        // Ostoskorin tila ja näkymäsidonnat.
        private void Alusta_Ostoskori()
        {
            if (ostoskori.Columns.Count == 0)
            {
                ostoskori.Columns.Add("tuote_id", typeof(long));
                ostoskori.Columns.Add("tuote", typeof(string));
                ostoskori.Columns.Add("maara", typeof(int));
                ostoskori.Columns.Add("yksikkohinta", typeof(decimal));
                ostoskori.Columns.Add("hinta", typeof(decimal));
            }

            if (FindName("ostoskori_lista") is DataGrid grid)
                grid.ItemsSource = ostoskori.DefaultView;
        }

        private void Tyhjenna_Ostoskori()
        {
            // Tyhjennä rivit ja salli asiakkaan valinta uudelleen.
            ostoskori.Rows.Clear();
            Paivita_OstoskoriTila();
        }

        private void Paivita_OstoskoriTila()
        {
            // Estä asiakkaan vaihto, kun rivejä on lisätty.
            if (cb_asiakas_tilaus != null)
                cb_asiakas_tilaus.IsEnabled = ostoskori.Rows.Count == 0;
        }

        // Tuotteet: lisäys (Lisää/poista-välilehti).
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string nimi = tekstikentta_1.Text.Trim();
            string hintaTeksti = tekstikentta_2.Text.Trim();
            string saldoTeksti = tekstikentta_3.Text.Trim();

            if (nimi == "" || hintaTeksti == "" || saldoTeksti == "")
            {
                MessageBox.Show("Täytä kaikki kentät ennen tallennusta.");
                return;
            }

            if (!decimal.TryParse(hintaTeksti, NumberStyles.Number, CultureInfo.GetCultureInfo("fi-FI"), out var hinta))
            {
                MessageBox.Show("Hinta ei ole kelvollinen numero.");
                return;
            }
            if (!int.TryParse(saldoTeksti, out var saldo) || saldo < 0)
            {
                MessageBox.Show("Varastosaldo ei ole kelvollinen.");
                return;
            }

            long? kategoriaId = GetSelectedCategoryId();
            try
            {
                appService.AddProduct(nimi, hinta, saldo, kategoriaId);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Tuotteen lisäys epäonnistui: " + ex.Message);
                return;
            }

            Paivita_Tuotelista();
            Paivita_TuoteCombos();

            tekstikentta_1.Clear();
            tekstikentta_2.Clear();
            tekstikentta_3.Clear();
        }

        // Tuotteet: hae tiedot -nappi.
        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Paivita_Tuotelista();
        }

        // Tuotteet: varastosaldon lisäys (sallittu ilman ylläpitäjää).
        private void Lisaa_Saldo_Click(object sender, RoutedEventArgs e)
        {
            if (tuotelista.SelectedItem is not DataRowView rivi)
            {
                MessageBox.Show("Valitse tuote listasta ensin.");
                return;
            }

            if (!int.TryParse(tb_saldo_lisays.Text.Trim(), out int lisays) || lisays <= 0)
            {
                MessageBox.Show("Anna lisättävä saldo (positiivinen kokonaisluku).");
                return;
            }

            long tuoteId = Convert.ToInt64(rivi["id"]);
            try
            {
                appService.AddStock(tuoteId, lisays);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Varastosaldon lisäys epäonnistui: " + ex.Message);
                return;
            }

            Paivita_Tuotelista();
            Paivita_TuoteCombos();
            tb_saldo_lisays.Clear();
        }

        // Tuotteet: poisto (ylläpitäjä).
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

        // Ylläpitäjätilan vaihto: lukitsee/avaa muokkaustoiminnot.
        private void CheckBox_Checked(object sender, RoutedEventArgs e)
        {
            MessageBox.Show(
                "Admin-oikeudet päällä.\nVoit lisätä/poistaa tuotteita ja kategorioita sekä muokata tuotteiden nimeä, hintaa ja kategoriaa.");
            Paivita_AdminOikeudet();
        }

        private void valinta_boksi_Unchecked(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Admin-oikeudet pois päältä.");
            Paivita_AdminOikeudet();
        }

        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void Paivita_AdminOikeudet()
        {
            bool isAdmin = valinta_boksi.IsChecked == true;

            asiakkaat_tab.IsEnabled = true;
        // Tuotelistan muokkaus on vain ylläpitäjälle.
            tuotelista.IsReadOnly = !isAdmin;

            tekstikentta_1.IsEnabled = isAdmin;
            tekstikentta_2.IsEnabled = isAdmin;
            tekstikentta_3.IsEnabled = isAdmin;
            cb_kategoria_tuote.IsEnabled = isAdmin;
            btn_lisaa_tuote.IsEnabled = isAdmin;
            btn_poista_tuote.IsEnabled = isAdmin;
            kategoria_nimi.IsEnabled = isAdmin;
            kategoria_kuvaus.IsEnabled = isAdmin;
            cb_kategoria_poisto.IsEnabled = isAdmin;
            btn_lisaa_kategoria.IsEnabled = isAdmin;
            btn_poista_kategoria.IsEnabled = isAdmin;

            asiakas_nimi.IsEnabled = true;
            asiakas_osoite.IsEnabled = true;
            asiakas_puhelin.IsEnabled = true;
        }

        // Asiakkaat: lisäys.
        private void Button_Click_3(object sender, RoutedEventArgs e)
        {
            string nimi = asiakas_nimi.Text.Trim();
            string osoite = asiakas_osoite.Text.Trim();
            string puhelin = asiakas_puhelin.Text.Trim();

            if (nimi == "" || osoite == "" || puhelin == "")
            {
                MessageBox.Show("Täytä kaikki kentät ennen tallennusta.");
                return;
            }

            try
            {
                appService.AddCustomer(nimi, osoite, puhelin);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Asiakkaan lisäys epäonnistui: " + ex.Message);
                return;
            }

            Paivita_Asiakaslista();
            Paivita_AsiakasCombo();

            asiakas_nimi.Clear();
            asiakas_osoite.Clear();
            asiakas_puhelin.Clear();
        }

        // Kategoriat: lisäys.
        private void Lisaa_Kategoria_Click(object sender, RoutedEventArgs e)
        {
            string nimi = kategoria_nimi.Text.Trim();
            string? kuvaus = string.IsNullOrWhiteSpace(kategoria_kuvaus.Text) ? null : kategoria_kuvaus.Text.Trim();

            if (nimi == "")
            {
                MessageBox.Show("Täytä nimi ennen tallennusta.");
                return;
            }

            try
            {
                appService.AddCategory(nimi, kuvaus);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Kategorian lisäys epäonnistui: " + ex.Message);
                return;
            }

            Paivita_Kategorialista();
            Paivita_KategoriaCombos();
            Paivita_Tuotelista();

            kategoria_nimi.Clear();
            kategoria_kuvaus.Clear();
        }

        // Kategoriat: poisto.
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

        // Valikko: yksinkertainen tiedoston avausdemo (näyttää polun).
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

        // Sovellus käynnistyy: avaa tietokantayhteys ja päivitä kaikki listat.
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                DbInit.EnsureDatabase();
                yhteys = DbInit.OpenConnection();
                appService = new AppService(yhteys);
                tilaviesti.Text = "Tietokantayhteys avattu onnistuneesti.";

                Alusta_Ostoskori();
                Paivita_Kategorialista();
                Paivita_Tuotelista();
                Paivita_Asiakaslista();

                Paivita_Tilausnakyma();

                Paivita_TuoteCombos();
                Paivita_AsiakasCombo();
                Paivita_KategoriaCombos();
                Paivita_OstoskoriTila();
                Paivita_TopTuotteet();
                Paivita_AdminOikeudet();
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

        // Tilaukset: lisää rivi ostoskoriin.
        private void Lisaa_Rivi_Click(object sender, RoutedEventArgs e)
        {
            if (cb_tuote_tilaus.SelectedValue == null)
            {
                MessageBox.Show("Valitse tuote ensin.");
                return;
            }

            long tuoteId = (long)cb_tuote_tilaus.SelectedValue;

            if (!int.TryParse(tb_tilausMaara.Text, out int maara) || maara <= 0)
            {
                MessageBox.Show("Anna kelvollinen määrä (positiivinen kokonaisluku).");
                return;
            }

            string tuoteNimi = "";
            decimal yksikkohinta = 0m;
            if (cb_tuote_tilaus.SelectedItem is DataRowView tuoteRow)
            {
                tuoteNimi = Convert.ToString(tuoteRow["nimi"]) ?? "";
                if (tuoteRow.Row.Table.Columns.Contains("hinta"))
                    yksikkohinta = Convert.ToDecimal(tuoteRow["hinta"]);
            }

            foreach (DataRow row in ostoskori.Rows)
            {
                if (Convert.ToInt64(row["tuote_id"]) != tuoteId)
                    continue;

                // Yhdistä olemassa olevaan riviin, jos sama tuote on jo ostoskorissa.
                row["maara"] = Convert.ToInt32(row["maara"]) + maara;
                row["hinta"] = Convert.ToDecimal(row["yksikkohinta"]) * Convert.ToInt32(row["maara"]);
                tb_tilausMaara.Text = "1";
                Paivita_OstoskoriTila();
                return;
            }

            ostoskori.Rows.Add(tuoteId, tuoteNimi, maara, yksikkohinta, yksikkohinta * maara);
            tb_tilausMaara.Text = "1";
            Paivita_OstoskoriTila();
        }

        private void Poista_OstoskoriRivi_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: DataRowView rivi })
                return;

            ostoskori.Rows.Remove(rivi.Row);
            Paivita_OstoskoriTila();
        }

        // Tilaukset: tallenna ostoskori tilaukseksi.
        private void Tee_Tilaus_Click(object sender, RoutedEventArgs e)
        {
            if (cb_asiakas_tilaus.SelectedValue == null)
            {
                MessageBox.Show("Valitse asiakas ennen tilauksen tekemistä.");
                return;
            }

            if (ostoskori.Rows.Count == 0)
            {
                MessageBox.Show("Ostoskori on tyhjä.");
                return;
            }

            long asiakasId = (long)cb_asiakas_tilaus.SelectedValue;
            var rivit = new List<(long productId, int quantity)>();
            foreach (DataRow row in ostoskori.Rows)
            {
                long productId = Convert.ToInt64(row["tuote_id"]);
                int quantity = Convert.ToInt32(row["maara"]);
                if (quantity <= 0)
                    continue;

                rivit.Add((productId, quantity));
            }

            if (rivit.Count == 0)
            {
                MessageBox.Show("Ostoskori on tyhjä.");
                return;
            }

            try
            {
                appService.CreateOrderWithLines(asiakasId, rivit);
                Tyhjenna_Ostoskori();
                Paivita_Tilausnakyma();
                Paivita_Tuotelista();
                Paivita_TuoteCombos();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Tilauksen tallennus epäonnistui: " + ex.Message);
            }
        }

        private void Toimita_Tilaus_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not Button { DataContext: TilausNakyma tilaus })
                return;

            long tilausId = tilaus.Id;

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
            if (sender is not Button { DataContext: TilausNakyma tilaus })
                return;

            long tilausId = tilaus.Id;

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
            if (sender is not Button { DataContext: TilausNakyma tilaus })
                return;

            long tilausId = tilaus.Id;

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
                Paivita_Tuotelista();
                Paivita_TuoteCombos();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Tilauksen poisto epäonnistui: " + ex.Message);
            }
        }

        // Tilaukset: rivin määrän muutos (suora muokkaus).
        private void TilausRivi_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit)
                return;

            if (e.Row.Item is not TilausRiviNakyma rivi)
                return;

            // Viivytä päivitystä, kunnes muokkaus on vahvistettu.
            Dispatcher.BeginInvoke(
                new Action(() => Paivita_TilausRivi(rivi)),
                System.Windows.Threading.DispatcherPriority.Background);
        }

        private void Paivita_TilausRivi(TilausRiviNakyma rivi)
        {
            if (rivi.Maara <= 0)
            {
                MessageBox.Show("Anna kelvollinen määrä (positiivinen kokonaisluku).");
                Paivita_Tilausnakyma();
                return;
            }

            try
            {
                appService.UpdateOrderQuantity(rivi.RiviId, rivi.Maara);
                Paivita_Tilausnakyma();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Tilauksen määrän päivitys epäonnistui: " + ex.Message);
                Paivita_Tilausnakyma();
            }
        }

        // DataGrid-valinnan poisto: sallii valinnan poistamisen klikkaamalla valittua riviä.
        private void Tilauslista_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not DataGrid grid)
                return;

            var row = ItemsControl.ContainerFromElement(grid, e.OriginalSource as DependencyObject) as DataGridRow;
            if (row == null)
            {
                if (grid.SelectedItem != null)
                {
                    grid.SelectedItem = null;
                    e.Handled = true;
                }
                return;
            }

            if (row.IsSelected)
            {
                grid.SelectedItem = null;
                e.Handled = true;
            }
        }

        private void Toimitetutlista_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not DataGrid grid)
                return;

            var row = ItemsControl.ContainerFromElement(grid, e.OriginalSource as DependencyObject) as DataGridRow;
            if (row == null)
            {
                if (grid.SelectedItem != null)
                {
                    grid.SelectedItem = null;
                    e.Handled = true;
                }
                return;
            }

            if (row.IsSelected)
            {
                grid.SelectedItem = null;
                e.Handled = true;
            }
        }

        private void Tuotelista_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is not DataGrid grid)
                return;

            var row = ItemsControl.ContainerFromElement(grid, e.OriginalSource as DependencyObject) as DataGridRow;
            if (row == null)
            {
                if (grid.SelectedItem != null)
                {
                    grid.SelectedItem = null;
                    e.Handled = true;
                }
                return;
            }

            if (row.IsSelected)
            {
                grid.SelectedItem = null;
                e.Handled = true;
            }
        }

        // Tuotteet: nimi/hinta/kategoria -muutokset (ylläpitäjä).
        private void Tuotelista_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction != DataGridEditAction.Commit)
                return;

            if (e.Row.Item is not DataRowView rivi)
                return;

            // Viivytä päivitystä, kunnes rivimuutos on vahvistettu.
            Dispatcher.BeginInvoke(new Action(() => Paivita_TuoteTiedot(rivi)), System.Windows.Threading.DispatcherPriority.Background);
        }

        private void Paivita_TuoteTiedot(DataRowView rivi)
        {
            if (rivi.Row.RowState != DataRowState.Modified)
                return;

            long tuoteId = Convert.ToInt64(rivi["id"]);
            string nimi = Convert.ToString(rivi["nimi"])?.Trim() ?? "";
            decimal hinta = Convert.ToDecimal(rivi["hinta"]);
            long? kategoriaId = null;
            if (!rivi.Row.IsNull("kategoria_id"))
                kategoriaId = Convert.ToInt64(rivi["kategoria_id"]);

            if (nimi == "" || hinta < 0)
            {
                MessageBox.Show("Tuotenimi ei voi olla tyhjä ja hinta ei voi olla negatiivinen.");
                Paivita_Tuotelista();
                return;
            }

            try
            {
                appService.UpdateProductDetails(tuoteId, nimi, hinta, kategoriaId);
                Paivita_Tuotelista();
                Paivita_TuoteCombos();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Tuotteen päivitys epäonnistui: " + ex.Message);
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
