using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;

namespace UG_DT_SendCxSms.Logic
{
    public class DataBaseContext
    {
        public MySqlConnection custServiceDbConnection = null;
        public MySqlConnection customersDbConnection = null;
        private MySqlCommand command = null;
        public DataBaseContext()
        {
            var custService_initdbConnection = System.Configuration.ConfigurationManager.AppSettings["custService"];
            var customers_initdbConnection = System.Configuration.ConfigurationManager.AppSettings["customers"];

            custServiceDbConnection = new MySqlConnection(custService_initdbConnection);
            customersDbConnection = new MySqlConnection(customers_initdbConnection);
        }
        public DataTable ExecuteQuery(string StoredProcedure, Hashtable Variables, MySqlConnection connection)
        {
            try
            {
                using (command = new MySqlCommand(StoredProcedure, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    foreach (DictionaryEntry variable in Variables)
                    {
                        try
                        {
                            if (!string.IsNullOrEmpty(variable.Value.ToString()))
                            {
                                command.Parameters.AddWithValue(variable.Key.ToString(), variable.Value.ToString());
                            }
                            else
                            {
                                command.Parameters.AddWithValue(variable.Key.ToString(), "");
                            }
                        }
                        catch (Exception)
                        {

                        }
                    }
                    try
                    {
                        command.CommandTimeout = 200;
                        DataTable dt = new DataTable();
                        if (connection.State == ConnectionState.Open)
                        {
                            connection.Close();
                        }
                        connection.Open();
                        dt.Load(command.ExecuteReader());
                        connection.Close();
                        return dt;
                    }
                    catch (Exception ex)
                    {
                        ArrayList errors = new ArrayList();
                        Helpers helpers = new Helpers();
                        errors.Add($"Error: {ex.Message}");
                        helpers.writeToFile(errors);
                        throw;
                    }

                }
            }
            catch (Exception ex)
            {
                ArrayList errors = new ArrayList();
                Helpers helpers = new Helpers();
                errors.Add($"Error: {ex.Message}");
                helpers.writeToFile(errors);
                throw;
            }
            finally { connection.Close(); }
        }

        public bool ExecuteNonQuery(string StoredProcedure, Hashtable Variables, MySqlConnection connection)
        {
            bool isSuccessfull = false;
            try
            {
                using (command = new MySqlCommand(StoredProcedure, connection))
                {
                    command.CommandType = CommandType.StoredProcedure;
                    foreach (DictionaryEntry variable in Variables)
                    {
                        if (variable.Value != null)
                        {
                            command.Parameters.AddWithValue(variable.Key.ToString(), variable.Value.ToString());
                        }
                        else
                        {
                            command.Parameters.AddWithValue(variable.Key.ToString(), "");
                        }
                    }
                    command.CommandTimeout = 200;
                    if (connection.State == ConnectionState.Open)
                    {
                        connection.Close();
                    }
                    connection.Open();
                    int isExecuted = command.ExecuteNonQuery();
                    connection.Close();

                    if (isExecuted > 0)
                    {
                        isSuccessfull = true;
                    }

                }
            }
            catch (Exception ex)
            {
                ArrayList errors = new ArrayList();
                Helpers helpers = new Helpers();
                errors.Add($"Error: {ex.Message}");
                helpers.writeToFile(errors);
            }
            finally { connection.Close(); }

            return isSuccessfull;
        }
    }
}
