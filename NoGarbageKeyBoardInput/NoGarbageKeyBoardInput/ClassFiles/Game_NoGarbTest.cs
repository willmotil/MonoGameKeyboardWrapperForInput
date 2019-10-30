using System;
using System.Collections.Generic;

using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace NoGarbageKeyBoardInput
{
    public class Game_NoGarbTest : Game
    {
        GraphicsDeviceManager graphics;
        SpriteBatch spriteBatch;
        Texture2D dotTexture;
        MgFrameRate frameRate;

        MgStringBuilder msg = new MgStringBuilder(40);
        MgStringBuilder msg2 = new MgStringBuilder(200);

        public Game_NoGarbTest()
        {
            graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            Window.AllowUserResizing = true;
            IsMouseVisible = true;
            graphics.PreferredBackBufferWidth = 1100;
            graphics.PreferredBackBufferHeight = 700;
        }

        protected override void Initialize()
        {

            base.Initialize();
        }

        protected override void LoadContent()
        {
            // Create a new SpriteBatch, which can be used to draw textures.
            spriteBatch = new SpriteBatch(GraphicsDevice);
            // pull in textures.
            dotTexture = CreateTextureDot(GraphicsDevice);
            Asset.LoadFrom_Content(Content);
            Asset.font_MgGenFont.DefaultCharacter = '?';

            frameRate = new MgFrameRate();
            frameRate.LoadSetUp(this, graphics, spriteBatch, true, false, 60, true);

            // initialize text input and key command input. 
            // this class process multiple key combinations up to 3.
            KeysAndTextInput.Initialize();

            // example, this is done once.
            KeysAndTextInput.AddMethodsToRaiseOnKeyBoardActivity(OnTextInput, OnKeyCommandsInput, this);
            // disable or enable like so prevents garbage collections from occuring.
            KeysAndTextInput.EnableKeyCommandsInput(this);
            KeysAndTextInput.EnableTextInput(this);

            // to visually test and see the above class in practical use.
            // don't have to save the refernce this is a static instance.
            new VisualKeyboardDisplay();
            // record characters.
            VisualKeyboardDisplay.RecordTypedCharacters = true;
            VisualKeyboardDisplay.DisplayRecordedTypedCharacters = true;
            VisualKeyboardDisplay.DisplayKeypressText = true;
        }

        /// <summary> 
        /// Raised only when valid text input is occuring and this instance is enabled.
        /// Command style key input prevents sending.
        /// </summary>
        public void OnTextInput(char character, Keys key)
        {
            if (character == '\t')
                msg.Append("    ");
            else
            {
                if (key == Keys.Back)
                {
                    if (msg.Length > 0)
                        msg.Length -= 1;
                }
                else
                    msg.Append(character);
            }

            // ok so it has something to do with capacity being exceeded i suppose that makes sense.
            // ah spritebatch is using chunked arrays with fixed sizes thats basically a non issue but will give some confusing garbage readings.
            // have to cut out spritebatch alltogether in order to prevent that which isn't needed other then for testing were that might be a issue.
            // or just do this for now.
            if (msg.Length > msg.Capacity - 10)  
                msg.Clear();

        }
        /// <summary>
        /// Raised for all command keys unless a disable is sent.
        /// This is pretty much always fireing unless disabled.
        /// Which text input boxes may or may not do.
        /// </summary>
        public void OnKeyCommandsInput(Keys firstKeyPressed, Keys secondKeyPressed, Keys thirdKeyPressed)
        {
            if (Keyboard.GetState().IsKeyDown(Keys.F12))
                frameRate.ClearCollectionTracking = true;
        }

        protected override void UnloadContent()
        {
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            // Updates and process key related actions.
            KeysAndTextInput.Update(gameTime);

            // Update the frame rate and gc display.
            frameRate.Update(gameTime);

            int tmp = msg2.Length;
            msg2.Clear();
            msg2
            .Append("\n F12 to force a collect. , Control + Delete to clear the string. NumLock Fixed or not fixed fps.").Append("")
            .Append("\n F7 F8 DisplayRecordedTypedCharacters ").Append(VisualKeyboardDisplay.DisplayRecordedTypedCharacters)
            .Append("\n msg2.Length ").Append(tmp).Append("  msg2.Capacity ").Append(msg2.Capacity)
            .Append("\n msg.Length ").Append(msg.Length).Append("  msg.Capacity ").Append(msg.Capacity)
            .Append("\n RepressSpeed ").Append(KeysAndTextInput.RepressSpeed)
            .Append("\n RepeatKeyAccelerationRatio ").Append(KeysAndTextInput.RepeatKeyAccelerationRatio)
            .Append("\n RepeatKeyMaxAccelerationRatio ").Append(KeysAndTextInput.RepeatKeyMaxAccelerationRatio)            
            ;

            //if (Keyboard.GetState().IsKeyDown(Keys.F12))
            //    frameRate.ClearCollectionTrackingAndGcCollect();

            // display recorded characters.
            if (Keyboard.GetState().IsKeyDown(Keys.F7))
                VisualKeyboardDisplay.DisplayRecordedTypedCharacters = true; // so it looks like spritebatch is the culprit here must be a buffer array resize kinda makes sense.

            // hide recorded characters.
            if (Keyboard.GetState().IsKeyDown(Keys.F8))
                VisualKeyboardDisplay.DisplayRecordedTypedCharacters = false;

            if (Keyboard.GetState().NumLock)
                this.IsFixedTimeStep = true;
            else
                this.IsFixedTimeStep = false;

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            spriteBatch.Begin();

            // Displays a image of a keyboard to show keystrokes and key combinations.
            VisualKeyboardDisplay.GetCurrentVisualKeyBoard.Draw(spriteBatch, Asset.font_MgGenFont, Asset.texture_QuertyKeyBoardSmall, dotTexture, new Vector2(10, 100), gameTime);

            // ++ draw msg for capacity.
            spriteBatch.DrawString(Asset.font_MgGenFont, msg2, new Vector2(310, 10), Color.AliceBlue);

            // simple test. just draw the string builder that has been building up.
            spriteBatch.DrawString(Asset.font_MgGenFont, msg, new Vector2(700, 360), Color.DarkGreen);

            //____________________________________________
            // Display the frame rate no garbage verifyed.
            frameRate.DrawFps(spriteBatch, Asset.font_MgGenFont, new Vector2(700, 60), Color.DarkGreen);

            spriteBatch.End();

            base.Draw(gameTime);
        }

        #region helper stuff dottexture ect...

        public static Texture2D CreateTextureDot(GraphicsDevice device)
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

        #endregion

    }

}
