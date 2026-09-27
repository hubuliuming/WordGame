using System;

namespace Code_01.Enemy
{
    [Serializable]
    public struct EnemyData
    {
        public string Name;
        public int HP;
        public int Attack;
        public int Defence;
        public int Speed;

        public int CostPower;
        public Award award;

        //奖励
        [Serializable]
        public struct Award
        {
            public int Exp;
            public int Coin;
            public string GoodsName;
        }

    }
}
