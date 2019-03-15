using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;

namespace Utilities
{
    public class JSON
    {
        private static Dictionary<String, Object> aJson(Boolean status,Object obj)
        {
            try
            {
                Dictionary<String, Object> d = new Dictionary<String, Object>();
                d.Add("result", obj);
                d.Add("status", status);
                return d;
            }catch(Exception ex)
            {
                Debug.WriteLine(ex.Message);
                Debug.WriteLine(ex.StackTrace);
                return null;
            }
        }

        public static Dictionary<String, Object> errorToJson(String message)
        {
            return aJson(false, message);
        }

        public static Dictionary<String, Object> successToJson(Object obj)
        {
            return aJson(true, obj);
        }
    }
}
