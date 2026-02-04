using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ServicioHora
{
    public partial class ServicioHora : ServiceBase
    {
        private Thread servicio;
        public ServicioHora()
        {
            InitializeComponent();
        }


        public void WriteEvent(string mensaje)
        {
            try
            {
                const string nombre = "ServicioHora";
                EventLog.WriteEntry(nombre, mensaje);
            }
            catch (Exception ex)
            {
                StreamWriter srp = new StreamWriter(Environment.ExpandEnvironmentVariables("%PROGRAMDATA%\\servicioHora_log.txt"), true);
                srp.WriteLine(String.Format("[ERROR]: {0} ({1})", DateTime.Now, ex.Message));
                srp.Close();
            }
          
        }

        protected override void OnStart(string[] args)
        {
            servicio = new Thread(InitServer);
            servicio.IsBackground = true;
            servicio.Start();
        }

        protected override void OnStop()
        {
            ServerRunning = false;
            base.OnStop();
        }


        public bool ServerRunning { set; get; } = true;
        public int Port;
        public int alt { get; } = 31416;
        private Socket s;
        bool buscaPuerto = false;
        public void InitServer()
        {

            string nombre = "ServicioHora"; // Nombre de la fuente de eventos.
            string logDestino = "Application"; // Log del visor de eventos donde aparece
                                               // Si es la primera vez que se escribe hay que crear
                                               // la fuente del mensaje (internamente es un diccionario)
            if (!EventLog.SourceExists(nombre))
            {
                // Requiere permisos de administrador que daremos
                // durante la instalación del servicio
                EventLog.CreateEventSource(nombre, logDestino);
            }

            int i = 0;

            IPEndPoint ie = null;
            s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            try
            {
                StreamReader srp = new StreamReader(Environment.ExpandEnvironmentVariables("%PROGRAMDATA%\\dataConfig.txt"));
                Port = int.Parse(srp.ReadToEnd());
                srp.Close();
                ie = new IPEndPoint(IPAddress.Any, Port);
                s.Bind(ie);
            }
            catch (DirectoryNotFoundException e)
            {
                WriteEvent("No se ha podido acceder a la ruta del archivo de configuracion, se usará la lista por defecto...");
                buscaPuerto = !buscaPuerto;
            }
            catch (FileNotFoundException e)
            {
                WriteEvent("No se ha encontrado el archivo de configuración de puertos, se usará la lista por defecto...");
                buscaPuerto = !buscaPuerto;
            } catch(FormatException e)
            {
                WriteEvent("El formato del archivo de configuración es incorrecto, se usará la lista por defecto...");
                buscaPuerto = true;
            }

            


      




            if (buscaPuerto)
            {
                try
                {
                    ie = new IPEndPoint(IPAddress.Any, alt);
                    s.Bind(ie);
                    buscaPuerto = !buscaPuerto;
                }
                catch (SocketException e) when (e.ErrorCode == (int)SocketError.AddressAlreadyInUse)
                {
                    try
                    {
                        ie = new IPEndPoint(IPAddress.Any, alt);

                    }
                    catch (SocketException ex) when (e.ErrorCode == (int)SocketError.AddressAlreadyInUse)
                    {
                        buscaPuerto = !buscaPuerto;
                    }
                   
                }

            }



            if (!buscaPuerto)
            {
                WriteEvent($"Servedor iniciado, escuchando en {ie.Address}:{ie.Port}");
                
                while (ServerRunning)
                {
                    s.Listen(1);

                    try
                    {
                        Socket client = s.Accept();

                        Thread hilo = new Thread(() => ClientDispatcher(client));
                        hilo.IsBackground = true;
                        hilo.Start();
                    }
                    catch (System.Net.Sockets.SocketException)
                    {
                        WriteEvent("Servidor cerrado");
                    }



                }
                if (!ServerRunning)
                {
                    s.Close();
                }


            }
            else
            {
                WriteEvent("No se ha podido iniciar el servidor en ninguno de los puertos indicados. Deteniendo el servicio...");
            }
        }




        private void ClientDispatcher(Socket sClient)
        {
            using (sClient)
            {
                IPEndPoint ieClient = (IPEndPoint)sClient.RemoteEndPoint;

                Encoding codificacion = Console.OutputEncoding;
                using (NetworkStream ns = new NetworkStream(sClient))
                using (StreamReader sr = new StreamReader(ns, codificacion))
                using (StreamWriter sw = new StreamWriter(ns, codificacion))
                {
                    sw.AutoFlush = true;
                    string welcome = "Bienvenido al servidor de fecha y hora";
                    sw.WriteLine(welcome);
                    string msg = "";
                    try
                    {
                        if (ServerRunning)
                        {
                            msg = sr.ReadLine().Trim();

                            StreamWriter srp = new StreamWriter(Environment.ExpandEnvironmentVariables("%PROGRAMDATA%\\servicioHora_log.txt"), true);
                            srp.WriteLine(String.Format("[{0} - @{1}:{2}]: {3}", DateTime.Now, ieClient.Address, ieClient.Port, msg));
                            srp.Close();


                            if (msg != null)
                            {

                                if (msg == "time")
                                {

                                    Console.WriteLine($"El cliente pide la hora");
                                    sw.WriteLine(String.Format("Son las {0}:{1} y {2} segundos", DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second));
                                }
                                else if (msg == "date")
                                {

                                    Console.WriteLine($"El cliente pide la fecha");
                                    sw.WriteLine(String.Format("Hoy es día {0} del {1} del año {2}", DateTime.Now.Day, DateTime.Now.Month, DateTime.Now.Year));
                                }
                                else if (msg == "all")
                                {

                                    Console.WriteLine($"El cliente pide fecha y hora");
                                    sw.WriteLine(String.Format("Son las {0}:{1}:{2} del día {3}/{4}/{5}", DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second, DateTime.Now.Day, DateTime.Now.Month, DateTime.Now.Year));
                                }
                                else
                                {
                                    WriteEvent("El cliente ha intetado usar \"" + msg + "\"");
                                    sw.WriteLine("No se reconoce el comando");
                                }
                                sClient.Close();
                            }
                        }
                    
                    }
                    catch (IOException)
                    {
                        msg = null;
                    }



                    Console.WriteLine("Cliente desconectado.\nConexión cerrada");
                }
            }
        }
    }
}
    

