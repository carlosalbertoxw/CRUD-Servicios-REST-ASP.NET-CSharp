using System;
using MySql.Data.MySqlClient;
using Model;
using System.Data;
using System.Diagnostics;
using System.Collections.Generic;

namespace Datos
{
    public class NotesDTO
    {
        private DataAccess dataAccess;

        public NotesDTO()
        {
            dataAccess = new DataAccess();
        }

        public bool delete(Int32 id)
        {
            try
            {
                bool r;
                MySqlConnection connection = dataAccess.openConnection();
                MySqlTransaction transaction = connection.BeginTransaction();
                MySqlCommand command = new MySqlCommand();
                command.Connection = connection;
                command.CommandText = "DELETE FROM notes WHERE id=@id;";
                command.Parameters.AddWithValue("@id", id);
                Int32 result = command.ExecuteNonQuery();
                if (result == 1)
                {
                    transaction.Commit();
                    r = true;
                }
                else
                {
                    transaction.Rollback();
                    r = false;
                }
                dataAccess.closeConnection(connection);
                return r;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                return false;
            }

        }

        public bool update(Notes model)
        {
            try
            {
                bool r;
                MySqlConnection connection = dataAccess.openConnection();
                MySqlTransaction transaction = connection.BeginTransaction();
                MySqlCommand command = new MySqlCommand();
                command.Connection = connection;
                command.CommandText = "UPDATE notes SET title=@title,text=@text WHERE id=@id;";
                command.Parameters.AddWithValue("@title", model.Title);
                command.Parameters.AddWithValue("@text", model.Text);
                command.Parameters.AddWithValue("@id", model.Id);
                Int32 result = command.ExecuteNonQuery();
                if (result == 1)
                {
                    transaction.Commit();
                    r = true;
                }
                else
                {
                    transaction.Rollback();
                    r = false;
                }
                dataAccess.closeConnection(connection);
                return r;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                return false;
            }

        }

        public Notes get(Int32 id)
        {
            try
            {
                Notes item = new Notes();
                MySqlConnection connection = dataAccess.openConnection();
                MySqlCommand command = new MySqlCommand();
                command.Connection = connection;
                command.CommandText = "SELECT * FROM notes WHERE id=@id;";
                command.Parameters.AddWithValue("@id", id);
                MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);
                dataAccess.closeConnection(connection);

                foreach (DataRow row in dataTable.Rows)
                {
                    item.Id = Int32.Parse(row[0].ToString());
                    item.Title = row[1].ToString();
                    item.Text = row[2].ToString();
                }

                return item;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                return null;
            }

        }

        public List<Notes> list()
        {
            try
            {
                List<Notes> list = new List<Notes>();
                MySqlConnection connection = dataAccess.openConnection();
                MySqlCommand command = new MySqlCommand();
                command.Connection = connection;
                command.CommandText = "SELECT * FROM notes;";
                MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);
                dataAccess.closeConnection(connection);

                foreach (DataRow row in dataTable.Rows)
                {
                    Notes item = new Notes();
                    item.Id = Int32.Parse(row[0].ToString());
                    item.Title = row[1].ToString();
                    item.Text = row[2].ToString();
                    list.Add(item);
                }

                return list;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                return null;
            }

        }

        public bool add(Notes model)
        {
            try
            {
                bool r;
                MySqlConnection connection = dataAccess.openConnection();
                MySqlTransaction transaction = connection.BeginTransaction();
                MySqlCommand command = new MySqlCommand();
                command.Connection = connection;
                command.CommandText = "INSERT INTO notes(title,text) VALUES(@title,@text);";
                command.Parameters.AddWithValue("@title", model.Title);
                command.Parameters.AddWithValue("@text", model.Text);
                Int32 result = command.ExecuteNonQuery();
                if (result == 1) { 
                    transaction.Commit();
                    r = true;
                }
                else
                {
                    transaction.Rollback();
                    r = false;
                }
                dataAccess.closeConnection(connection);
                return r;
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                return false;
            }

        }
    }
}
