
public class UnitData 
{
    public string Name { get; protected set; }
    public int Hp { get; protected set; }
    public int MaxHp { get; protected set; }
    public int Atk { get; protected set; }
    public int AtkMax { get; protected set; }
    public int Def { get; protected set; }
    public int DefMax { get; protected set; }
    public float Speed { get; protected set; }
    public float SpeedMax { get; protected set; }

    public virtual void SetName(string name)
    {
        Name = name;
    }

    public virtual void SetHp(int hp)
    {
        Hp = hp;
    }

    public virtual void SetMaxHp(int maxHp)
    {
        MaxHp = maxHp;
    }

    public virtual void SetAtk(int atk)
    {
        Atk = atk;
    }

    public virtual void SetAtkMax(int atkMax)
    {
        AtkMax = atkMax;
    }

    public virtual void SetDef(int def)
    {
        Def = def;
    }

    public virtual void SetDefMax(int defMax)
    {
        DefMax = defMax;
    }

    public virtual void SetSpeed(float speed)
    {
        Speed = speed;
    }

    public virtual void SetSpeedMax(float speedMax)
    {
        SpeedMax = speedMax;
    }
}
