using System;
//using System.IO;
//using System.Text;
//using System.Linq;
//using System.Collections.Generic;
//using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
//using Microsoft.Xna.Framework.Input;
//using System.Diagnostics;
//using System.Reflection;
////using System.Xml.Serialization;


namespace Microsoft.Xna.Framework
{    
    /// <summary>
    /// A more detailed frameRate class.
    /// </summary>
    public class MgFrameRate
    {
        private Game gameRef;
        private GraphicsDeviceManager graphics;

        public Texture2D dotTexture;
        SpriteBatch spriteBatch;
        MgStringBuilder msg = new MgStringBuilder();
        MgStringBuilder fpsmsg = new MgStringBuilder();
        MgStringBuilder gcmsg = new MgStringBuilder();

        public bool DisplayFrameRate = true;
        public bool DisplayGarbageCollectionRate = true;
        public bool DisplayVisualizations = true;
        public double DisplayedMessageFrequency = 1.0d;
        public bool DisplayCollectionAlert = true;

        private const double MEGABYTE = 1048576d;

        private double fps = 0d;
        private double frames = 0;
        private double updates = 0;
        private double ufRatio = 1f;
        private double elapsed = 0;
        private double last = 0;
        private double now = 0;

        private double lastRanSlowly = 0d;
        private double numberOfSlowFrames = 0d;

        private long gcNow = 0;
        private long gcLast = 0;
        private long gcDiff = 0;
        private long gcAccumulatedSinceStart = 0;
        private long gcIncreasedSinceLastCollect = 0;
        private long gcCollectMemoryTotal = 0;
        private long gcSizeOfLastCollect = 0;
        private long gcRecordedCollects = 0;

        private int collectionColorAlertRedValue = 0;

        //private bool isFixed = false;
        //private bool vsync = false;
        private double desiredFramesPerSecond = 60;

        /// <summary>
        /// clears all the tracked garbage and resets counters to zero.
        /// </summary>
        public bool ClearCollectionTracking
        {
            set;
            get;
        }

        public void ClearCollectionTrackingAndGcCollect()
        {
            GC.Collect();
            ClearCollectionTracking = true;
        }

        public void LoadSetUp(Game pass_in_this, GraphicsDeviceManager gdm, SpriteBatch spriteBatch, bool fixedon, bool vsync, double desiredFramesPerSecond, bool displayGc)
        {
            gameRef = pass_in_this;
            gameRef.IsFixedTimeStep = fixedon;
            graphics = gdm;
            dotTexture = TextureDotCreate(pass_in_this.GraphicsDevice);
            this.spriteBatch = spriteBatch;
            this.desiredFramesPerSecond = desiredFramesPerSecond;
            //this.isFixed = fixedon;
            //this.vsync = vsync;
            if (fixedon)
            {
                //gameRef.TargetElapsedTime = TimeSpan.FromSeconds(1d / desiredFramesPerSecond);
                gameRef.TargetElapsedTime = TimeSpan.FromTicks((long)(TimeSpan.TicksPerSecond / desiredFramesPerSecond));
            }
            graphics.SynchronizeWithVerticalRetrace = vsync;
            graphics.ApplyChanges();
            DisplayGarbageCollectionRate = displayGc;
        }

