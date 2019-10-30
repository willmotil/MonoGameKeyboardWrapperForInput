/* 
 * +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++
 This version is newer and is meant to replace my first couple attempts.
 This version operationally has a better strategy to ensure there is no garbage.
 The idea here is simple enable or disable msg callbacks after registering with this class.
 Prevent text input when control key combinations are pressed so only control keys are fired.
 This helps a great deal at reducing complexity and the text input checks required.
 Primarily it is most helpful for the classes that use it.
 Todo..
 I might however make this a singleton or a single static instance.
 As well as yank in the sub classes proper dunno still a new class.

 * +++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++++ 
 */

using System;
using System.Linq;
//using System.Xml.Serialization;
//using System.Xml;
//using System.Text;
using System.Collections.Generic;
//using Microsoft.Xna.Framework;
//using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System.Threading;

namespace Microsoft.Xna.Framework
{
    /// <summary>
    ///
    /// </summary>
    public static class KeysAndTextInput
    {
        public static MgStringBuilder KeyPressInformationMessage = new MgStringBuilder("");
        public static bool IsRecordingKeyPressInformation = true;

        public static bool IsKeyPressOccuring { get; private set; } = false;
        public static float RepressSpeed { get; set; } = .25f;
        public static float RepeatKeyMaxAccelerationRatio { get; set; } = .90f;
        public static float RepeatKeyAccelerationRatio { get; set; } = .09f;

        public static bool Caps { get; private set; }
        public static bool NumLock { get; private set; }
        public static bool Shift { get; private set; }
        public static bool Control { get; private set; }

        public static Keys Key0 { get; private set; }
        public static Keys Key1 { get; private set; }
        public static Keys Key2 { get; private set; }
        public static int Key0_Index { get; private set; }
        public static int Key1_Index { get; private set; }
        public static int Key2_Index { get; private set; }

        static Keys[] keyArray;
        static string[] keyName;
        static char[] charUpper;
        static char[] charLower;
        static bool[] charIsControl;
        static bool[] charIsAvailableAsTextInput;

        private static bool isInitialize = false;
        private static TriggerableTimer RepeatKeyDelayTimer = new TriggerableTimer(.25f);// .25
        private static Keys keyLast = Keys.None;
        private static float RepeatKeyTimeVelocity { get; set; } = 0f;

        private static float elapsedGameTime = 0f;
        private static bool DelayKeyRepressExpired { get { return RepeatKeyDelayTimer.IsTriggered; } }
        private static float DelayKeyRepressTimeElapsed { get { return RepeatKeyDelayTimer.Elapsed; } }
        private static float DelayKeyRepressTimerAmount { get { return RepeatKeyDelayTimer.Timer; } }


        public static void AddMethodToRaiseOnTextInput<T>(Action<char, Keys> method, T t)
        {
            ActionTextInputPoolingList.InitialRegistration(method, t);
        }
        public static void AddMethodToRaiseOnCommandInput<T>(Action<Keys, Keys, Keys> method, T t)
        {
            ActionKeyCommandsInputPoolingList.InitialRegistration(method, t);
        }
        public static void RemoveMethodToRaiseOnTextInput<T>(Action<char, Keys> method, T t)
        {
            ActionTextInputPoolingList.UnRegister(method, t);
        }
        public static void RemoveMethodToRaiseOnCommandInput<T>(Action<Keys, Keys, Keys> method, T t)
        {
            ActionKeyCommandsInputPoolingList.UnRegister(method, t);
        }
        /// <summary>
        /// This is to register two callbacks when keys are pressed some key combos wont raise the OnCharacterKeysPressed
        /// As for control keys at the moment its required that update is called for it to function properly.
        /// </summary>
        public static void AddMethodsToRaiseOnKeyBoardActivity<T>(Action<char, Keys> textInputMethodToCall, Action<Keys, Keys, Keys> controlKeysMethodToCall, T t)
        {
            ActionTextInputPoolingList.InitialRegistration(textInputMethodToCall, t);
            ActionKeyCommandsInputPoolingList.InitialRegistration(controlKeysMethodToCall, t);
        }
        /// <summary>
        /// Removes the pair of methods registered.
        /// </summary>
        public static void RemoveMethodsToRaiseOnKeyBoardActivity<T>(Action<char, Keys> textInputMethodToCall, Action<Keys, Keys, Keys> controlKeysMethodToCall, T t)
        {
            ActionTextInputPoolingList.UnRegister(textInputMethodToCall, t);
            ActionKeyCommandsInputPoolingList.UnRegister(controlKeysMethodToCall, t);
        }

        public static void EnableTextInput<T>(T t)
        {
            ActionTextInputPoolingList.Enable(t);
        }
        public static void DisableTextInput<T>(T t)
        {
            ActionTextInputPoolingList.Disable(t);
        }
        public static void EnableKeyCommandsInput<T>(T t)
        {
            ActionKeyCommandsInputPoolingList.Enable(t);
        }
        public static void DisableKeyCommandsInput<T>(T t)
        {
            ActionKeyCommandsInputPoolingList.Disable(t);
        }

