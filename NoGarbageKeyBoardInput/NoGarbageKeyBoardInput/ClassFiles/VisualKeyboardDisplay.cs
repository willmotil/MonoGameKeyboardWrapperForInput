//using System;
//using System.IO;
using System;
using System.Text;
//using System.Linq;
//using System.Collections.Generic;
//using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Microsoft.Xna.Framework
{

    /// <summary>
    /// A single instanciated static class instance.
    /// </summary>
    public class VisualKeyboardDisplay
    {
        // Disallow multiple copies or setting or recreation of the original on subsequent constructions.
        private static VisualKeyboardDisplay usKeyBoard;
        public static VisualKeyboardDisplay GetCurrentVisualKeyBoard { get { return usKeyBoard; } }
        private static bool IsInitialized = false;

        public static bool RecordTypedCharacters = false;
        public static bool DisplayRecordedTypedCharacters = false;
        public static bool DisplayKeypressText = false;

        MgStringBuilder sbTotalMsg = new MgStringBuilder(12000);
        Rectangle k0_Rect = new Rectangle(0, 0, 0, 0);
        Rectangle k1_Rect = new Rectangle(0, 0, 0, 0);
        Rectangle k2_Rect = new Rectangle(0, 0, 0, 0);
        Rectangle k_caps_Rect = new Rectangle(0, 0, 0, 0);

        const string textInputMsg = "\n TextInputIsExecuted.";
        const string controlKeyInputMsg = "\n ControlKeyInputIsExecuted.";
        const string userClearTextInstructionMsg = "\n Press Control Z to clear text.";

        Color lastInputColorSelector = Color.Red;
        bool TextInputIsExecuting = false;
        bool ControlKeyInputIsExecuting = false;

        public Vector2 KeyPressInfoOffset { get; set; } = new Vector2(0, 0);
        public Vector2 VisualGraphicPositionOffset { get; set; } = new Vector2(0, 210);
        public Vector2 TextPositionOffset { get; set; } = new Vector2(0, 350);

        private Vector2 keyPressInfoPosition = new Vector2(0, 0);
        private Vector2 visualGraphicPosition = new Vector2(0, 0);
        private Vector2 textPosition = new Vector2(0, 0);

        /// <summary>
        /// Just type new VisualKeyboardDisplay() to initialize this anywere. 
        /// You don't need the reference doesn't matter if it gets reinitialized twice.
        /// Similar to a singleton i don't like them though, so this is my way of doing it.
        /// </summary>
        public VisualKeyboardDisplay()
        {
            if (IsInitialized == false)
            {
                KeysAndTextInput.AddMethodToRaiseOnTextInput(TextInput, this);
                KeysAndTextInput.AddMethodToRaiseOnCommandInput(CntrKeyInput, this);
                KeysAndTextInput.EnableTextInput(this);
                KeysAndTextInput.EnableKeyCommandsInput(this);
                usKeyBoard = this;
                IsInitialized = true;             
            }
        }

        // control keys have firing priority.
        public void CntrKeyInput(Keys key0, Keys key1, Keys key2)
        {
            ControlKeyInputIsExecuting = false;
            TextInputIsExecuting = false;

            ControlKeyInputIsExecuting = true;

            lastInputColorSelector = new Color(0, 0, 255, 99);

            k0_Rect = UsKeyStruct.GetRectangle(key0, visualGraphicPosition);
            k1_Rect = UsKeyStruct.GetRectangle(key1, visualGraphicPosition);
            k2_Rect = UsKeyStruct.GetRectangle(key2, visualGraphicPosition);

            // control z clear the input recording buffer.
            if (IsCommandActive(key0, key1, key2, Keys.LeftControl, Keys.Delete))
                sbTotalMsg.Clear();
        }

        public void TextInput(char c, Keys key)
        {
            TextInputIsExecuting = true;

            lastInputColorSelector = new Color(255, 0, 0, 99);

            if (RecordTypedCharacters)
            {
                if (c == '\t')
                    sbTotalMsg.Append("    ");
                else
                {
                    if (key == Keys.Back)
                    {
                        if (sbTotalMsg.Length > 0)
                            sbTotalMsg.Length -= 1;
                    }
                    else
                        sbTotalMsg.Append(c);
                }
            }

            //k0_Rect = UsKeyStruct.GetRectangle(Keys.None, visualGraphicPosition);
            //k1_Rect = UsKeyStruct.GetRectangle(Keys.None, visualGraphicPosition);

            k0_Rect = UsKeyStruct.GetRectangle(key, visualGraphicPosition);
        }

        public static bool IsCommandActive(Keys k0, Keys k1, Keys k2, Keys commandKey1, Keys commandKey2)
        {
            int sum = 0;
            if (k0 == commandKey1 || k1 == commandKey1 || k1 == commandKey1)
                sum++;
            if (k0 == commandKey2 || k1 == commandKey2 || k1 == commandKey2)
                sum++;
            if (sum > 1)
                return true;
            else
                return false;
        }

        public void Draw(SpriteBatch spriteBatch, SpriteFont currentFont, Texture2D keyboardTexture, Texture2D dot, Vector2 position, GameTime gameTime)
        {
            keyPressInfoPosition = position + KeyPressInfoOffset;
            visualGraphicPosition = position + VisualGraphicPositionOffset;
            textPosition = position + TextPositionOffset;
            var offsetTxtInputmsg = keyPressInfoPosition + new Vector2(0, 120);
            var offsetCntrlInputmsg = keyPressInfoPosition + new Vector2(0, 140);
            var offsetClearTextmsg = keyPressInfoPosition + new Vector2(0, 160);

            Color solid = lastInputColorSelector;
            solid.A = 255;

            if (KeysAndTextInput.Caps)
                k_caps_Rect = UsKeyStruct.GetRectangle(Keys.CapsLock, visualGraphicPosition);
            else
                k_caps_Rect = UsKeyStruct.GetRectangle(Keys.None, visualGraphicPosition);

            // display the visual keyboard.
            spriteBatch.Draw(keyboardTexture, visualGraphicPosition, Color.Wheat);
            if (TextInputIsExecuting || ControlKeyInputIsExecuting)
                spriteBatch.Draw(dot, k0_Rect, lastInputColorSelector);
            if (ControlKeyInputIsExecuting)
            {
                spriteBatch.Draw(dot, k1_Rect, lastInputColorSelector);
                spriteBatch.Draw(dot, k2_Rect, lastInputColorSelector);
            }
            spriteBatch.Draw(dot, k_caps_Rect, new Color(0,120, 220, 79) );

            if (DisplayKeypressText)
            {
                // watch the actual key presses.
                spriteBatch.DrawString(currentFont, KeysAndTextInput.KeyPressInformationMessage, keyPressInfoPosition, Color.Wheat);
                // display which are firing.
                if (TextInputIsExecuting)
                    spriteBatch.DrawString(currentFont, textInputMsg, offsetTxtInputmsg, Color.Red);
                if (ControlKeyInputIsExecuting)
                    spriteBatch.DrawString(currentFont, controlKeyInputMsg, offsetCntrlInputmsg, Color.Blue);
                // clear instructions.
                spriteBatch.DrawString(currentFont, userClearTextInstructionMsg, offsetClearTextmsg, Color.White);
            }

           // display key presses as a continuous string.
           if(DisplayRecordedTypedCharacters)
                 spriteBatch.DrawString(currentFont, sbTotalMsg, textPosition, Color.DarkGreen);

            if (KeysAndTextInput.IsKeyPressOccuring == false)
            {
                // Clear out the visual keys
                k0_Rect = UsKeyStruct.GetRectangle(Keys.None, visualGraphicPosition);
                k1_Rect = UsKeyStruct.GetRectangle(Keys.None, visualGraphicPosition);
                k2_Rect = UsKeyStruct.GetRectangle(Keys.None, visualGraphicPosition);
            }
        }
    }



    public struct UsKeyStruct
    {

        public Keys key;
        public Rectangle rect;
        public UsKeyStruct(Keys key, Rectangle rect)
        {
            this.key = key;
            this.rect = rect;
        }

        public static Rectangle GetRectangle(Keys key, Vector2 offset)
        {
            for (int i = 0; i < visualKeyboardKeyRectangles.Length; i++)
                if (key == visualKeyboardKeyRectangles[i].key)
                {
                    var r = visualKeyboardKeyRectangles[i].rect;
                    return new Rectangle(r.X + (int)offset.X, r.Y + (int)offset.Y, r.Width, r.Height);
                }
            return new Rectangle();
        }

        public static UsKeyStruct[] visualKeyboardKeyRectangles = new UsKeyStruct[]
        {
new UsKeyStruct( Keys.None ,  new Rectangle( 0, 0, 0, 0 ) ),
new UsKeyStruct( Keys.Back ,  new Rectangle( 225, 38, 32, 16 ) ),
new UsKeyStruct( Keys.Tab ,  new Rectangle( 8, 55, 24, 16 ) ),
new UsKeyStruct( Keys.Enter ,  new Rectangle( 222, 72, 36, 16 ) ),
new UsKeyStruct( Keys.Pause ,  new Rectangle( 301, 6, 16, 16 ) ),
new UsKeyStruct( Keys.CapsLock ,  new Rectangle( 8, 72, 28, 16 ) ),
new UsKeyStruct( Keys.Kana ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.Kanji ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.Escape ,  new Rectangle( 8, 0, 16, 16 ) ),
new UsKeyStruct( Keys.ImeConvert ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.ImeNoConvert ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.Space ,  new Rectangle( 76, 107, 90, 16 ) ),
new UsKeyStruct( Keys.PageUp ,  new Rectangle( 300, 38, 16, 16 ) ),
new UsKeyStruct( Keys.PageDown ,  new Rectangle( 300, 56, 16, 16 ) ),
new UsKeyStruct( Keys.End ,  new Rectangle( 283, 56, 16, 16 ) ),
new UsKeyStruct( Keys.Home ,  new Rectangle( 282, 38, 16, 16 ) ),
new UsKeyStruct( Keys.Left ,  new Rectangle( 266, 107, 16, 16 ) ),
new UsKeyStruct( Keys.Up ,  new Rectangle( 283, 90, 16, 16 ) ),
new UsKeyStruct( Keys.Right ,  new Rectangle( 300, 107, 16, 16 ) ),
new UsKeyStruct( Keys.Down ,  new Rectangle( 283, 107, 16, 16 ) ),
new UsKeyStruct( Keys.Select ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.Print ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.Execute ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.PrintScreen ,  new Rectangle( 267, 6, 16, 16 ) ),
new UsKeyStruct( Keys.Insert ,  new Rectangle( 265, 38, 16, 16 ) ),
new UsKeyStruct( Keys.Delete ,  new Rectangle( 266, 56, 16, 16 ) ),
new UsKeyStruct( Keys.Help ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.D0 ,  new Rectangle( 174, 38, 15, 16 ) ),
new UsKeyStruct( Keys.D1 ,  new Rectangle( 24, 38, 15, 16 ) ),
new UsKeyStruct( Keys.D2 ,  new Rectangle( 42, 38, 15, 16 ) ),
new UsKeyStruct( Keys.D3 ,  new Rectangle( 59, 38, 15, 16 ) ),
new UsKeyStruct( Keys.D4 ,  new Rectangle( 75, 38, 15, 16 ) ),
new UsKeyStruct( Keys.D5 ,  new Rectangle( 92, 38, 15, 16 ) ),
new UsKeyStruct( Keys.D6 ,  new Rectangle( 109, 38, 15, 16 ) ),
new UsKeyStruct( Keys.D7 ,  new Rectangle( 125, 38, 15, 16 ) ),
new UsKeyStruct( Keys.D8 ,  new Rectangle( 141, 38, 15, 16 ) ),
new UsKeyStruct( Keys.D9 ,  new Rectangle( 158, 38, 15, 16 ) ),
new UsKeyStruct( Keys.A ,  new Rectangle( 36, 72, 16, 16 ) ),
new UsKeyStruct( Keys.B ,  new Rectangle( 115, 90, 17, 16 ) ),
new UsKeyStruct( Keys.C ,  new Rectangle( 81, 90, 17, 16 ) ),
new UsKeyStruct( Keys.D ,  new Rectangle( 70, 72, 16, 16 ) ),
new UsKeyStruct( Keys.E ,  new Rectangle( 65, 55, 17, 16 ) ),
new UsKeyStruct( Keys.F ,  new Rectangle( 87, 72, 16, 16 ) ),
new UsKeyStruct( Keys.G ,  new Rectangle( 104, 72, 16, 16 ) ),
new UsKeyStruct( Keys.H ,  new Rectangle( 121, 72, 16, 16 ) ),
new UsKeyStruct( Keys.I ,  new Rectangle( 150, 55, 17, 16 ) ),
new UsKeyStruct( Keys.J ,  new Rectangle( 138, 72, 16, 16 ) ),
new UsKeyStruct( Keys.K ,  new Rectangle( 155, 72, 16, 16 ) ),
new UsKeyStruct( Keys.L ,  new Rectangle( 172, 72, 16, 16 ) ),
new UsKeyStruct( Keys.M ,  new Rectangle( 149, 90, 17, 16 ) ),
new UsKeyStruct( Keys.N ,  new Rectangle( 132, 90, 17, 16 ) ),
new UsKeyStruct( Keys.O ,  new Rectangle( 167, 55, 17, 16 ) ),
new UsKeyStruct( Keys.P ,  new Rectangle( 184, 55, 17, 16 ) ),
new UsKeyStruct( Keys.Q ,  new Rectangle( 31, 55, 17, 16 ) ),
new UsKeyStruct( Keys.R ,  new Rectangle( 82, 55, 17, 16 ) ),
new UsKeyStruct( Keys.S ,  new Rectangle( 53, 72, 16, 16 ) ),
new UsKeyStruct( Keys.T ,  new Rectangle( 99, 55, 17, 16 ) ),
new UsKeyStruct( Keys.U ,  new Rectangle( 133, 55, 17, 16 ) ),
new UsKeyStruct( Keys.V ,  new Rectangle( 100, 90, 17, 16 ) ),
new UsKeyStruct( Keys.W ,  new Rectangle( 48, 55, 17, 16 ) ),
new UsKeyStruct( Keys.X ,  new Rectangle( 64, 90, 17, 16 ) ),
new UsKeyStruct( Keys.Y ,  new Rectangle( 116, 55, 17, 16 ) ),
new UsKeyStruct( Keys.Z ,  new Rectangle( 47, 90, 17, 16 ) ),
new UsKeyStruct( Keys.LeftWindows ,  new Rectangle( 33, 107, 20, 16 ) ),
new UsKeyStruct( Keys.RightWindows ,  new Rectangle( 190, 107, 20, 16 ) ),
new UsKeyStruct( Keys.Apps ,  new Rectangle( 211, 107, 20, 16 ) ),
new UsKeyStruct( Keys.Sleep ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.NumPad0 ,  new Rectangle( 325, 107, 16, 16 ) ),
new UsKeyStruct( Keys.NumPad1 ,  new Rectangle( 325, 90, 16, 16 ) ),
new UsKeyStruct( Keys.NumPad2 ,  new Rectangle( 342, 90, 16, 16 ) ),
new UsKeyStruct( Keys.NumPad3 ,  new Rectangle( 359, 90, 16, 16 ) ),
new UsKeyStruct( Keys.NumPad4 ,  new Rectangle( 325, 72, 16, 16 ) ),
new UsKeyStruct( Keys.NumPad5 ,  new Rectangle( 342, 72, 16, 16 ) ),
new UsKeyStruct( Keys.NumPad6 ,  new Rectangle( 359, 72, 16, 16 ) ),
new UsKeyStruct( Keys.NumPad7 ,  new Rectangle( 325, 55, 16, 16 ) ),
new UsKeyStruct( Keys.NumPad8 ,  new Rectangle( 342, 55, 16, 16 ) ),
new UsKeyStruct( Keys.NumPad9 ,  new Rectangle( 359, 55, 16, 16 ) ),
new UsKeyStruct( Keys.Multiply ,  new Rectangle( 359, 38, 16, 16 ) ),
new UsKeyStruct( Keys.Add ,  new Rectangle( 376, 55, 16, 32 ) ),
new UsKeyStruct( Keys.Separator ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.Subtract ,  new Rectangle( 376, 38, 16, 16 ) ),
new UsKeyStruct( Keys.Decimal ,  new Rectangle( 183, 90, 17, 16 ) ),
new UsKeyStruct( Keys.Divide ,  new Rectangle( 342, 38, 16, 16 ) ),
new UsKeyStruct( Keys.F1 ,  new Rectangle( 35, 6, 16, 16 ) ),
new UsKeyStruct( Keys.F2 ,  new Rectangle( 52, 6, 16, 16 ) ),
new UsKeyStruct( Keys.F3 ,  new Rectangle( 69, 6, 16, 16 ) ),
new UsKeyStruct( Keys.F4 ,  new Rectangle( 86, 6, 16, 16 ) ),
new UsKeyStruct( Keys.F5 ,  new Rectangle( 112, 6, 16, 16 ) ),
new UsKeyStruct( Keys.F6 ,  new Rectangle( 129, 6, 16, 16 ) ),
new UsKeyStruct( Keys.F7 ,  new Rectangle( 146, 6, 16, 16 ) ),
new UsKeyStruct( Keys.F8 ,  new Rectangle( 163, 6, 16, 16 ) ),
new UsKeyStruct( Keys.F9 ,  new Rectangle( 189, 6, 16, 16 ) ),
new UsKeyStruct( Keys.F10 ,  new Rectangle( 206, 6, 16, 16 ) ),
new UsKeyStruct( Keys.F11 ,  new Rectangle( 223, 6, 16, 16 ) ),
new UsKeyStruct( Keys.F12 ,  new Rectangle( 240, 6, 16, 16 ) ),
new UsKeyStruct( Keys.F13 ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.F14 ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.F15 ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.F16 ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.F17 ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.F18 ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.F19 ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.F20 ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.F21 ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.F22 ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.F23 ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.F24 ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.NumLock ,  new Rectangle( 325, 38, 16, 16 ) ),
new UsKeyStruct( Keys.Scroll ,  new Rectangle( 284, 6, 16, 16 ) ),
new UsKeyStruct( Keys.LeftShift ,  new Rectangle( 8, 90, 38, 16 ) ),
new UsKeyStruct( Keys.RightShift ,  new Rectangle( 217, 90, 38, 16 ) ),
new UsKeyStruct( Keys.LeftControl ,  new Rectangle( 9, 107, 24, 16 ) ),
new UsKeyStruct( Keys.RightControl ,  new Rectangle( 235, 107, 20, 16 ) ),
new UsKeyStruct( Keys.LeftAlt ,  new Rectangle( 54, 107, 20, 16 ) ),
new UsKeyStruct( Keys.RightAlt ,  new Rectangle( 167, 107, 20, 16 ) ),
new UsKeyStruct( Keys.BrowserBack ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.BrowserForward ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.BrowserRefresh ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.BrowserStop ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.BrowserSearch ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.BrowserFavorites ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.BrowserHome ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.VolumeMute ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.VolumeDown ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.VolumeUp ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.MediaNextTrack ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.MediaPreviousTrack ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.MediaStop ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.MediaPlayPause ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.LaunchMail ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.SelectMedia ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.LaunchApplication1 ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.LaunchApplication2 ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.OemSemicolon ,  new Rectangle( 189, 72, 16, 16 ) ),
new UsKeyStruct( Keys.OemPlus ,  new Rectangle( 208, 38, 15, 16 ) ),
new UsKeyStruct( Keys.OemComma ,  new Rectangle( 166, 90, 17, 16 ) ),
new UsKeyStruct( Keys.OemMinus ,  new Rectangle( 191, 38, 15, 16 ) ),
new UsKeyStruct( Keys.OemPeriod ,  new Rectangle( 183, 90, 17, 16 ) ),
new UsKeyStruct( Keys.OemQuestion ,  new Rectangle( 200, 90, 17, 16 ) ),
new UsKeyStruct( Keys.OemTilde ,  new Rectangle( 8, 38, 15, 16 ) ),
new UsKeyStruct( Keys.ChatPadGreen ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.ChatPadOrange ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.OemOpenBrackets ,  new Rectangle( 199, 55, 17, 16 ) ),
new UsKeyStruct( Keys.OemPipe ,  new Rectangle( 233, 55, 22, 16 ) ),
new UsKeyStruct( Keys.OemCloseBrackets ,  new Rectangle( 216, 55, 17, 16 ) ),
new UsKeyStruct( Keys.OemQuotes ,  new Rectangle( 206, 72, 16, 16 ) ),
new UsKeyStruct( Keys.Oem8 ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.OemBackslash ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.ProcessKey ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.OemCopy ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.OemAuto ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.OemEnlW ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.Attn ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.Crsel ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.Exsel ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.EraseEof ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.Play ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.Zoom ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.Pa1 ,  new Rectangle( 0, 0, 20, 20 ) ),
new UsKeyStruct( Keys.OemClear ,  new Rectangle( 0, 0, 20, 20 ) )
            };
    }

}
