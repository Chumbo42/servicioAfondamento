using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;


namespace ServidorHora
{
    public class ServidorDt//  Cierre socketo de servidor.  
    {
        public bool ServerRunning { set; get; } = true;
        public int Port;
        public int[] alt { get; } = { 31416, 1234, 2345, 5678 };
        private Socket s;
        public void InitServer()
        {
            int i = 0;

            
            
                using (StreamReader srp = new StreamReader("%PROGRAMDATA%\\dataConfig"))
                {
                    int.TryParse(srp.ReadLine(), out Port);
                }
            
            
            IPEndPoint ie = new IPEndPoint(IPAddress.Any, Port);


            s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            bool buscaPuerto = true;


            while (buscaPuerto)
            {
                try
                {
                    s.Bind(ie);
                    buscaPuerto = !buscaPuerto;
                }
                catch (SocketException e) when (e.ErrorCode == (int)SocketError.AddressAlreadyInUse)
                {
                    Console.WriteLine("El puerto " + Port + " ya está en uso");
                    if (i < alt.Length)
                    {
                        Console.WriteLine("Probando otra opción...");
                        Port = alt[i];
                        ie = new IPEndPoint(IPAddress.Any, Port);
                        i++;
                    }
                    else
                    {
                        buscaPuerto = !buscaPuerto;

                    }

                }
            }



            if (!buscaPuerto)
            {
                Console.WriteLine($"Servidor iniciado. " + $"Escuchando en {ie.Address}:{ie.Port}");
                Console.WriteLine("Esperando conexiones... (Ctrl+C para salir)");



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
                    catch(System.Net.Sockets.SocketException)
                    {
                        Console.WriteLine("Servidor cerrado");
                    }
                    


                }
                if (!ServerRunning)
                {
                    s.Close();
                }


            }


        }

        private void ClientDispatcher(Socket sClient)
        {
            using (sClient)
            {
                IPEndPoint ieClient = (IPEndPoint)sClient.RemoteEndPoint;
                Console.WriteLine($"Cliente conectado:{ieClient.Address} " + $"en puerto {ieClient.Port}");
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
                            if (!sr.EndOfStream)
                            {
                                msg = sr.ReadLine().Trim();
                            }




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
                                Console.WriteLine("El cliente ha intetado usar \"" + msg + "\"");
                                sw.WriteLine("No se reconoce el comando");
                            }
                            sClient.Close();
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