        /// <summary>
        /// Roundabout sets keys to a array so i can iterate them in a check.
        /// This is going to be part of a intensively repetitious operation per frame.
        /// Since there are a ton of keys i want a contiguous block for fast iteration.
        /// </summary>
        public static void Initialize()
        {
            // disallow double initializations.
            if (isInitialize == false)
            {
                // make a single listing to remove duplicates.
                var temp = ((Keys[])Enum.GetValues(typeof(Keys))).ToList();
                List<Keys> thekeys = new List<Keys>();
                for (int j = 0; j < temp.Count(); j++)
                {
                    bool match = false;
                    for (int k = 0; k < thekeys.Count(); k++)
                        if (thekeys[k].ToString() == temp[j].ToString())
                            match = true;
                    if (match == false)
                    {
                        thekeys.Add(temp[j]);
                        Console.WriteLine("new DrawUsKeyStruct( Keys." + thekeys[j].ToString() + " ,  new Rectangle( 0, 0, 20, 20 ) ),");
                    }
                }

                // size the arrays since they are a set size as are our keyboard keys.
                var len = thekeys.Count;
                keyArray = new Keys[len];
                keyName = new string[len];
                charIsControl = new bool[len];
                charIsAvailableAsTextInput = new bool[len];
                charUpper = new char[len];
                charLower = new char[len];

                // set this keyboards keys to arrays for faster lookups.
                for (int i = 0; i < thekeys.Count; i++)
                {
                    Keys k = thekeys[i];
                    // Pull out more specific info that keys simply doesn't have or basically denys you access to as there is no char or unicode lookups possible on them.
                    KeyInformation ki = KeyInformation.GetKeyInfo(k);
                    keyArray[i] = k;
                    keyName[i] = ki.KeyName;
                    charIsControl[i] = ki.IsControl;
                    charIsAvailableAsTextInput[i] = ki.hasInputCharacter;
                    char U = ' ';
                    char L = ' ';
                    if (ki.characterUpper.Length > 0)
                        U = ki.characterUpper[0];
                    if (ki.characterLower.Length > 0)
                        L = ki.characterLower[0];
                    charUpper[i] = (U);
                    charLower[i] = (L);
                }
                isInitialize = true;
            }
        }

