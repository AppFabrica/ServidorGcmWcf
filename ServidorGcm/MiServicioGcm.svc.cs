using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;

namespace ServidorGcm
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "MiServicioGcm" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select MiServicioGcm.svc or MiServicioGcm.svc.cs at the Solution Explorer and start debugging.
    public class MiServicioGcm : IMiServicioGcm
    {
        public string RegistroGcm(string imei, string registrationId)
        {
            String resultado = "OK";

            try
            {
                EjemploGcmDBEntities dataBase = new EjemploGcmDBEntities();
                RegistroGcm registroGcm = dataBase.RegistroGcm.Where(x => x.Imei == imei).FirstOrDefault();

                if(registroGcm == null)
                {
                    //Insertar nuevo registro
                    registroGcm = new RegistroGcm
                    {
                        Imei = imei, RegistrationToken = registrationId
                    };
                    dataBase.RegistroGcm.Add(registroGcm);
                }
                else
                {
                    //Actualizar registro
                    registroGcm.RegistrationToken = registrationId;
                }

                dataBase.SaveChanges();
            }
            catch (Exception ex)
            {
                resultado = "ERROR-" + ex.Message;
            };

            return resultado;
        }


        public string EnviaMensaje(string imei, string mensaje)
        {
            String respuesta = "OK";
            try
            {
                EjemploGcmDBEntities dataBase = new EjemploGcmDBEntities();
                RegistroGcm registroGcm = dataBase.RegistroGcm.Where(x => x.Imei == imei).FirstOrDefault();

                if (registroGcm == null)
                    throw new Exception( "No se encontro ningun dispositivo registrado conb el IMEI: " + imei);

                string registrationToken = registroGcm.RegistrationToken;

                string apiKey = "AIzaSyCuPuIp38d3OojqL6Utpjuok7RcpCkYGfs";
                WebRequest request = WebRequest.Create("https://gcm-http.googleapis.com/gcm/send");

                //Crear el cuerpo (body) de la soicitud
                string registration_id = registrationToken;
                string collapse_Key = DateTime.Now.ToString();

                //Ejemplo de mensaje: registration_id=123&collapse_key=321&data.mi_mensaje=Mensaje de prueba
                mensaje = string.Format("registration_id={0}&collapse_key={1}&data.mi_mensaje={2}",
                    registration_id,
                    collapse_Key,
                    mensaje);

                //Crear cabeceras (heders) de la solicitud: API Key y formato de la solicitud con texto plano
                request.ContentType = "application/x-www-form-urlencoded;charset=UTF-8";
                request.Headers.Add(HttpRequestHeader.Authorization, "key=" + apiKey);
                request.Method = "POST";
                request.ContentLength = mensaje.Length;

                //Enviar la solicitud
                using (StreamWriter oWriter = new StreamWriter(request.GetRequestStream()))
                    oWriter.Write(mensaje);

                //Obtener la respuesta
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                StreamReader stremReader = new StreamReader(response.GetResponseStream());

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    //El mensaje se procesó correctamente - HTTP 200

                    string linea1 = stremReader.ReadLine();
                    string linea2 = stremReader.ReadLine();

                    if (linea1.StartsWith("id"))
                    {
                        respuesta = "Mensaje enviado correctamente. Id mensaje: " + linea1;

                        //verificamos segunda linea
                        if (linea2 != null && linea2.StartsWith("registration_id"))
                        {
                            //Actualizamos registration token en la base de datos
                            respuesta = "Mensaje enviado correctamente. El id de registro cambio. Id mensaje: " + linea1 + "\n" + linea2;
                            
                            registroGcm.RegistrationToken = linea2.Split('=')[2];
                        }

                    }
                    if (linea1.StartsWith("Error"))
                    {
                        if (linea1 == "Error=InvalidRegistration")
                            throw new Exception( "Registration Token no valido. " + linea1);

                        if (linea1 == "Error=NotRegistered")
                        {
                            //La apicacion cliente se desinstalo del dispositivo o se desregistro de GCM.
                            throw new Exception( "Dispositivo no registrado. Elimine el registro de la base de datos" + linea1);
                        }
                    }
                }
                else
                {
                    switch (response.StatusCode)
                    {
                        case HttpStatusCode.BadRequest://400
                            respuesta = "La solicitud no fue entendida por el servidor.";
                            break;
                        case HttpStatusCode.Unauthorized://401
                            respuesta = "La API Key de la plicacion servidor no es valida.";
                            break;
                        case HttpStatusCode.NotFound://404
                            respuesta = "El recurso slicitado no existe, verifique la URL.";
                            break;
                        case HttpStatusCode.ServiceUnavailable://503
                            respuesta = "Servicio no disonible, intentes mas tarde.";
                            break;
                        default:
                            respuesta = "Error esconocido: " + response.StatusCode;
                            break;
                    }
                }

                stremReader.Close();
                response.Close();
            }
            catch(Exception ex)
            {
                respuesta = ex.Message;
            }

            return respuesta;


        }

    }
}
