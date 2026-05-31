using System;

namespace DungeonRpg
{
    [Serializable]
    public struct BilingualText
    {
        public string Spanish;
        public string English;

        public BilingualText(string spanish, string english)
        {
            Spanish = spanish;
            English = english;
        }

        public string Display()
        {
            return $"{Spanish} / {English}";
        }

        public string Format(params object[] values)
        {
            return $"{string.Format(Spanish, values)} / {string.Format(English, values)}";
        }
    }
}