        /// <summary>
        /// Id like to trim this down a bit.
        /// </summary>
        public static void Update(GameTime gameTime)
        {
            elapsedGameTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var itsOkToRepeatNow = RepeatKeyDelayTimer.Update(gameTime);

            Caps = Keyboard.GetState().CapsLock;
            NumLock = Keyboard.GetState().NumLock;

            bool upper = false;
            Key0 = Keys.None;
            Key0_Index = 0;
            Key1 = Keys.None;
            Key1_Index = 0;
            Key2 = Keys.None;
            Key2_Index = 0;

            IsKeyPressOccuring = false;

            // Check all the keys in a loop.
            for (int i = 0; i < keyArray.Length; i++)
            {
                Keys k = keyArray[i];
                var keydown = Keyboard.GetState().IsKeyDown(k);
                if (keydown)
                {
                    IsKeyPressOccuring = true;
                    if (keydown)
                    {
                        if (Key0 == Keys.None)
                        {
                            Key0 = k;
                            Key0_Index = i;
                        }
                        else
                        {
                            if (Key1 == Keys.None)
                            {
                                Key1 = k;
                                Key1_Index = i;
                            }
                            else
                            {
                                Key2 = k;
                                Key2_Index = i;
                            }
                        }
                    }
                }
            }

            // Do we procced 
            // At this point we know if a key is pressed or not.
            bool conditionNotTheSameLastKey = Key0 != keyLast;
            bool conditionRepeatOkOrNotTheSameLastKey = Key0 != keyLast || itsOkToRepeatNow;
            //
            if (IsKeyPressOccuring && conditionRepeatOkOrNotTheSameLastKey)
            {
                // special checks.
                if (Key0 == Keys.LeftControl || Key1 == Keys.LeftControl || Key2 == Keys.LeftControl)
                    Control = true;
                else
                    Control = false;
                if (Caps)
                    upper = true;
                if (Key0 == Keys.LeftShift || Key0 == Keys.RightShift || Key1 == Keys.LeftShift || Key1 == Keys.RightShift || Key2 == Keys.LeftShift || Key2 == Keys.RightShift)
                {
                    Shift = true;
                    upper = !upper;
                }
                else
                    Shift = false;

                // basically anything can be a command key except none and maybe caps lock cause its a weird one.
                int resultOfTestForComandkeyActivation = 0;
                if (Key0 != Keys.None && Key0 != Keys.CapsLock)
                    resultOfTestForComandkeyActivation++;
                if (Key1 != Keys.None && Key0 != Keys.CapsLock)
                    resultOfTestForComandkeyActivation++;
                if (Key2 != Keys.None && Key0 != Keys.CapsLock)
                    resultOfTestForComandkeyActivation++;
                // Test for a control command key combination that should cause the exclusion of textInput.
                int resultOfTestForTextInputExclusion = 0;
                if (charIsControl[Key0_Index] && charIsAvailableAsTextInput[Key0_Index] == false && Key0 != Keys.None && Key0 != Keys.LeftShift && Key0 != Keys.RightShift)
                    resultOfTestForTextInputExclusion++;
                if (charIsControl[Key1_Index] && charIsAvailableAsTextInput[Key1_Index] == false && Key1 != Keys.None && Key1 != Keys.LeftShift && Key1 != Keys.RightShift)
                    resultOfTestForTextInputExclusion++;
                if (charIsControl[Key2_Index] && charIsAvailableAsTextInput[Key2_Index] == false && Key2 != Keys.None && Key2 != Keys.LeftShift && Key2 != Keys.RightShift)
                    resultOfTestForTextInputExclusion++;
                
                //
                // Do we send to control key command events, we pretty much always do unless the reciever disables it.
                //
                if (resultOfTestForComandkeyActivation > 0 && conditionRepeatOkOrNotTheSameLastKey && IsKeyPressOccuring)
                        ActionKeyCommandsInputPoolingList.CallKeyCommandInputDestinationRecievers(Key0, Key1, Key2);
                //
                // Do we send to text input. 
                // this however we don't always do.
                //
                if (resultOfTestForTextInputExclusion < 1)
                {
                    // what to send to text input 
                    char charToSend = '\0';
                    Keys keyToSend = Key0;
                    bool dok1test = true;
                    if (Key0 != Keys.None && Key0 != Keys.LeftShift && Key0 != Keys.RightShift)
                    {
                        if (charIsAvailableAsTextInput[Key0_Index] == true) // send or not
                        {
                            if (upper)
                                charToSend = charUpper[Key0_Index];
                            else
                                charToSend = charLower[Key0_Index];
                            keyToSend = Key0;
                            dok1test = false;
                        }
                    }
                    if (Key1 != Keys.None && dok1test) // send or not
                    {
                        if (charIsAvailableAsTextInput[Key1_Index] == true)
                        {
                            if (upper)
                                charToSend = charUpper[Key1_Index];
                            else
                                charToSend = charLower[Key1_Index];
                            keyToSend = Key1;
                        }
                    }
                    // Send to text input for recieving. One slight problem is what to return if we have a press and control key of unknown type slips by.
                    if ((conditionRepeatOkOrNotTheSameLastKey) && IsKeyPressOccuring && charToSend != '\0')
                        ActionTextInputPoolingList.CallTextInputDestinationRecievers(charToSend, keyToSend);
                }
            }

            // try to get the key repeat and acceleration correct.
            CheckRepress(IsKeyPressOccuring, conditionNotTheSameLastKey == false);
            
            // set last key to current key.
            keyLast = Key0;           
            // key status information
            if (IsRecordingKeyPressInformation)
            {
                KeyPressInformationMessage.Clear();
                KeyPressInformationMessage
                    .Append(" Caps On: ").Append(Caps)
                    .Append(" \n")
                    .Append(" ShiftIsDown: ").Append(Shift)
                    .Append(" \n")
                    .Append(" Upper case on: ").Append(upper)
                    .Append(" \n")
                    .Append(" Control Key is down: ").Append(Control)
                    .Append(" \n")
                    .Append(" K0: name: ").Append(keyName[Key0_Index]).Append(" char: ").Append(upper ? charUpper[Key0_Index] : charLower[Key0_Index])
                    .Append(" iscontrol: ").Append(charIsControl[Key0_Index]).Append(" isTextSringInput: ").Append(charIsAvailableAsTextInput[Key0_Index])
                    .Append(" \n")
                    .Append(" K1: name: ").Append(keyName[Key1_Index]).Append(" char: ").Append(upper ? charUpper[Key1_Index] : charLower[Key1_Index])
                    .Append(" iscontrol: ").Append(charIsControl[Key1_Index]).Append(" isTextSringInput: ").Append(charIsAvailableAsTextInput[Key1_Index])
                    .Append(" \n")
                    .Append(" K2: name: ").Append(keyName[Key2_Index]).Append(" char: ").Append(upper ? charUpper[Key2_Index] : charLower[Key2_Index])
                    .Append(" iscontrol: ").Append(charIsControl[Key2_Index]).Append(" isTextSringInput: ").Append(charIsAvailableAsTextInput[Key2_Index])
                    .Append(" \n")
                    ;
            }
        }

