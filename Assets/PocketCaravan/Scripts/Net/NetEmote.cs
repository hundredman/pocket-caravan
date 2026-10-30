namespace PocketCaravan
{
    /// <summary>
    /// 리액션을 온라인의 같은 맵 사람들에게 보여 주는 입구 (2026-10-30, feature/online 8단계).
    /// 지역 리액션 코드에서 동작을 시작할 때 한 줄만 부르면 된다:  NetEmote.Send("dance", "춤");
    /// 접속하지 않았으면 아무것도 하지 않는다. 받는 쪽은 RemotePlayers가 그 사람 캐릭터를 움직이고 머리 위에 크게 띄운다.
    /// 아이디로 동작 모양을 고른다 (bow·wave·nod·pray 인사 / hop·joy·surprise 깡총 / clap 박수 / spin·mask 빙글 /
    /// dance·row·stretch 춤 / love 하트 / nap·think 쉬기). 모르는 아이디는 깡총으로 보인다.
    /// </summary>
    public static class NetEmote
    {
        public static void Send(string id, string name)
        {
            var net = NetClient.I;
            if (net != null) net.Emote(id, name);
        }
    }
}
