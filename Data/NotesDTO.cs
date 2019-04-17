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
            MySqlConnection connection=null;
            MySqlTransaction transaction=null;
            try
            {
                bool r;
                connection = dataAccess.openConnection();
                transaction = connection.BeginTransaction();
                MySqlCommand command = new MySqlCommand();
                command.Connection = connection;
                command.CommandText = "DELETE FROM notes WHERE id=@id;";
                command.Parameters.AddWithValue("@id", id);
                Int32 result = command.ExecuteNonQuery();
                if (result == 1)
                {
                    r = true;
                }
                else
                {
                    r = false;
                }
                transaction.Commit();
                return r;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                return false;
            }
            finally
            {
                dataAccess.closeConnection(connection);
            }

        }

        public bool update(Notes model)
        {
            MySqlConnection connection = null;
            MySqlTransaction transaction = null;
            try
            {
                bool r;
                connection = dataAccess.openConnection();
                transaction = connection.BeginTransaction();
                MySqlCommand command = new MySqlCommand();
                command.Connection = connection;
                command.CommandText = "UPDATE notes SET title=@title,text=@text WHERE id=@id;";
                command.Parameters.AddWithValue("@title", model.Title);
                command.Parameters.AddWithValue("@text", model.Text);
                command.Parameters.AddWithValue("@id", model.Id);
                Int32 result = command.ExecuteNonQuery();
                if (result == 1)
                {
                    r = true;
                }
                else
                {
                    r = false;
                }
                transaction.Commit();
                return r;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                return false;
            }
            finally
            {
                dataAccess.closeConnection(connection);
            }

        }

        public Notes get(Int32 id)
        {
            MySqlConnection connection = null;
            try
            {
                Notes item = new Notes();
                connection = dataAccess.openConnection();
                MySqlCommand command = new MySqlCommand();
                command.Connection = connection;
                command.CommandText = "SELECT * FROM notes WHERE id=@id;";
                command.Parameters.AddWithValue("@id", id);
                MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

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
            finally
            {
                dataAccess.closeConnection(connection);
            }

        }

        public List<Notes> list()
        {
            MySqlConnection connection = null;
            try
            {
                List<Notes> list = new List<Notes>();
                connection = dataAccess.openConnection();
                MySqlCommand command = new MySqlCommand();
                command.Connection = connection;
                command.CommandText = "SELECT * FROM notes;";
                MySqlDataAdapter adapter = new MySqlDataAdapter(command);
                DataTable dataTable = new DataTable();
                adapter.Fill(dataTable);

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
            finally
            {
                dataAccess.closeConnection(connection);
            }

        }

        public bool add(Notes model)
        {
            MySqlConnection connection = null;
            MySqlTransaction transaction = null;
            try
            {
                bool r;
                connection = dataAccess.openConnection();
                transaction = connection.BeginTransaction();
                MySqlCommand command = new MySqlCommand();
                command.Connection = connection;
                command.CommandText = "INSERT INTO notes(title,text) VALUES(@title,@text);";
                command.Parameters.AddWithValue("@title", model.Title);
                command.Parameters.AddWithValue("@text", model.Text);
                Int32 result = command.ExecuteNonQuery();
                if (result == 1) { 
                    r = true;
                }
                else
                {
                    r = false;
                }
                transaction.Commit();
                return r;
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                return false;
            }
            finally
            {
                dataAccess.closeConnection(connection);
            }

        }
    }
}