        private static void CheckRepress(bool conditionPressOccured, bool conditionTheSameLastKey)
        {
            //elapsedGameTime CurrentUserRepressTime  maxAcceleration RepeatKeyAcceleration RepeatKeyDelayTimer.Elapsed  RepeatKeyDelayTimer.Timer
            
            if (conditionPressOccured && conditionTheSameLastKey)
            {
                var tempRepeatKeyMaxAcceleration = RepeatKeyMaxAccelerationRatio * RepressSpeed;
                var tempRepeatKeyAcceleration = RepeatKeyAccelerationRatio * RepressSpeed;

                if (RepeatKeyDelayTimer.IsTriggered)
                {
                    RepeatKeyDelayTimer.ResetElapsedToZeroAndStartTiming();

                    RepeatKeyTimeVelocity += tempRepeatKeyAcceleration; //.02f;
                    if (RepeatKeyTimeVelocity > tempRepeatKeyMaxAcceleration)
                        RepeatKeyTimeVelocity = tempRepeatKeyMaxAcceleration;

                    RepeatKeyDelayTimer.Timer = RepressSpeed - RepeatKeyTimeVelocity;
                    RepeatKeyDelayTimer.AddToElapsed = elapsedGameTime; //RepeatKeyAcceleration; // * elapsedGameTime
                }
            }
            else
            {
                RepeatKeyTimeVelocity = 0;
                RepeatKeyDelayTimer.Timer = RepressSpeed;
                RepeatKeyDelayTimer.ResetElapsedToZeroAndStartTiming();
            }
        }

        public static void Swap(int keyIndex0, int keyIndex1, out Keys k0, out Keys k1, out int k0_Index, out int k1_Index)
        {
            Keys kA = keyArray[keyIndex0];
            Keys kB = keyArray[keyIndex1];
            int tA = keyIndex0;
            int tB = keyIndex1;
            k0 = kB;
            k1 = kA;
            k0_Index = tB;
            k1_Index = tA;
        }

        public class KeyInformation
        {
            public Keys key;
            public string KeyName;
            public bool IsControl = false;
            public bool hasInputCharacter = false;
            public string characterUpper;
            public string characterLower;

            private KeyInformation(int number, Keys key, bool IsControl, bool ControlHasCharacter, string characterUpper, string characterLower)
            {
                this.key = key;
                this.KeyName = key.ToString();
                this.IsControl = IsControl;
                this.hasInputCharacter = ControlHasCharacter;
                this.characterUpper = characterUpper;
                this.characterLower = characterLower;
            }
            private KeyInformation(int number, Keys key) // letters only
            {
                this.key = key;
                this.KeyName = key.ToString();
                this.IsControl = false;
                this.hasInputCharacter = true;
                // Good answer here in this case we compare keys themselves primarily however for input out put we should use the regular version.
                //
                // Depending on the current culture, ToLower might produce a culture specific lowercase letter, that you aren't expecting. 
                // Such as producing ınfo without the dot on the i instead of info and thus mucking up string comparisons. 
                // For that reason, ToLowerInvariant should be used on any non-language-specific data.
                this.characterUpper = key.ToString().ToUpperInvariant();
                this.characterLower = key.ToString().ToLowerInvariant();
            }

            public static KeyInformation GetKeyInfo(Keys k)
            {
                KeyInformation result = keysInfo[0];
                foreach (var ki in keysInfo)
                {
                    if (ki.key == k)
                        result = ki;
                }
                return result;
            }

            public static bool IsText(Keys k)
            {
                var i = GetKeyInfo(k);
                if (i.IsControl)
                    return false;
                else
                    return true;
            }

            /// <summary>
            /// not the greatest method id rather not get a key char from a string but the alternative from the key itself well there is no character
            /// </summary>
            public static char GetLowerCaseChar(Keys k)
            {
                var i = GetKeyInfo(k);
                return i.characterLower[0];
            }

