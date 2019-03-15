using System;
using MySql.Data.MySqlClient;
using Model;
using System.Diagnostics;

namespace Datos
{
    public class NotesDTO
    {
        private DataAccess dataAccess;

        public NotesDTO()
        {
            dataAccess = new DataAccess();
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
