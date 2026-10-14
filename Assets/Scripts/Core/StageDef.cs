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
        public int width;
        public int height;
        /// <summary>행 우선(y * width + x). 패 번호 0~47, -1 빈칸, -2 막힌 칸.</summary>
        public int[] cells;
        /// <summary>GravityMode 값.</summary>
        public int gravity;
        /// <summary>이 시간(초) 안에 깨면 별 3개.</summary>
        public int star3;
        /// <summary>이 시간(초) 안에 깨면 별 2개. 제한 시간 안이면 별 1개.</summary>
        public int star2;
        /// <summary>제한 시간(초). 넘기면 실패.</summary>
        public int timeLimit;
        /// <summary>짝을 맞춘 뒤 이 시간(초) 안에 다음 짝을 맞춰야 연속(콤보)으로 친다.</summary>
        public float comboWindow;
        /// <summary>모의 풀이로 잰 예상 시간(초). 참고용.</summary>
        public float simSeconds;

        public GravityMode Gravity => (GravityMode)gravity;

        public Board CreateBoard() => new Board(width, height, cells);

        /// <summary>걸린 시간으로 별 개수(0~3). 제한 시간을 넘겼으면 1개(부활해서 깬 경우).</summary>
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