            /// <summary>
            /// This is primarily needed because the enums are such a shitty thing that i can't pull a character off of them for a key.
            /// Logically a keys data should include the unicode characters that a keyboard keys labeled characters represent from top bottom left to right.
            /// Key labels themselves should map to different keyboard characters for different layouts. As it is this is a us keyboard character mapping.
            /// The characters in the string are of course written in vs219 utf16, le-16 or be-16 ... be in my case.
            /// So for another language this would need to be converted when detected at start up if so desired.
            /// </summary>
            public static KeyInformation[] keysInfo = new KeyInformation[]
            {
                new KeyInformation(0 , Keys.None, true, false,"","" ),
                new KeyInformation(1 , Keys.Back, true, true,"\b","\b" ),
                new KeyInformation(2 , Keys.Tab, true, true,"\t","\t" ),
                new KeyInformation(3 , Keys.Enter, true, true,"\n","\n" ),
                new KeyInformation(4 , Keys.Pause, true, false,"","" ),
                new KeyInformation(5 , Keys.CapsLock, true, false,"","" ),
                new KeyInformation(6 , Keys.Kana, true, false,"","" ),
                new KeyInformation(7 , Keys.Kanji, true, false,"","" ),
                new KeyInformation(8 , Keys.Escape, true, false,"","" ),
                new KeyInformation(9 , Keys.ImeConvert, true, false,"","" ),
                new KeyInformation(10 , Keys.ImeNoConvert, true, false,"","" ),
                new KeyInformation(11 , Keys.Space, true, true," "," " ),
                new KeyInformation(12 , Keys.PageUp, true, false,"","" ),
                new KeyInformation(13 , Keys.PageDown, true, false,"","" ),
                new KeyInformation(14 , Keys.End, true, false,"","" ),
                new KeyInformation(15 , Keys.Home, true, false,"","" ),
                new KeyInformation(16 , Keys.Left, true, false,"","" ),
                new KeyInformation(17 , Keys.Up, true, false,"","" ),
                new KeyInformation(18 , Keys.Right, true, false,"","" ),
                new KeyInformation(19 , Keys.Down, true, false,"","" ),
                new KeyInformation(20 , Keys.Select, true, false,"","" ),
                new KeyInformation(21 , Keys.Print, true, false,"","" ),
                new KeyInformation(22 , Keys.Execute, true, false,"","" ),
                new KeyInformation(23 , Keys.PrintScreen, true, false,"","" ),
                new KeyInformation(24 , Keys.Insert, true, false,"","" ),
                new KeyInformation(25 , Keys.Delete, true, false,"","" ),
                new KeyInformation(26 , Keys.Help, true, false,"","" ),
                new KeyInformation(27 , Keys.D0, false, true,"!","0" ),
                new KeyInformation(28 , Keys.D1, false, true,"@","1" ),
                new KeyInformation(29 , Keys.D2, false, true,"#","2" ),
                new KeyInformation(30 , Keys.D3, false, true,"$","3" ),
                new KeyInformation(31 , Keys.D4, false, true,"%","4" ),
                new KeyInformation(32 , Keys.D5, false, true,"^","5" ),
                new KeyInformation(33 , Keys.D6, false, true,"&","6" ),
                new KeyInformation(34 , Keys.D7, false, true,"*","7" ),
                new KeyInformation(35 , Keys.D8, false, true,"(","8" ),
                new KeyInformation(36 , Keys.D9, false, true,")","9" ),
                new KeyInformation(37 , Keys.A),
                new KeyInformation(38 , Keys.B),
                new KeyInformation(39 , Keys.C),
                new KeyInformation(40 , Keys.D),
                new KeyInformation(41 , Keys.E),
                new KeyInformation(42 , Keys.F),
                new KeyInformation(43 , Keys.G),
                new KeyInformation(44 , Keys.H),
                new KeyInformation(45 , Keys.I),
                new KeyInformation(46 , Keys.J),
                new KeyInformation(47 , Keys.K),
                new KeyInformation(48 , Keys.L),
                new KeyInformation(49 , Keys.M),
                new KeyInformation(50 , Keys.N),
                new KeyInformation(51 , Keys.O),
                new KeyInformation(52 , Keys.P),
                new KeyInformation(53 , Keys.Q),
                new KeyInformation(54 , Keys.R),
                new KeyInformation(55 , Keys.S),
                new KeyInformation(56 , Keys.T),
                new KeyInformation(57 , Keys.U),
                new KeyInformation(58 , Keys.V),
                new KeyInformation(59 , Keys.W),
                new KeyInformation(60 , Keys.X),
                new KeyInformation(61 , Keys.Y),
                new KeyInformation(62 , Keys.Z),
                new KeyInformation(63 , Keys.LeftWindows, true, false,"","" ),
                new KeyInformation(64 , Keys.RightWindows, true, false,"","" ),
                new KeyInformation(65 , Keys.Apps, true, false,"","" ),
                new KeyInformation(66 , Keys.Sleep, true, false,"","" ),
                new KeyInformation(67 , Keys.NumPad0, false, true,"0","0" ),
                new KeyInformation(68 , Keys.NumPad1, false, true,"1","1" ),
                new KeyInformation(69 , Keys.NumPad2, false, true,"2","2" ),
                new KeyInformation(70 , Keys.NumPad3, false, true,"3","3" ),
                new KeyInformation(71 , Keys.NumPad4, false, true,"4","4" ),
                new KeyInformation(72 , Keys.NumPad5, false, true,"5","5" ),
                new KeyInformation(73 , Keys.NumPad6, false, true,"6","6" ),
                new KeyInformation(74 , Keys.NumPad7, false, true,"7","7" ),
                new KeyInformation(75 , Keys.NumPad8, false, true,"8","8" ),
                new KeyInformation(76 , Keys.NumPad9, false, true,"9","9" ),
                new KeyInformation(77 , Keys.Multiply, false, true,"*","*" ),
                new KeyInformation(78 , Keys.Add, false, true,"+","+" ),
                new KeyInformation(79 , Keys.Separator, false, true,"<","," ),
                new KeyInformation(80 , Keys.Subtract, false, true,"-","-" ),
                new KeyInformation(81 , Keys.Decimal, false, true,">","." ),
                new KeyInformation(82 , Keys.Divide, false, true,"?","/" ),
                new KeyInformation(83 , Keys.F1, true, false,"","" ),
                new KeyInformation(84 , Keys.F2, true, false,"","" ),
                new KeyInformation(85 , Keys.F3, true, false,"","" ),
                new KeyInformation(86 , Keys.F4, true, false,"","" ),
                new KeyInformation(87 , Keys.F5, true, false,"","" ),
                new KeyInformation(88 , Keys.F6, true, false,"","" ),
                new KeyInformation(89 , Keys.F7, true, false,"","" ),
                new KeyInformation(90 , Keys.F8, true, false,"","" ),
                new KeyInformation(91 , Keys.F9, true, false,"","" ),
                new KeyInformation(92 , Keys.F10, true, false,"","" ),
                new KeyInformation(93 , Keys.F11, true, false,"","" ),
                new KeyInformation(94 , Keys.F12, true, false,"","" ),
                new KeyInformation(95 , Keys.F13, true, false,"","" ),
                new KeyInformation(96 , Keys.F14, true, false,"","" ),
                new KeyInformation(97 , Keys.F15, true, false,"","" ),
                new KeyInformation(98 , Keys.F16, true, false,"","" ),
                new KeyInformation(99 , Keys.F17, true, false,"","" ),
                new KeyInformation(100 , Keys.F18, true, false,"","" ),
                new KeyInformation(101 , Keys.F19, true, false,"","" ),
                new KeyInformation(102 , Keys.F20, true, false,"","" ),
                new KeyInformation(103 , Keys.F21, true, false,"","" ),
                new KeyInformation(104 , Keys.F22, true, false,"","" ),
                new KeyInformation(105 , Keys.F23, true, false,"","" ),
                new KeyInformation(106 , Keys.F24, true, false,"","" ),
                new KeyInformation(107 , Keys.NumLock, true, false,"","" ),
                new KeyInformation(108 , Keys.Scroll, true, false,"","" ),
                new KeyInformation(109 , Keys.LeftShift, true, false,"","" ),
                new KeyInformation(110 , Keys.RightShift, true, false,"","" ),
                new KeyInformation(111 , Keys.LeftControl, true, false,"","" ),
                new KeyInformation(112 , Keys.RightControl, true, false,"","" ),
                new KeyInformation(113 , Keys.LeftAlt, true, false,"","" ),
                new KeyInformation(114 , Keys.RightAlt, true, false,"","" ),
                new KeyInformation(115 , Keys.BrowserBack, true, false,"","" ),
                new KeyInformation(116 , Keys.BrowserForward, true, false,"","" ),
                new KeyInformation(117 , Keys.BrowserRefresh, true, false,"","" ),
                new KeyInformation(118 , Keys.BrowserStop, true, false,"","" ),
                new KeyInformation(119 , Keys.BrowserSearch, true, false,"","" ),
                new KeyInformation(120 , Keys.BrowserFavorites, true, false,"","" ),
                new KeyInformation(121 , Keys.BrowserHome, true, false,"","" ),
                new KeyInformation(122 , Keys.VolumeMute, true, false,"","" ),
                new KeyInformation(123 , Keys.VolumeDown, true, false,"","" ),
                new KeyInformation(124 , Keys.VolumeUp, true, false,"","" ),
                new KeyInformation(125 , Keys.MediaNextTrack, true, false,"","" ),
                new KeyInformation(126 , Keys.MediaPreviousTrack, true, false,"","" ),
                new KeyInformation(127 , Keys.MediaStop, true, false,"","" ),
                new KeyInformation(128 , Keys.MediaPlayPause, true, false,"","" ),
                new KeyInformation(129 , Keys.LaunchMail, true, false,"","" ),
                new KeyInformation(130 , Keys.SelectMedia, true, false,"","" ),
                new KeyInformation(131 , Keys.LaunchApplication1, true, false,"","" ),
                new KeyInformation(132 , Keys.LaunchApplication2, true, false,"","" ),
                new KeyInformation(133 , Keys.OemSemicolon, false, true,":",";" ),
                new KeyInformation(134 , Keys.OemPlus, false, true,"+","=" ),
                new KeyInformation(135 , Keys.OemComma, false, true,"<","." ),
                new KeyInformation(136 , Keys.OemMinus, false, true,"_","-" ),
                new KeyInformation(137 , Keys.OemPeriod, false, true,">","." ),
                new KeyInformation(138 , Keys.OemQuestion, false, true,"?","/" ),
                new KeyInformation(139 , Keys.OemTilde, false, true,"~","`" ),
                new KeyInformation(140 , Keys.ChatPadGreen, true, false,"","" ),
                new KeyInformation(141 , Keys.ChatPadOrange, true, false,"","" ),
                new KeyInformation(142 , Keys.OemOpenBrackets, false, true,"{","[" ),
                new KeyInformation(143 , Keys.OemPipe, false, true,"|",@"\" ),
                new KeyInformation(144 , Keys.OemCloseBrackets, false, true,"}","]" ),
                new KeyInformation(145 , Keys.OemQuotes, false, true,"\"","'" ),
                new KeyInformation(146 , Keys.Oem8, false, false,"","" ),
                new KeyInformation(147 , Keys.OemBackslash, false, true,"|",@"\" ),
                new KeyInformation(148 , Keys.ProcessKey, true, false,"","" ),
                new KeyInformation(149 , Keys.OemCopy, true, false,"","" ),
                new KeyInformation(150 , Keys.OemAuto, true, false,"","" ),
                new KeyInformation(151 , Keys.OemEnlW, true, false,"","" ),
                new KeyInformation(152 , Keys.Attn, true, false,"","" ),
                new KeyInformation(153 , Keys.Crsel, true, false,"","" ),
                new KeyInformation(154 , Keys.Exsel, true, false,"","" ),
                new KeyInformation(155 , Keys.EraseEof, true, false,"","" ),
                new KeyInformation(156 , Keys.Play, true, false,"","" ),
                new KeyInformation(157 , Keys.Zoom, true, false,"","" ),
                new KeyInformation(158 , Keys.Pa1, true, false,"","" ),
                new KeyInformation(159 , Keys.OemClear, true, false,"","" )
          };
        }
    }