        /// <summary>
        /// The msgFrequency here is the reporting time to update the message.
        /// </summary>
        public void Update(GameTime gameTime)
        {
            //now = gameTime.TotalGameTime.TotalSeconds; // TimeSpan.FromTicks(166666);
            now = (double)gameTime.TotalGameTime.Ticks / (double)TimeSpan.TicksPerSecond;
            elapsed = (double)(now - last);

            // is running slowly when applicable.
            if (gameTime.IsRunningSlowly)
            {
                var nowInMinutes = now / 60d;
                if (nowInMinutes < lastRanSlowly + .1)
                    numberOfSlowFrames++;
                else
                    numberOfSlowFrames = 1;
                lastRanSlowly = nowInMinutes;
            }

            // fps msg's
            if (elapsed >= DisplayedMessageFrequency) // || (gcDiff != 0)
            {
                // time
                if (DisplayFrameRate)
                {
                    fps = (frames / elapsed);
                    ufRatio = (float)frames / (float)updates;

                    fpsmsg.Clear();
                    if(gameRef.IsFixedTimeStep || graphics.SynchronizeWithVerticalRetrace)
                        fpsmsg.Append(" Fixed: ").Append(gameRef.IsFixedTimeStep).Append(" Vsync: ").Append(graphics.SynchronizeWithVerticalRetrace).AppendLine(); // total minutes ran 
                    fpsmsg.Append(" Minutes Running: ").AppendTrim(now / 60d, 3).AppendLine(); // total minutes ran                  
                    fpsmsg.Append(" Fps: ").AppendTrim(fps).AppendLine(); // frames per second or per messege frequency really though that is ussually set to 1 second.
                    fpsmsg.Append(" Draw to Update Ratio: ").AppendTrim(ufRatio).AppendLine(); // if your draws are lower then your updates you have a gpu bottleneck
                    fpsmsg.Append(" Msg Freq: ").AppendTrim(DisplayedMessageFrequency).Append(" Elapsed interval: ").AppendTrim(elapsed).AppendLine(); // this is the of the elapsed time that these stats were measured over.
                    fpsmsg.Append(" Interval error: ").Append(elapsed - DisplayedMessageFrequency).AppendLine(); // the expected measure and difference to it.
                    fpsmsg.Append(" Updates: ").Append(updates).AppendLine();
                    fpsmsg.Append(" Frames: ").Append(frames).AppendLine();
                    if (lastRanSlowly > 0d)
                        fpsmsg.Append(" Last Ran Slow At ").AppendTrim(lastRanSlowly).Append(" Duration: ").AppendTrim(numberOfSlowFrames).AppendLine();

                    frames = 0;
                    updates = 0;
                    last = now;
                }
                // Gc Messages
                if (DisplayGarbageCollectionRate)
                {
                    gcNow = GC.GetTotalMemory(false);
                    gcDiff = gcNow - gcLast;
                    gcLast = gcNow;

                    // give the app a little time to load and let the gc run a bit.
                    if (now < 6d || ClearCollectionTracking)
                    {
                        if (now > 5.9d && now < 6d)
                            ClearCollectionTrackingAndGcCollect();
                        gcSizeOfLastCollect = 0;
                        gcCollectMemoryTotal = 0;
                        gcRecordedCollects = 0;
                        gcAccumulatedSinceStart = 0;
                        gcIncreasedSinceLastCollect = 0;
                        gcLast = gcNow;
                        gcDiff = 0;
                        ClearCollectionTracking = false;
                    }

                    gcmsg.Clear();
                    gcmsg.Append(" GC Memory (Mb) Now: ").AppendTrim((double)gcNow / MEGABYTE).AppendLine();

                    if (gcDiff == 0)
                    {
                        gcmsg.AppendLine(" GC (Mb) No change");
                    }
                    if (gcDiff < 0)
                    {
                        gcmsg.Append(" !!! COLLECTION OCCURED !!! ").Append(" GC Memory(Mb) Lost: ").AppendTrim((double)(gcDiff) / MEGABYTE).AppendLine();
                        var lostMemoryNow = -gcDiff;
                        gcSizeOfLastCollect = lostMemoryNow;
                        gcCollectMemoryTotal += lostMemoryNow;
                        gcRecordedCollects++;
                        gcIncreasedSinceLastCollect = 0;
                        gcDiff = 0;
                        gcLast = gcNow;
                        collectionColorAlertRedValue = 255;
                    }
                    if (gcDiff > 0) // this is not a memory collection garbage has been created but its not neccessarily going to be collected or even a problem.
                    {
                        gcmsg.Append(" GC (Mb) Increase Now: ").AppendTrim((double)(gcDiff) / MEGABYTE).AppendLine();
                        gcAccumulatedSinceStart += gcDiff;
                        gcIncreasedSinceLastCollect += gcDiff;
                        gcLast = gcNow;
                    }
                    gcmsg.Append(" GC (Mb) Increase Since Last Collect: ").AppendTrim(gcIncreasedSinceLastCollect / MEGABYTE).AppendLine();
                    if (gcRecordedCollects > 0)
                    {
                        gcmsg.Append(" GC (Mb) Lost to Last Collection: ").AppendTrim(gcSizeOfLastCollect / MEGABYTE).AppendLine();
                        gcmsg.Append(" GC (Mb) Total Lost to Collections: ").AppendTrim(gcCollectMemoryTotal / MEGABYTE).AppendLine();
                    }
                    gcmsg.Append(" GC (Mb) Accumulated Since Start ").AppendTrim(gcAccumulatedSinceStart / MEGABYTE, 6).AppendLine();
                    gcmsg.Append(" GC Number Of Memory Collections: ").AppendTrim(gcRecordedCollects).AppendLine();
                }
                msg.Clear();
                msg.Append(fpsmsg).Append(gcmsg);
            }
            updates++;
        }

        public void DrawFps(SpriteBatch spritebatch, SpriteFont font, Vector2 fpsDisplayPosition, Color fpsTextColor)
        {
            if (DisplayVisualizations)
            {
                DrawVisualizations(fpsTextColor);
                fpsDisplayPosition.Y += 10;
            }
            spriteBatch = spritebatch;
            if (collectionColorAlertRedValue > fpsTextColor.R)
                collectionColorAlertRedValue -= 1;
            if (DisplayCollectionAlert && gcRecordedCollects > 0)
            {
                spriteBatch.DrawString(font, msg, fpsDisplayPosition, new Color(collectionColorAlertRedValue, fpsTextColor.G, fpsTextColor.B, fpsTextColor.A));
            }
            else
            {
                if (now < 6d)
                    spriteBatch.DrawString(font, msg, fpsDisplayPosition, new Color(92, 64, 64, 255) );
                else
                    spriteBatch.DrawString(font, msg, fpsDisplayPosition, new Color(fpsTextColor.R, fpsTextColor.G, fpsTextColor.B, fpsTextColor.A));
            }
            frames++;
        }

