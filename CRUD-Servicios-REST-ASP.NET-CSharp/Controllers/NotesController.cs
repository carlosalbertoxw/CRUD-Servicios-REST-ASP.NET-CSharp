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
        public Dictionary<String, Object> Get(HttpRequestMessage request)
        {
            IEnumerable<string> headerValues = request.Headers.GetValues("Authentication");
            var authentication = headerValues.FirstOrDefault();
            try
            {
                if (authentication == token)
                {
                    List<Notes> list = notesDTO.list();
                    if (list != null)
                    {
                        return JSON.successToJson(list);
                    }
                    else
                    {
                        return JSON.errorToJson(Message.ERROR_CONSULT);
                    }
                }
                else
                {
                    return JSON.errorToJson(Message.ERROR_SESSION_VALIDATION);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                return JSON.errorToJson(Message.ERROR_PROCESSING_DATA);
            }
        }

        // GET: api/Notes/5
        public Dictionary<String, Object> Get(HttpRequestMessage request, int id)
        {
            IEnumerable<string> headerValues = request.Headers.GetValues("Authentication");
            var authentication = headerValues.FirstOrDefault();
            try
            {
                if (authentication == token)
                {
                    Notes notes = notesDTO.get(id);
                    if (notes != null)
                    {
                        return JSON.successToJson(notes);
                    }
                    else
                    {
                        return JSON.errorToJson(Message.ERROR_CONSULT);
                    }
                }
                else
                {
                    return JSON.errorToJson(Message.ERROR_SESSION_VALIDATION);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                return JSON.errorToJson(Message.ERROR_PROCESSING_DATA);
            }
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
        public Dictionary<String, Object> Put(HttpRequestMessage request, [FromBody]Notes notes, int id)
        {
            IEnumerable<string> headerValues = request.Headers.GetValues("Authentication");
            var authentication = headerValues.FirstOrDefault();
            try
            {
                if (authentication == token)
                {
                    notes.Id = id;
                    if (notesDTO.update(notes))
                    {
                        return JSON.successToJson(Message.SUCCESSFUL_UPDATE);
                    }
                    else
                    {
                        return JSON.errorToJson(Message.ERROR_UPDATE);
                    }
                }
                else
                {
                    return JSON.errorToJson(Message.ERROR_SESSION_VALIDATION);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                return JSON.errorToJson(Message.ERROR_PROCESSING_DATA);
            }
        }

        // DELETE: api/Notes/5
        public Dictionary<String, Object> Delete(HttpRequestMessage request, int id)
        {
            IEnumerable<string> headerValues = request.Headers.GetValues("Authentication");
            var authentication = headerValues.FirstOrDefault();
            try
            {
                if (authentication == token)
                {
                    if (notesDTO.delete(id))
                    {
                        return JSON.successToJson(Message.SUCCESSFUL_DELETE);
                    }
                    else
                    {
                        return JSON.errorToJson(Message.ERROR_DELETE);
                    }
                }
                else
                {
                    return JSON.errorToJson(Message.ERROR_SESSION_VALIDATION);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                return JSON.errorToJson(Message.ERROR_PROCESSING_DATA);
            }
        }
    }
}