    #region delegate registration gc work around.

    public static class ActionTextInputPoolingList
    {
        static readonly object accessLock = new object();

        static List<Action<char, Keys>> actionList = new List<Action<char, Keys>>();
        static List<ActionListTrackingAndMapping> actionListMap = new List<ActionListTrackingAndMapping>();

        /// <summary>
        /// Calls the registered destination recievers.
        /// </summary>
        public static void CallTextInputDestinationRecievers(char c, Keys k)
        {
            for (int forindex = 0; forindex < actionListMap.Count; forindex++)
            {
                if (actionListMap[forindex].actionIsEnabled)
                    actionList[forindex]?.Invoke(c, k);
            }
        }

        public static void InitialRegistration<T>(Action<char, Keys> methodToCall, T inst)
        {
            try
            {
                Monitor.Enter(accessLock);
                InitialRegistration(methodToCall, inst.GetHashCode());
            }
            catch (Exception e)
            {
                Monitor.Exit(accessLock);
                //Console.WriteLine(e.ToString());
            }
        }

        public static void UnRegister<T>(Action<char, Keys> methodToCall, T inst)
        {
            try
            {
                Monitor.Enter(accessLock);
                UnRegister(methodToCall, inst.GetHashCode());
            }
            catch (Exception e)
            {
                Monitor.Exit(accessLock);
                //Console.WriteLine(e.ToString());
            }
        }

