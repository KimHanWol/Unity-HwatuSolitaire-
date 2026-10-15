using System;

namespace HwatuSolitaire.Core
{
    /// <summary>
    /// 스테이지 한 개. Resources/Stages/stages.json 에 300개가 들어 있다 (Tools/StageTool 로 만든다).
    /// 필드 이름은 JSON 키와 같다 (Unity JsonUtility와 System.Text.Json 둘 다 읽는다).
    /// </summary>
    [Serializable]
    public sealed class StageDef
    {
        /// <summary>1부터.</summary>
        public int number;
        /// <summary>판 모양 이름 (pyramid, steps, diamond, cross, twin, frame, tower). 참고용.</summary>
        public string shape;
        /// <summary>자리 [x0,y0,z0, x1,y1,z1, ...] — 반 칸 단위, z는 층.</summary>
        public int[] tiles;
        /// <summary>자리마다 화투패 번호 (tiles와 같은 순서).</summary>
        public int[] cells;
        /// <summary>이 시간(초) 안에 깨면 3광.</summary>
        public int star3;
        /// <summary>이 시간(초) 안에 깨면 2광. 제한 시간 안이면 1광.</summary>
        public int star2;
        /// <summary>제한 시간(초). 넘기면 실패.</summary>
        public int timeLimit;
        /// <summary>짝을 맞춘 뒤 두 장이 포개져 있는 시간(초). 이 안에 같은 달 셋째 패를 누르면 "쌌다".</summary>
        public float stackWindow;
        /// <summary>모의 풀이로 잰 예상 시간(초). 참고용.</summary>
        public float simSeconds;

        [NonSerialized] Layout layout;

        public Layout Layout => layout ?? (layout = Layout.FromFlat(tiles));

        public Board CreateBoard() => new Board(Layout, cells);

        /// <summary>걸린 시간으로 광 개수(1~3).</summary>
        public int StarsFor(float seconds)
        {
            if (seconds <= star3) return 3;
            if (seconds <= star2) return 2;
            return 1;
        }
    }

    [Serializable]
    public sealed class StageList
    {
        public int version;
        public StageDef[] stages;
    }
}
