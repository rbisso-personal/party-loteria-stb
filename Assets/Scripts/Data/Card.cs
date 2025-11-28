using System;

namespace PartyLoteria.Data
{
    [Serializable]
    public class Card
    {
        public int id;
        public string name_es;
        public string name_en;
        public string verse_es;
        public string verse_en;
        public string image;
        public string vo_es;
        public string vo_en;

        public string GetName(string language = "es")
        {
            return language == "en" ? name_en : name_es;
        }

        public string GetVerse(string language = "es")
        {
            return language == "en" ? verse_en : verse_es;
        }
    }

    [Serializable]
    public class CardData
    {
        public string version;
        public int totalCards;
        public Card[] cards;
    }
}