        public static void Enable<T>(T inst)
        {
            Enable(inst.GetHashCode());
        }
        public static void Disable<T>(T inst)
        {
            Disable(inst.GetHashCode());
        }

        private static void InitialRegistration(Action<char, Keys> methodToCall, int id)
        {
            bool match = false;
            int index = actionListMap.Count;
            int theid = id;
            bool isEnabled = false;
            for (int forindex = 0; forindex < actionListMap.Count; forindex++)
            {
                var m = actionListMap[forindex];
                if (m.actionID == id)
                {
                    match = true;
                    index = forindex;
                    isEnabled = m.actionIsEnabled;
                    forindex = actionListMap.Count; // break
                }
            }
            if (match == false)
            {
                actionList.Add(methodToCall);
                actionListMap.Add(new ActionListTrackingAndMapping(id, index, true));
            }
        }

        private static void UnRegister(Action<char, Keys> methodToCall, int id)
        {
            bool match = false;
            int index = actionListMap.Count;
            int theid = id;
            bool isEnabled = false;
            for (int forindex = 0; forindex < actionListMap.Count; forindex++)
            {
                var m = actionListMap[forindex];
                if (m.actionID == id)
                {
                    match = true;
                    index = forindex;
                    isEnabled = m.actionIsEnabled;
                    forindex = actionListMap.Count; // break
                }
            }
            if (match == true) // its not in the list already user error throw msg.
            {
                actionList.RemoveAt(index);
                actionListMap.RemoveAt(index);
            }
        }

        private static void Enable(int id)
        {
            for (int forindex = 0; forindex < actionListMap.Count; forindex++)
            {
                if (actionListMap[forindex].actionID == id)
                {
                    actionListMap[forindex] = new ActionListTrackingAndMapping(id, forindex, true);
                    forindex = actionListMap.Count; // break
                }
            }
        }

