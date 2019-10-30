
// Auto Generated Content Asset class file.
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Microsoft.Xna.Framework
{
    public static class Asset
    {

        #region  assets in Content

        public static List<Texture2D> Texture2DList = new List<Texture2D>();
        public static List<SpriteFont> SpriteFontList = new List<SpriteFont>();
        public static List<Effect> EffectList = new List<Effect>();

        public static SpriteFont font_MgGenFont;
        public static Texture2D texture_QuertyKeyBoardSmall;

        public static void LoadFrom_Content(Microsoft.Xna.Framework.Content.ContentManager Content)
        {
            Content.RootDirectory = @"Content";

            font_MgGenFont = Content.Load<SpriteFont>("MgGenFont");
            texture_QuertyKeyBoardSmall = Content.Load<Texture2D>("QuertyKeyBoardSmall");

            SpriteFontList.Add(font_MgGenFont);
            Texture2DList.Add(texture_QuertyKeyBoardSmall);


            Content.RootDirectory = @"Content";
        }

        #endregion

    }
}