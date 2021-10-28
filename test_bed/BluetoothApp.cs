using System;
using Android.App;
using Android.Content;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Android.OS;
using Android.Bluetooth;


using System.Linq;



namespace Backlight
{
    [Activity(Label = "BluetoothApp", MainLauncher = true, Icon = "@drawable/icon")]
    public class MainActivity : Activity
    {
       
        BluetoothConnection myConnection = new BluetoothConnection();
    

        protected override void OnCreate(Bundle bundle)
        {
            base.OnCreate(bundle);

            // Set our view from the "main" layout resource
            SetContentView(Resource.Layout.Main);

            // Get our button from the layout resource,
            // and attach an event to it
            Button buttonConnect = FindViewById<Button>(Resource.Id.button1) ;
            Button buttonDisconnect = FindViewById<Button>(Resource.Id.button2);

            Button button1On = FindViewById<Button>(Resource.Id.button3);
            Button button2On = FindViewById<Button>(Resource.Id.button4);

            Button button3On = FindViewById<Button>(Resource.Id.button5);
            Button buttonOff= FindViewById<Button>(Resource.Id.button6);

            SeekBar brightness = FindViewById<SeekBar>(Resource.Id.seekBar1);

            SeekBar rot = FindViewById<SeekBar>(Resource.Id.seekBar2);

            SeekBar blau = FindViewById<SeekBar>(Resource.Id.seekBar4);

            SeekBar gelb = FindViewById<SeekBar>(Resource.Id.seekBar3);

            TextView connected = FindViewById<TextView>(Resource.Id.textView1);

      


            
           // buttonDisconnect.Enabled = false;

            BluetoothSocket _socket = null;

            System.Threading.Thread listenThread = new System.Threading.Thread(listener);
            listenThread.Abort();

            buttonConnect.Click += delegate {

                //DISCONNECT CLOSE
                //try {
                //    buttonDisconnect.Enabled = false;
                //    buttonConnect.Enabled = true;
                //    listenThread.Abort();

                //    myConnection.thisDevice.Dispose();

                //    myConnection.thisSocket.OutputStream.WriteByte(187);
                //    myConnection.thisSocket.OutputStream.Close();

                //    myConnection.thisSocket.Close();

                //    myConnection = new BluetoothConnection();
                //    _socket = null;

                //    connected.Text = "Disconnected!";
                //}
                //catch { }

                ////////////////////////////////////////////////
                listenThread.Start();

                myConnection = new BluetoothConnection();
                //myConnection.thisSocket = null;
                //_socket = null;

            myConnection.getAdapter();

            myConnection.thisAdapter.StartDiscovery();

            try
            { 
                    
                myConnection.getDevice();
                     myConnection.thisDevice.SetPairingConfirmation(false);
                  //   myConnection.thisDevice.Dispose();
                    myConnection.thisDevice.SetPairingConfirmation(true); 
                    myConnection.thisDevice.CreateBond();


            }
            catch(Exception deviceEX)
            {
            }

                myConnection.thisAdapter.CancelDiscovery();


            _socket = myConnection.thisDevice.CreateRfcommSocketToServiceRecord(Java.Util.UUID.FromString("00001101-0000-1000-8000-00805f9b34fb"));

            myConnection.thisSocket = _socket;

         //   System.Threading.Thread.Sleep(500);
                   try
                    {

              

                            myConnection.thisSocket.Connect();

                            connected.Text = "Connected!";
                            buttonDisconnect.Enabled = true;
                            buttonConnect.Enabled = false;

                            if (listenThread.IsAlive == false)
                            {
                                listenThread.Start();
                            }
                            //else
                            //{
                            //    listenThread.Abort();
                            //}

                   }
                         catch (Exception CloseEX)
                    {
                       
                    }


            };

            buttonDisconnect.Click += delegate {

                try {
                    //  buttonDisconnect.Enabled = false;
                    buttonConnect.Enabled = true;
                    listenThread.Abort();

                    myConnection.thisDevice.Dispose();

                    myConnection.thisSocket.OutputStream.WriteByte(187);
                    myConnection.thisSocket.OutputStream.Close();

                    myConnection.thisSocket.Close();

                    myConnection = new BluetoothConnection();
                    _socket = null;

                    connected.Text = "Disconnected!";
                }
                catch  { }
            };
                    
                    

                
                button1On.Click +=delegate{
                
                
                
                   //byte[] Test = new byte[5];
                   // Test[0] = 72; //H
                   // Test[1] = 101;//e
                   // Test[2] = 108;//l
                   // Test[3] = 108;//l
                   // Test[4] = 111;//o
                    try
                    {
                 
                     //   myConnection.thisSocket.OutputStream.Write(Test, 0, Test.Length);
                        myConnection.thisSocket.OutputStream.WriteByte(1);
                        myConnection.thisSocket.OutputStream.WriteByte(1);
                        myConnection.thisSocket.OutputStream.WriteByte(1);
                        myConnection.thisSocket.OutputStream.Close();



                      
                    }
                    catch (Exception outPutEX)
                    {

                    }
         
                
                };

            button2On.Click += delegate {



                //byte[] Test = new byte[5];
                // Test[0] = 72; //H
                // Test[1] = 101;//e
                // Test[2] = 108;//l
                // Test[3] = 108;//l
                // Test[4] = 111;//o
                try
                {

                    //   myConnection.thisSocket.OutputStream.Write(Test, 0, Test.Length);
                    myConnection.thisSocket.OutputStream.WriteByte(2);
                    myConnection.thisSocket.OutputStream.WriteByte(2);
                    myConnection.thisSocket.OutputStream.WriteByte(2);
                    myConnection.thisSocket.OutputStream.Close();




                }
                catch (Exception outPutEX)
                {

                }


            };

            button3On.Click += delegate {



                //byte[] Test = new byte[5];
                // Test[0] = 72; //H
                // Test[1] = 101;//e
                // Test[2] = 108;//l
                // Test[3] = 108;//l
                // Test[4] = 111;//o
                try
                {

                    //   myConnection.thisSocket.OutputStream.Write(Test, 0, Test.Length);
                    myConnection.thisSocket.OutputStream.WriteByte(3);
                    myConnection.thisSocket.OutputStream.WriteByte(3);
                    myConnection.thisSocket.OutputStream.WriteByte(3);
                    myConnection.thisSocket.OutputStream.Close();




                }
                catch (Exception outPutEX)
                {

                }


            };



            buttonOff.Click += delegate {



                //byte[] Test = new byte[5];
                //Test[0] = 72; //H
                //Test[1] = 101;//e
                //Test[2] = 108;//l
                //Test[3] = 108;//l
                //Test[4] = 111;//o
                try
                {

                    //   myConnection.thisSocket.OutputStream.Write(Test, 0, Test.Length);
                    myConnection.thisSocket.OutputStream.WriteByte(0);
                    myConnection.thisSocket.OutputStream.WriteByte(0);
                    myConnection.thisSocket.OutputStream.WriteByte(0);
                    myConnection.thisSocket.OutputStream.Close();




                }
                catch (Exception outPutEX)
                {

                }


            };


            brightness.ProgressChanged += (object sender, SeekBar.ProgressChangedEventArgs e) => {
                if (e.FromUser)
                {
                    try
                    {
                        if (e.Progress % 2 == 0)
                        {
                            myConnection.thisSocket.OutputStream.WriteByte(Convert.ToByte(e.Progress + 15));
                            myConnection.thisSocket.OutputStream.Close();
                            System.Threading.Thread.Sleep(10);
                        }
                    }
                    catch { }
                }
            };

            rot.ProgressChanged += (object sender, SeekBar.ProgressChangedEventArgs e) => {
                if (e.FromUser)
                {
                    try
                    {
                        if (e.Progress % 2 == 0)
                        {
                            myConnection.thisSocket.OutputStream.WriteByte(Convert.ToByte(e.Progress + 2000));
                            myConnection.thisSocket.OutputStream.Close();
                            System.Threading.Thread.Sleep(10);
                        }
                    }
                    catch { }
                }
            };
            blau.ProgressChanged += (object sender, SeekBar.ProgressChangedEventArgs e) => {
                if (e.FromUser)
                {
                    try
                    {
                        if (e.Progress % 2 == 0)
                        {
                            myConnection.thisSocket.OutputStream.WriteByte(Convert.ToByte(e.Progress + 4000));
                            myConnection.thisSocket.OutputStream.Close();
                            System.Threading.Thread.Sleep(10);
                        }
                    }
                    catch { }
                }
            };

           gelb.ProgressChanged += (object sender, SeekBar.ProgressChangedEventArgs e) => {
                if (e.FromUser)
                {
                    try
                    {
                        if (e.Progress % 2 == 0)
                        {
                            myConnection.thisSocket.OutputStream.WriteByte(Convert.ToByte(e.Progress + 3000));
                            myConnection.thisSocket.OutputStream.Close();
                            System.Threading.Thread.Sleep(10);
                        }
                    }
                    catch { }
                }
            };
        }