        private static void Disable(int id)
        {
            for (int forindex = 0; forindex < actionListMap.Count; forindex++)
            {
                if (actionListMap[forindex].actionID == id)
                {
                    actionListMap[forindex] = new ActionListTrackingAndMapping(id, forindex, false);
                    forindex = actionListMap.Count; // break
                }
            }
        }

        public struct ActionListTrackingAndMapping
        {
            public int actionID { get; set; }
            public int actionIndexToListForId;
            public bool actionIsEnabled;

            public ActionListTrackingAndMapping(int id, int index, bool alive)
            {
                actionID = id;
                actionIndexToListForId = index;
                actionIsEnabled = alive;
            }
        }
    }

    public static class ActionKeyCommandsInputPoolingList
    {
        static readonly object accessLock = new object();

        static List<Action<Keys, Keys, Keys>> actionList = new List<Action<Keys, Keys, Keys>>();
        static List<ActionListTrackingAndMapping> actionListMap = new List<ActionListTrackingAndMapping>();

        /// <summary>
        /// Calls the registered destination recievers.
        /// </summary>
        public static void CallKeyCommandInputDestinationRecievers(Keys k0, Keys k1, Keys k2)
        {
            for (int forindex = 0; forindex < actionListMap.Count; forindex++)
            {
                if (actionListMap[forindex].actionIsEnabled)
                    actionList[forindex]?.Invoke(k0, k1, k2);
            }
        }

        public static void InitialRegistration<T>(Action<Keys, Keys, Keys> methodToCall, T inst)
        {
            try
            {
                Monitor.Enter(accessLock);
                InitialRegistration(methodToCall, inst.GetHashCode());
            }
            catch (Exception e)
            {
                Monitor.Exit(accessLock);
                //Console.WriteLine(e.ToString());
            }
        }

        public static void UnRegister<T>(Action<Keys, Keys, Keys> methodToCall, T inst)
        {
            try
            {
                Monitor.Enter(accessLock);
                UnRegister(methodToCall, inst.GetHashCode());
            }
            catch (Exception e)
            {
                Monitor.Exit(accessLock);
                //Console.WriteLine(e.ToString());
            }
        }
        public static void Enable<T>(T inst)
        {
            Enable(inst.GetHashCode());
        }
        public static void Disable<T>(T inst)
        {
            Disable(inst.GetHashCode());
        }

        private static void InitialRegistration(Action<Keys, Keys, Keys> methodToCall, int id)
        {
            bool match = false;
            int index = actionListMap.Count;
            int theid = id;
            bool isEnabled = false;
            for (int forindex = 0; forindex < actionListMap.Count; forindex++)
            {
                var m = actionListMap[forindex];
                if (m.actionID == id)
                {
                    match = true;
                    index = forindex;
                    isEnabled = m.actionIsEnabled;
                    forindex = actionListMap.Count; // break
                }
            }
            if (match == false)
            {
                actionList.Add(methodToCall);
                actionListMap.Add(new ActionListTrackingAndMapping(id, index, true));
            }
        }

        private static void UnRegister(Action<Keys, Keys, Keys> methodToCall, int id)
        {
            bool match = false;
            int index = actionListMap.Count;
            int theid = id;
            bool isEnabled = false;
            for (int forindex = 0; forindex < actionListMap.Count; forindex++)
            {
                var m = actionListMap[forindex];
                if (m.actionID == id)
                {
                    match = true;
                    index = forindex;
                    isEnabled = m.actionIsEnabled;
                    forindex = actionListMap.Count; // break
                }
            }
            if (match == true) // its not in the list already user error throw msg.
            {
                actionList.RemoveAt(index);
                actionListMap.RemoveAt(index);
            }
        }

        private static void Enable(int id)
        {
            for (int forindex = 0; forindex < actionListMap.Count; forindex++)
            {
                if (actionListMap[forindex].actionID == id)
                {
                    actionListMap[forindex] = new ActionListTrackingAndMapping(id, forindex, true);
                    forindex = actionListMap.Count; // break
                }
            }
        }

        private static void Disable(int id)
        {
            for (int forindex = 0; forindex < actionListMap.Count; forindex++)
            {
                if (actionListMap[forindex].actionID == id)
                {
                    actionListMap[forindex] = new ActionListTrackingAndMapping(id, forindex, false);
                    forindex = actionListMap.Count; // break
                }
            }
        }

        public struct ActionListTrackingAndMapping
        {
            public int actionID { get; set; }
            public int actionIndexToListForId;
            public bool actionIsEnabled;

            public ActionListTrackingAndMapping(int id, int index, bool alive)
            {
                actionID = id;
                actionIndexToListForId = index;
                actionIsEnabled = alive;
            }
        }
    }

    #endregion
}


