using System.Collections.Generic;
using System.Web.Http;
using Model;
using Datos;
using Utilities;
using System.Net.Http;
using System.Linq;
using System;
using System.Diagnostics;

namespace CRUD_Servicios_REST_ASP.NET_CSharp.Controllers
{
    public class NotesController : ApiController
    {
        private NotesDTO notesDTO;
        private string token;

        public NotesController()
        {
            notesDTO = new NotesDTO();
            token = "qwerty";
        }

        // GET: api/Notes
        public IEnumerable<string> Get()
        {
            return new string[] { "value1", "value2" };
        }

        // GET: api/Notes/5
        public string Get(int id)
        {
            return "value";
        }

        // POST: api/Notes
        public Dictionary<String, Object> Post(HttpRequestMessage request, [FromBody]Notes notes)
        {
            IEnumerable<string> headerValues = request.Headers.GetValues("Authentication");
            var authentication = headerValues.FirstOrDefault();
            try
            {
                if (authentication == token)
                {
                    if (notesDTO.add(notes))
                    {
                        return JSON.successToJson(Message.SUCCESSFUL_SAVE);
                    }
                    else
                    {
                        return JSON.errorToJson(Message.ERROR_SAVE);
                    }
                }
                else
                {
                    return JSON.errorToJson(Message.ERROR_SESSION_VALIDATION);
                }
            }catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                return JSON.errorToJson(Message.ERROR_PROCESSING_DATA);
            }
        }

        // PUT: api/Notes/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE: api/Notes/5
        public void Delete(int id)
        {
        }
    }
}
