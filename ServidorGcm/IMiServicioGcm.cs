using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;

namespace ServidorGcm
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IMiServicioGcm" in both code and config file together.
    [ServiceContract]
    public interface IMiServicioGcm
    {
        [OperationContract]
        [WebGet(ResponseFormat = WebMessageFormat.Json, BodyStyle=WebMessageBodyStyle.WrappedResponse)]
        string RegistroGcm(string imei, string registrationId);

        [OperationContract]
        [WebGet(ResponseFormat = WebMessageFormat.Json, BodyStyle = WebMessageBodyStyle.WrappedResponse)]
        string EnviaMensaje(string imei, string mensaje);
    }
}
