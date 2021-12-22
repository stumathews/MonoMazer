//-----------------------------------------------------------------------

// <copyright file="FileSaver.cs" company="Stuart Mathews">

// Copyright (c) Stuart Mathews. All rights reserved.

// <author>Stuart Mathews</author>

// <date>03/10/2021 13:16:11</date>

// </copyright>

//-----------------------------------------------------------------------

namespace MazerPlatformer
{
    public class FileSaver : IFileSaver
    {
        public LevelDetails SaveLevelFile(LevelDetails levelDetails, string filename)
        {
            GameLib.Files.Xml.SerializeObject(filename, levelDetails);
            return GameLib.Files.Xml.DeserializeFile<LevelDetails>(filename);
        }
    }
}
