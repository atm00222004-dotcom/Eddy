using Npgsql;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace _8F
{
    public partial class OperatorMaster : Window
    {
        private int SelectedId = 0;
        private List<Operator> operatorList = new List<Operator>();

        public OperatorMaster()
        {
            InitializeComponent();
            LoadOperators();
        }

        // =========================
        // LOAD
        // =========================
        private void LoadOperators()
        {
            operatorList.Clear();

            using (var con = new NpgsqlConnection(ConfigurationSettings.AppSettings["ConnectionString"]))
            {
                con.Open();

                string sql = @"SELECT *
                               FROM public.""Operators""
                               WHERE ""IsActive"" = true
                               ORDER BY ""OperatorName""";

                using var cmd = new NpgsqlCommand(sql, con);
                using var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    operatorList.Add(new Operator()
                    {
                        Id = Convert.ToInt32(reader["Id"]),
                        OperatorName = reader["OperatorName"].ToString(),
                        IsActive = Convert.ToBoolean(reader["IsActive"])
                    });
                }
            }

            grdOperator.ItemsSource = null;
            grdOperator.ItemsSource = operatorList;

            // EMPTY STATE FIXED
            if (operatorList.Count == 0)
            {
                txtOperatorMessage.Visibility = Visibility.Visible;
                grdOperator.Visibility = Visibility.Collapsed;
            }
            else
            {
                txtOperatorMessage.Visibility = Visibility.Collapsed;
                grdOperator.Visibility = Visibility.Visible;
            }

            ResetForm();
        }

        // =========================
        // ADD / SAVE
        // =========================
        private void btnAddSave_MouseDown(object sender, MouseButtonEventArgs e)
        {
            string name = txtOperatorName.Text.Trim();

            if (string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show("Operator name cannot be empty.");
                return;
            }

            using var con = new NpgsqlConnection(ConfigurationSettings.AppSettings["ConnectionString"]);
            con.Open();

            if (SelectedId == 0)
            {
                if (OperatorExists(name))
                {
                    MessageBox.Show("Operator already exists.");
                    return;
                }

                string sql = @"INSERT INTO public.""Operators""(""OperatorName"")
                               VALUES(@name)";

                using var cmd = new NpgsqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.ExecuteNonQuery();

                MessageBox.Show("Operator added.");
            }
            else
            {
                if (OperatorExists(name, SelectedId))
                {
                    MessageBox.Show("Duplicate operator name.");
                    return;
                }

                string sql = @"UPDATE public.""Operators""
                               SET ""OperatorName""=@name
                               WHERE ""Id""=@id";

                using var cmd = new NpgsqlCommand(sql, con);
                cmd.Parameters.AddWithValue("@name", name);
                cmd.Parameters.AddWithValue("@id", SelectedId);
                cmd.ExecuteNonQuery();

                MessageBox.Show("Operator updated.");
            }

            LoadOperators();
        }

        // =========================
        // DUPLICATE CHECK
        // =========================
        private bool OperatorExists(string name, int ignoreId = 0)
        {
            using var con = new NpgsqlConnection(ConfigurationSettings.AppSettings["ConnectionString"]);
            con.Open();

            string sql = @"SELECT COUNT(1)
                           FROM public.""Operators""
                           WHERE LOWER(""OperatorName"") = LOWER(@name)
                           AND ""IsActive"" = true
                           AND (@id = 0 OR ""Id"" <> @id)";

            using var cmd = new NpgsqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@name", name);
            cmd.Parameters.AddWithValue("@id", ignoreId);

            return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
        }

        // =========================
        // GRID EDIT (FIXED)
        // =========================
        private void Edit_Click(object sender, RoutedEventArgs e)
        {
            var op = ((FrameworkElement)sender).DataContext as Operator;
            if (op == null) return;

            SelectedId = op.Id;
            txtOperatorName.Text = op.OperatorName;
            lblAddSave.Content = "Save";

            grdOperator.SelectedItem = op;
        }

        // =========================
        // GRID DELETE (FIXED)
        // =========================
        private void Delete_Click(object sender, RoutedEventArgs e)
        {
            var op = ((FrameworkElement)sender).DataContext as Operator;
            if (op == null) return;

            if (MessageBox.Show("Delete this operator?", "Confirm",
                MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                return;

            using var con = new NpgsqlConnection(ConfigurationSettings.AppSettings["ConnectionString"]);
            con.Open();

            string sql = @"UPDATE public.""Operators""
                           SET ""IsActive"" = false
                           WHERE ""Id"" = @id";

            using var cmd = new NpgsqlCommand(sql, con);
            cmd.Parameters.AddWithValue("@id", op.Id);
            cmd.ExecuteNonQuery();

            LoadOperators();
        }

        // =========================
        // SELECTION
        // =========================
        private void grdOperator_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (grdOperator.SelectedItem == null)
                return;

            var op = (Operator)grdOperator.SelectedItem;

            SelectedId = op.Id;
            txtOperatorName.Text = op.OperatorName;

            lblAddSave.Content = "Save";
        }

        // =========================
        // TEXT CHANGE
        // =========================
        private void txtOperatorName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtOperatorName.Text))
            {
                SelectedId = 0;
                lblAddSave.Content = "Add";
                grdOperator.SelectedItem = null;
            }
        }

        // =========================
        // RESET
        // =========================
        private void ResetForm()
        {
            txtOperatorName.Clear();
            SelectedId = 0;
            lblAddSave.Content = "Add";
            grdOperator.SelectedItem = null;
        }

        // CLOSE
        private void btnClose_MouseDown(object sender, MouseButtonEventArgs e)
        {
            Close();
        }
    }
}