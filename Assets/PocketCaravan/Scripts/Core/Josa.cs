namespace PocketCaravan
{
    /// <summary>한국어 조사: 받침 유무에 따라 을/를, 이/가, 은/는, 으로/로를 고른다.</summary>
    public static class Josa
    {
        static bool HasBatchim(string w, bool rieulAsNone = false)
        {
            if (string.IsNullOrEmpty(w)) return false;
            char c = w[w.Length - 1];
            if (c < 0xAC00 || c > 0xD7A3) return false;
            int jong = (c - 0xAC00) % 28;
            if (rieulAsNone && jong == 8) return false;   // ㄹ 받침은 '로'
            return jong != 0;
        }

        public static string Eul(string w) => w + (HasBatchim(w) ? "을" : "를");
        public static string Ga(string w) => w + (HasBatchim(w) ? "이" : "가");
        public static string Eun(string w) => w + (HasBatchim(w) ? "은" : "는");
        public static string Ro(string w) => w + (HasBatchim(w, true) ? "으로" : "로");
    }
}
