using ControlzEx.Theming;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Data.Sqlite;
using System.Data;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Globalization;



namespace Sovelluskehitys_2025
{
    public partial class MainWindow : MetroWindow
    {
        SqliteConnection yhteys;

        public MainWindow()
        {
            InitializeComponent();

            ThemeManager.Current.ChangeTheme(this, "Light.Blue");
        }

        private void Paivita_DataGrid(string kysely, string taulun_nimi, DataGrid grid)
        {
            using var komento = yhteys.CreateCommand();
            komento.CommandText = kysely;

            using var lukija = komento.ExecuteReader();
            var taulu = new DataTable(taulun_nimi);
            taulu.Load(lukija);

            grid.ItemsSource = taulu.DefaultView;
        }

        private void Paivita_ComboBox(string kysely, ComboBox kombo)
        {
            using var komento = yhteys.CreateCommand();
            komento.CommandText = kysely;

            using var lukija = komento.ExecuteReader();

            var taulu = new DataTable();
            taulu.Columns.Add("id", typeof(long));
            taulu.Columns.Add("nimi", typeof(string));

            kombo.ItemsSource = taulu.DefaultView;
            kombo.DisplayMemberPath = "nimi";
            kombo.SelectedValuePath = "id";

            while (lukija.Read())
            {
                long id = lukija.GetInt64(0);
                string nimi = lukija.GetString(1);
                taulu.Rows.Add(id, nimi);
            }
        }