        private void DrawVisualizations(Color fpsTextColor)
        {
            // draw the visualization stutter bars to try to pick up on any stutter by eye when testing.
            var dist = 600d;
            var vmsrheight = 4;
            var vmsrWidth = 30;
            Rectangle visualMotionStutterRect = new Rectangle((int)(dist * elapsed) + 5, 2, vmsrWidth, vmsrheight);
            Rectangle visualMotionStutterRect2 = new Rectangle((int)(dist - (dist * elapsed)) + 5, 2, vmsrWidth, vmsrheight);
            DrawSquare(visualMotionStutterRect, 1, false, Color.LemonChiffon);
            DrawSquare(visualMotionStutterRect2, 1, false, Color.LemonChiffon);

            // draw the spinning ray this is for the same purpose as above.
            DrawRaySegment(new Vector2(15, 15), 15, 2, (float)((elapsed / DisplayedMessageFrequency) * Math.PI * 2d), Color.Gray);

            // gc memory visualization background rect.
            double visualMax = 100;
            Rectangle visualGcRect = new Rectangle(15, 12, (int)visualMax, 6);
            DrawSquare(visualGcRect, 1, true, Color.Moccasin);

            // bars uptop to show the memory change if it is occuring.
            Rectangle visualGcMemRect = new Rectangle();
            double gcmem  = (double)gcNow;
            double gctempmem = gcmem;
            int gcverylarge = 0;
            int gclarge = 0;
            int gcmed = 0;
            int gcsmall = 0;
            double tmptest = 1000000;
            if (gctempmem > tmptest)
            {
                gcverylarge = (int)(gctempmem / tmptest);
                gctempmem -= tmptest * gcverylarge;
                visualGcMemRect = new Rectangle(15, 13, (int)(gcverylarge), 1);
                DrawSquare(visualGcMemRect, 1, false, Color.Brown);
            }
            tmptest *= .01;
            if (gctempmem > tmptest)
            {
                gclarge = (int)(gctempmem / tmptest);
                gctempmem -= tmptest * gclarge;
                visualGcMemRect = new Rectangle(15, 14, (int)(gclarge), 1);
                DrawSquare(visualGcMemRect, 1, false, Color.Orange);
            }
            tmptest *= .01;
            if (gctempmem > tmptest)
            {
                gcmed = (int)(gctempmem / tmptest);
                gctempmem -= tmptest * gcmed;
                visualGcMemRect = new Rectangle(15, 15, (int)(gcmed), 1);
                DrawSquare(visualGcMemRect, 1, false, Color.Yellow);
            }
            tmptest *= .01;
            if (gctempmem > tmptest)
            {
                gcsmall = (int)(gctempmem / tmptest);
                gctempmem -= tmptest * gcsmall;
                visualGcMemRect = new Rectangle(15, 16, (int)(gcsmall), 1);
                DrawSquare(visualGcMemRect, 1, false, Color.Blue);
            }

            // increase in gc typically this goes up to about 2 mb max before a collection.
            var visualGcIncrease = (gcIncreasedSinceLastCollect / (MEGABYTE )) * 150;
            Rectangle visualGcIncreaseRect = new Rectangle(15, 18, (int)(visualGcIncrease), 2);
            DrawSquare(visualGcIncreaseRect, 1, true, Color.Red);
        }


        public static Texture2D TextureDotCreate(GraphicsDevice device)
        {
            Color[] data = new Color[1];
            data[0] = new Color(255, 255, 255, 255);
            return TextureFromColorArray(device, data, 1, 1);
        }
        public static Texture2D TextureFromColorArray(GraphicsDevice device, Color[] data, int width, int height)
        {
            Texture2D tex = new Texture2D(device, width, height);
            tex.SetData<Color>(data);
            return tex;
        }
        public void DrawSquare(Rectangle r, int lineThickness, bool drawOutline ,Color c)
        {
            if (drawOutline)
            {
                Rectangle TLtoR = new Rectangle(r.Left, r.Top, r.Width, lineThickness);
                Rectangle BLtoR = new Rectangle(r.Left, r.Bottom - lineThickness, r.Width, lineThickness);
                Rectangle LTtoB = new Rectangle(r.Left, r.Top, lineThickness, r.Height);
                Rectangle RTtoB = new Rectangle(r.Right - lineThickness, r.Top, lineThickness, r.Height);
                spriteBatch.Draw(dotTexture, TLtoR, c);
                spriteBatch.Draw(dotTexture, BLtoR, c);
                spriteBatch.Draw(dotTexture, LTtoB, c);
                spriteBatch.Draw(dotTexture, RTtoB, c);
            }
            else
                spriteBatch.Draw(dotTexture, r, c);
        }
        public void DrawRaySegment(Vector2 postion, int length, int linethickness, float rot, Color c)
        {
            rot += 3.141592f;
            Rectangle screendrawrect = new Rectangle((int)postion.X, (int)postion.Y, linethickness, length);
            spriteBatch.Draw(dotTexture, screendrawrect, new Rectangle(0, 0, 1, 1), c, rot, Vector2.Zero, SpriteEffects.None, 0);
        }
    }
}
