namespace Microsoft.Samples.Kinect.DiscreteGestureBasics
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Kinect;    
    using Microsoft.Kinect.VisualGestureBuilder;    
    using mmisharp;

    public class GestureDetector : IDisposable
    {
                
        /// <summary> Path to the gesture database that was trained with VGB </summary>
        private readonly string gestureDatabase = @"Database\Gestures.gbd";

        /// <summary> Name of the discrete gesture in the database that we want to track </summary>
        private readonly string crouch_gesture = "CrouchContinuous";
        private readonly string dab_gesture = "Dab";
        private readonly string hey_gesture = "Hey_Right";
        private readonly string hold_gesture = "Hold_Right";
        private readonly string reload_gesture = "Reload_Right";
        

        /// <summary> Gesture frame source which should be tied to a body tracking ID </summary>
        private VisualGestureBuilderFrameSource vgbFrameSource = null;

        /// <summary> Gesture frame reader which will handle gesture events coming from the sensor </summary>
        private VisualGestureBuilderFrameReader vgbFrameReader = null;        

        private LifeCycleEvents lce;
        private MmiCommunication mmic;

        private int hysterisis;



        public GestureDetector(KinectSensor kinectSensor, GestureResultView gestureResultView)
        {

            //init LifeCycleEvents..
            lce = new LifeCycleEvents("GESTURES", "FUSION", "gestures-1", "acoustic", "command"); // LifeCycleEvents(string source, string target, string id, string medium, string mode)
            //mmic = new MmiCommunication("localhost",9876,"User1", "ASR");  //PORT TO FUSION - uncomment this line to work with fusion later
            mmic = new MmiCommunication("localhost", 8000, "User1", "GESTURES"); // MmiCommunication(string IMhost, int portIM, string UserOD, string thisModalityName)
            mmic.Send(lce.NewContextRequest());


            

            if (kinectSensor == null)
            {
                throw new ArgumentNullException("kinectSensor");
            }

            if (gestureResultView == null)
            {
                throw new ArgumentNullException("gestureResultView");
            }
            
            this.GestureResultView = gestureResultView;
            
            // create the vgb source. The associated body tracking ID will be set when a valid body frame arrives from the sensor.
            this.vgbFrameSource = new VisualGestureBuilderFrameSource(kinectSensor, 0);
            this.vgbFrameSource.TrackingIdLost += this.Source_TrackingIdLost;

            // open the reader for the vgb frames
            this.vgbFrameReader = this.vgbFrameSource.OpenReader();
            if (this.vgbFrameReader != null)
            {
                this.vgbFrameReader.IsPaused = true;
                this.vgbFrameReader.FrameArrived += this.Reader_GestureFrameArrived;
            }

            // load the 'Seated' gesture from the gesture database
            using (VisualGestureBuilderDatabase database = new VisualGestureBuilderDatabase(this.gestureDatabase))
            {
                // we could load all available gestures in the database with a call to vgbFrameSource.AddGestures(database.AvailableGestures), 
                // but for this program, we only want to track one discrete gesture from the database, so we'll load it by name
                foreach (Gesture gesture in database.AvailableGestures)
                {
                    if (gesture.Name.Equals(this.crouch_gesture) ||
                        gesture.Name.Equals(this.dab_gesture) ||
                        gesture.Name.Equals(this.hey_gesture) ||
                        gesture.Name.Equals(this.hold_gesture) ||
                        gesture.Name.Equals(this.reload_gesture))
                    {
                        this.vgbFrameSource.AddGesture(gesture);
                    }
                }
            }
        }

        // Handle the TrackingIdLost event for the VisualGestureBuilderSource object
        private void Source_TrackingIdLost(object sender, TrackingIdLostEventArgs e)
        {
            // Update the GestureResultView object to show the 'Not Tracked' image in the UI
            GestureResultView.UpdateGestureResult(false, false, false, false, false, false, false, 0.0f);
        }

        /// <summary> Gets the GestureResultView object which stores the detector results for display in the UI </summary>
        public GestureResultView GestureResultView { get; private set; }

        /// <summary>
        /// Gets or sets the body tracking ID associated with the current detector
        /// The tracking ID can change whenever a body comes in/out of scope
        /// </summary>
        public ulong TrackingId
        {
            get
            {
                return this.vgbFrameSource.TrackingId;
            }

            set
            {
                if (this.vgbFrameSource.TrackingId != value)
                {
                    this.vgbFrameSource.TrackingId = value;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether or not the detector is currently paused
        /// If the body tracking ID associated with the detector is not valid, then the detector should be paused
        /// </summary>
        public bool IsPaused
        {
            get
            {
                return this.vgbFrameReader.IsPaused;
            }

            set
            {
                if (this.vgbFrameReader.IsPaused != value)
                {
                    this.vgbFrameReader.IsPaused = value;
                }
            }
        }

        /// <summary>
        /// Disposes all unmanaged resources for the class
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes the VisualGestureBuilderFrameSource and VisualGestureBuilderFrameReader objects
        /// </summary>
        /// <param name="disposing">True if Dispose was called directly, false if the GC handles the disposing</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (this.vgbFrameReader != null)
                {
                    this.vgbFrameReader.FrameArrived -= this.Reader_GestureFrameArrived;
                    this.vgbFrameReader.Dispose();
                    this.vgbFrameReader = null;
                }

                if (this.vgbFrameSource != null)
                {
                    this.vgbFrameSource.TrackingIdLost -= this.Source_TrackingIdLost;
                    this.vgbFrameSource.Dispose();
                    this.vgbFrameSource = null;
                }
            }
        }

        /// <summary>
        /// Handles gesture detection results arriving from the sensor for the associated body tracking Id
        /// </summary>
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        /// 

        private void Reader_GestureFrameArrived(object sender, VisualGestureBuilderFrameArrivedEventArgs e)
        {
            VisualGestureBuilderFrameReference frameReference = e.FrameReference;
            using (VisualGestureBuilderFrame frame = frameReference.AcquireFrame())
            {
                bool isGestureDetected = false;
                bool iscrouched = false;
                bool isdabbing = false;
                bool isheying = false;
                bool isholding = false;                
                bool isreloading = false;

                float level = 0;

                if (frame != null)
                {
                    // get the discrete gesture results which arrived with the latest frame
                    IReadOnlyDictionary<Gesture, DiscreteGestureResult> discreteResults = frame.DiscreteGestureResults;
                    IReadOnlyDictionary<Gesture, ContinuousGestureResult> continuousResults = frame.ContinuousGestureResults;

                    if (discreteResults != null)
                    {
                        
                    }
                    if (continuousResults != null)
                    {

                        foreach (Gesture gesture in vgbFrameSource.Gestures)
                        {
                            
                            if (gesture.Name.Equals(this.crouch_gesture) ||
                                gesture.Name.Equals(this.dab_gesture) ||
                                gesture.Name.Equals(this.hey_gesture) ||
                                gesture.Name.Equals(this.hold_gesture) ||
                                gesture.Name.Equals(this.reload_gesture))
                            {
                                {
                                    ContinuousGestureResult result = null;
                                    continuousResults.TryGetValue(gesture, out result);

                                    if (result != null)
                                    {
                                        level = result.Progress;
                                        if (level >= 0.8)
                                        {
                                            hysterisis++;
                                            if (hysterisis != 20)
                                            {
                                                return;
                                            }
                                            hysterisis = 0;
                                            
                                            ((MainWindow)System.Windows.Application.Current.MainWindow).overlay_crouch.Opacity = 0.0;
                                            ((MainWindow)System.Windows.Application.Current.MainWindow).overlay_dab.Opacity = 0.0;
                                            ((MainWindow)System.Windows.Application.Current.MainWindow).overlay_hey.Opacity = 0.0;
                                            ((MainWindow)System.Windows.Application.Current.MainWindow).overlay_hold.Opacity = 0.0;
                                            ((MainWindow)System.Windows.Application.Current.MainWindow).overlay_reload.Opacity = 0.0;
                                            ((MainWindow)System.Windows.Application.Current.MainWindow).overlay_normal.Opacity = 0.0;



                                            if (gesture.Name.Equals(crouch_gesture))
                                            {
                                                sre_GestureRecognized(level, "CROUCH");
                                                Console.WriteLine(" CROUCH ");
                                                isGestureDetected = true;
                                                iscrouched = true;
                                                isdabbing = false;
                                                isheying = false;
                                                isholding = false;
                                                isreloading = false;
                                                // Never do this, use MVVM, only used this because there's no time left

                                                ((MainWindow)System.Windows.Application.Current.MainWindow).current_gesture.Text = "Crouch";
                                                ((MainWindow)System.Windows.Application.Current.MainWindow).overlay_crouch.Opacity = 0.5;


                                            }
                                            else if (gesture.Name.Equals(dab_gesture))
                                            {

                                               
                                                sre_GestureRecognized(level, "DAB");
                                                Console.WriteLine(" DAB ");
                                                isGestureDetected = true;
                                                iscrouched = false;
                                                isdabbing = true;
                                                isheying = false;
                                                isholding = false;
                                                isreloading = false;

                                                ((MainWindow)System.Windows.Application.Current.MainWindow).current_gesture.Text = "Dab";
                                                ((MainWindow)System.Windows.Application.Current.MainWindow).overlay_dab.Opacity = 0.5;


                                            }
                                            else if (gesture.Name.Equals(hey_gesture))
                                            {
                                                sre_GestureRecognized(level, "HEY");
                                                Console.WriteLine(" HEY ");
                                                isGestureDetected = true;
                                                iscrouched = false;
                                                isdabbing = true;
                                                isheying = false;
                                                isholding = false;
                                                isreloading = false;

                                                ((MainWindow)System.Windows.Application.Current.MainWindow).current_gesture.Text = "Hey";
                                                ((MainWindow)System.Windows.Application.Current.MainWindow).overlay_hey.Opacity = 0.5;

                                            }
                                            else if (gesture.Name.Equals(hold_gesture))
                                            {
                                                sre_GestureRecognized(level, "HOLD");
                                                Console.WriteLine(" HOLD ");
                                                isGestureDetected = true;
                                                iscrouched = false;
                                                isdabbing = true;
                                                isheying = false;
                                                isholding = false;
                                                isreloading = false;

                                                ((MainWindow)System.Windows.Application.Current.MainWindow).current_gesture.Text = "Hold";
                                                ((MainWindow)System.Windows.Application.Current.MainWindow).overlay_hold.Opacity = 0.5;

                                            }
                                            else if (gesture.Name.Equals(reload_gesture))
                                            {
                                                sre_GestureRecognized(level,"RELOAD");
                                                Console.WriteLine(" RELOAD ");
                                                isGestureDetected = true;
                                                iscrouched = false;
                                                isdabbing = true;
                                                isheying = false;
                                                isholding = false;
                                                isreloading = false;

                                                ((MainWindow)System.Windows.Application.Current.MainWindow).current_gesture.Text = "Reload";
                                                ((MainWindow)System.Windows.Application.Current.MainWindow).overlay_reload.Opacity = 0.5;

                                            }
                                            

                                        } 
                                    }
                                }
                            }                            
                        }
                        GestureResultView.UpdateGestureResult(true, isGestureDetected, iscrouched, isdabbing,isheying, isholding, isreloading, level);                       
                    }
                    
                }
                
            }

        }

       

        // Send JSON message indicating the parameters in use
        private void sre_GestureRecognized(double confidence, string gesture)
        {
            string json = "{ \"recognized\": [";
            json += "\"" + confidence + "\", ";
            json += "\"" + gesture + "\", ";
            // Just using the first two comands. The rest is EMP
            for (int i = 0; i < 8; i++)
            {
                json += "\"" + "EMP" + "\", ";
            }
            json = json.Substring(0, json.Length - 2);
            json += "] }";
            var exNot = lce.ExtensionNotification("", "", 1, json);
            mmic.Send(exNot);
        }
    }
}