            void listener(){
                byte[] read = new byte[1];

                TextView readTextView = FindViewById<TextView>(Resource.Id.textView2);
                //DateTime onTime = DateTime.Now;
                //DateTime offTime = DateTime.Now;
                //DateTime thisTime;
                //TimeSpan thisSpan;

                //int timeSetOn = 0;
                //int timeSetOff = 0;

                TextView timeTextView = FindViewById<TextView>(Resource.Id.textView3);
                while (true)
                {

                    //thisTime = DateTime.Now;

                    
                    try
                    {

                        myConnection.thisSocket.InputStream.Read(read, 0, 1);
                        myConnection.thisSocket.InputStream.Close();
                        RunOnUiThread(() =>
                        {
                          
                            if (read[0] == 1)
                            {
                              
                                readTextView.Text = "Relais AN";

                                //if (timeSetOn == 0)
                                //{
                                //    onTime = DateTime.Now;
                                //    timeSetOn = 1;
                                    
                                //}
                               

                            }
                            else if (read[0] == 0)
                            {
                                readTextView.Text = "Relais AUS";
                                //timeSetOn = 0;

                                timeTextView.Text = "";
                            }

                            //if (timeSetOn == 1)
                            //{
                            //    thisSpan = thisTime-onTime;
                            //    timeTextView.Text = thisSpan.Minutes + ":" + thisSpan.Seconds;
                            //}
                       
                        });
                    }
                    catch { }
                  
                }
            }

            


    


        }


       

        public class BluetoothConnection
        {

            public void getAdapter() { this.thisAdapter = BluetoothAdapter.DefaultAdapter; }
            public void getDevice() { this.thisDevice = (from bd in this.thisAdapter.BondedDevices where bd.Name == "HC-05" select bd).FirstOrDefault(); }

            public BluetoothAdapter thisAdapter { get; set; }
            public BluetoothDevice thisDevice   { get; set; }

            public BluetoothSocket thisSocket { get; set; }



        }
    
}