        private void Paivita_Tilauslistat()
        {
            Paivita_DataGrid(@"
                SELECT t.id as id,
                    tr.id as rivi_id,
                    a.nimi as asiakas,
                    a.osoite as osoite,
                    tu.nimi as tuote,
                    tr.maara as maara,
                    tr.tuote_id as tuote_id,
                    t.toimitettu as toimitettu
                FROM tilaukset t
                JOIN asiakkaat a ON a.id = t.asiakas_id
                JOIN tilausrivit tr ON tr.tilaus_id = t.id
                JOIN tuotteet tu ON tu.id = tr.tuote_id
                WHERE t.toimitettu = 0
                ORDER BY t.id DESC;", "tilaukset", tilauslista);
        }
        private void Paivita_Toimitetutlistat() 
        {
            Paivita_DataGrid(@"
                SELECT t.id as id,
                    a.nimi as asiakas,
                    a.osoite as osoite,
                    tu.nimi as tuote,
                    tr.maara as maara,
                    t.toimitettu as toimitettu
                FROM tilaukset t
                JOIN asiakkaat a ON a.id = t.asiakas_id
                JOIN tilausrivit tr ON tr.tilaus_id = t.id
                JOIN tuotteet tu ON tu.id = tr.tuote_id
                WHERE t.toimitettu = 1
                ORDER BY t.id DESC;", "toimitetut", toimitetut_lista);
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

            using var cmd = yhteys.CreateCommand();
            cmd.CommandText = "INSERT INTO tuotteet (nimi, hinta, varastosaldo) VALUES (@nimi, @hinta, @saldo);";
            cmd.Parameters.AddWithValue("@nimi", tekstikentta_1.Text);
            cmd.Parameters.AddWithValue("@hinta", hinta);
            cmd.Parameters.AddWithValue("@saldo", saldo);
            cmd.ExecuteNonQuery();


            Paivita_DataGrid("SELECT id, nimi, hinta, varastosaldo FROM tuotteet ORDER BY nimi", "tuotteet", tuotelista);
            Paivita_ComboBox("SELECT id, nimi FROM tuotteet ORDER BY nimi", cb_tuotelista);
            Paivita_ComboBox("SELECT id, nimi FROM tuotteet ORDER BY nimi", cb_tuote_tilaus);

            tekstikentta_1.Clear();
            tekstikentta_2.Clear();
            tekstikentta_3.Clear();
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            Paivita_DataGrid("SELECT id, nimi, hinta, varastosaldo FROM tuotteet ORDER BY nimi", "tuotteet", tuotelista);
        }

        private void Button_Click_2(object sender, RoutedEventArgs e)
        {
            if (cb_tuotelista.SelectedValue == null)
            {
                MessageBox.Show("Valitse poistettava tuote ensin.");
                return;
            }

            long id = (long)cb_tuotelista.SelectedValue;

            using var cmd = yhteys.CreateCommand();
            cmd.CommandText = "DELETE FROM tuotteet WHERE id = @id;";
            cmd.Parameters.AddWithValue("@id", id);
            cmd.ExecuteNonQuery();

            Paivita_DataGrid("SELECT id, nimi, hinta, varastosaldo FROM tuotteet ORDER BY nimi", "tuotteet", tuotelista);
            Paivita_ComboBox("SELECT id, nimi FROM tuotteet ORDER BY nimi", cb_tuotelista);
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

            using var cmd = yhteys.CreateCommand();
            cmd.CommandText = "INSERT INTO asiakkaat (nimi, osoite, puhelin) VALUES (@nimi, @osoite, @puhelin);";
            cmd.Parameters.AddWithValue("@nimi", asiakas_nimi.Text);
            cmd.Parameters.AddWithValue("@osoite", asiakas_osoite.Text);
            cmd.Parameters.AddWithValue("@puhelin", asiakas_puhelin.Text);
            cmd.ExecuteNonQuery();


            Paivita_DataGrid("SELECT id, nimi, osoite, puhelin FROM asiakkaat ORDER BY nimi", "asiakkaat", asiakaslista);
            Paivita_ComboBox("SELECT id, nimi FROM asiakkaat ORDER BY nimi", cb_asiakas_tilaus);

            asiakas_nimi.Clear();
            asiakas_osoite.Clear();
            asiakas_puhelin.Clear();
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
                tilaviesti.Text = "Tietokantayhteys avattu onnistuneesti.";

                Paivita_DataGrid("SELECT id, nimi, hinta, varastosaldo FROM tuotteet ORDER BY nimi", "tuotteet", tuotelista);
                Paivita_DataGrid("SELECT id, nimi, osoite, puhelin FROM asiakkaat ORDER BY nimi", "asiakkaat", asiakaslista);

                Paivita_Tilausnakyma();

                Paivita_ComboBox("SELECT id, nimi FROM tuotteet ORDER BY nimi;", cb_tuotelista);
                Paivita_ComboBox("SELECT id, nimi FROM tuotteet ORDER BY nimi;", cb_tuote_tilaus);
                Paivita_ComboBox("SELECT id, nimi FROM asiakkaat ORDER BY nimi;", cb_asiakas_tilaus);
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

            using var tx = yhteys.BeginTransaction();
            try
            {
                long tilausId;

                using (var cmd = yhteys.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = "INSERT INTO tilaukset (asiakas_id) VALUES (@asiakas_id); SELECT last_insert_rowid();";
                    cmd.Parameters.AddWithValue("@asiakas_id", asiakasId);
                    tilausId = (long)cmd.ExecuteScalar();
                }

                using (var cmd = yhteys.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"
                        INSERT INTO tilausrivit (tilaus_id, tuote_id, maara, rivihinta)
                        VALUES (@tilaus_id, @tuote_id, @maara,
                                (SELECT hinta FROM tuotteet WHERE id = @tuote_id) * @maara);";
                    cmd.Parameters.AddWithValue("@tilaus_id", tilausId);
                    cmd.Parameters.AddWithValue("@tuote_id", tuoteId);
                    cmd.Parameters.AddWithValue("@maara", maara);
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = yhteys.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"
                        UPDATE tuotteet
                        SET varastosaldo = varastosaldo - @maara
                        WHERE id = @tuote_id AND varastosaldo >= @maara;";
                    cmd.Parameters.AddWithValue("@maara", maara);
                    cmd.Parameters.AddWithValue("@tuote_id", tuoteId);

                    int affected = cmd.ExecuteNonQuery();
                    if (affected == 0)
                        throw new Exception("Varastosaldo ei riitä tilaukseen.");
                }

                tx.Commit();

                Paivita_Tilausnakyma();
            }
            catch (Exception ex)
            {
                tx.Rollback();
                MessageBox.Show("Tilauksen tallennus epäonnistui: " + ex.Message);
            }
        }

        private void Toimita_Tilaus_Click(object sender, RoutedEventArgs e)
        { 
            DataRowView rivi = (DataRowView)((Button)e.Source).DataContext;
            long tilausId = Convert.ToInt64(rivi["id"]);

            using var tx = yhteys.BeginTransaction();
            try
            {
                // Päivitä tilaus toimitetuksi
                using (var komento = yhteys.CreateCommand())
                {
                    komento.Transaction = tx;
                    komento.CommandText = "UPDATE tilaukset SET toimitettu = 1 WHERE id = @id;";
                    komento.Parameters.AddWithValue("@id", tilausId);
                    komento.ExecuteNonQuery();
                }

                tx.Commit();
                Paivita_Tilausnakyma();
            }
            catch (Exception ex)
            {
                tx.Rollback();
                MessageBox.Show("Tilauksen toimitus epäonnistui: " + ex.Message);
            }
        }

        private void Peruuta_Tilaus_Click(object sender, RoutedEventArgs e)
        {
            DataRowView rivi = (DataRowView)((Button)e.Source).DataContext;
            long tilausId = Convert.ToInt64(rivi["id"]);

            using var tx = yhteys.BeginTransaction();
            try
            {
                // Merkitse tilaus peruutetuksi
                using (var komento = yhteys.CreateCommand())
                {
                    komento.Transaction = tx;
                    komento.CommandText = "UPDATE tilaukset SET toimitettu = 0 WHERE id = @id;";
                    komento.Parameters.AddWithValue("@id", tilausId);
                    komento.ExecuteNonQuery();
                }

                tx.Commit();
                Paivita_Tilausnakyma();
            }
            catch (Exception ex)
            {
                tx.Rollback();
                MessageBox.Show("Tilauksen peruutus epäonnistui: " + ex.Message);
            }
        }

        private void Poista_Tilaus_Click(object sender, RoutedEventArgs e)
        {
            DataRowView rivi = (DataRowView)((Button)e.Source).DataContext;
            long tilausId = Convert.ToInt64(rivi["id"]);

            using var tx = yhteys.BeginTransaction();
            try
            {
                using (var cmd = yhteys.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"
                        UPDATE tuotteet
                        SET varastosaldo = varastosaldo + (
                            SELECT SUM(tr.maara)
                            FROM tilausrivit tr
                            WHERE tr.tuote_id = tuotteet.id AND tr.tilaus_id = @tilaus_id
                        )
                        WHERE id IN (
                            SELECT tuote_id FROM tilausrivit WHERE tilaus_id = @tilaus_id
                        );";
                    cmd.Parameters.AddWithValue("@tilaus_id", tilausId);
                    cmd.ExecuteNonQuery();
                }

                using (var cmd = yhteys.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = "DELETE FROM tilaukset WHERE id = @id;";
                    cmd.Parameters.AddWithValue("@id", tilausId);
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
                Paivita_Tilausnakyma();
            }
            catch (Exception ex)
            {
                tx.Rollback();
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

            int muutos = uusiMaara - vanhaMaara;
            if (muutos == 0)
            {
                rivi.Row.AcceptChanges();
                return;
            }

            long riviId = Convert.ToInt64(rivi["rivi_id"]);
            long tuoteId = Convert.ToInt64(rivi["tuote_id"]);

            using var tx = yhteys.BeginTransaction();
            try
            {
                if (muutos > 0)
                {
                    using var saldo = yhteys.CreateCommand();
                    saldo.Transaction = tx;
                    saldo.CommandText = @"
                        UPDATE tuotteet
                        SET varastosaldo = varastosaldo - @muutos
                        WHERE id = @tuote_id AND varastosaldo >= @muutos;";
                    saldo.Parameters.AddWithValue("@muutos", muutos);
                    saldo.Parameters.AddWithValue("@tuote_id", tuoteId);
                    int affected = saldo.ExecuteNonQuery();
                    if (affected == 0)
                        throw new Exception("Varastosaldo ei riitä tilaukseen.");
                }
                else
                {
                    using var saldo = yhteys.CreateCommand();
                    saldo.Transaction = tx;
                    saldo.CommandText = @"
                        UPDATE tuotteet
                        SET varastosaldo = varastosaldo + @palauta
                        WHERE id = @tuote_id;";
                    saldo.Parameters.AddWithValue("@palauta", -muutos);
                    saldo.Parameters.AddWithValue("@tuote_id", tuoteId);
                    saldo.ExecuteNonQuery();
                }

                using (var cmd = yhteys.CreateCommand())
                {
                    cmd.Transaction = tx;
                    cmd.CommandText = @"
                        UPDATE tilausrivit
                        SET maara = @maara,
                            rivihinta = (SELECT hinta FROM tuotteet WHERE id = @tuote_id) * @maara
                        WHERE id = @rivi_id;";
                    cmd.Parameters.AddWithValue("@maara", uusiMaara);
                    cmd.Parameters.AddWithValue("@tuote_id", tuoteId);
                    cmd.Parameters.AddWithValue("@rivi_id", riviId);
                    cmd.ExecuteNonQuery();
                }

                tx.Commit();
                rivi.Row.AcceptChanges();
            }
            catch (Exception ex)
            {
                tx.Rollback();
                MessageBox.Show("Tilauksen määrän päivitys epäonnistui: " + ex.Message);
                Paivita_Tilausnakyma();
            }
        }

        private void Tuotelista_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if (string.Equals(e.PropertyName, "varastosaldo", StringComparison.OrdinalIgnoreCase))
                e.Column.IsReadOnly = true;
        }
    }
}
