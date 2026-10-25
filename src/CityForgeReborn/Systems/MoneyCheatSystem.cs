using Game;
using Game.City;
using Game.Simulation;
using Unity.Entities;

namespace CityForgeReborn.Systems
{
    /// <summary>Adds/removes city funds and keeps the balance topped up when Infinite Money is on.</summary>
    public partial class MoneyCheatSystem : GameSystemBase
    {
        private const int kInfiniteFloor = 1_000_000_000;

        private CitySystem m_CitySystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            m_CitySystem = World.GetOrCreateSystemManaged<CitySystem>();
        }

        public override int GetUpdateInterval(SystemUpdatePhase phase) => 64;

        protected override void OnUpdate()
        {
            if (!Mod.Settings.InfiniteMoney || !TryGetMoney(out var money))
                return;

            if (money.money < kInfiniteFloor)
            {
                money.Add(kInfiniteFloor - money.money);
                EntityManager.SetComponentData(m_CitySystem.City, money);
            }
        }

        /// <summary>Adds (or, with a negative amount, removes) money without overflowing.</summary>
        public void AddMoney(int amount)
        {
            if (!TryGetMoney(out var money))
                return;

            long target = (long)money.money + amount;
            if (target > int.MaxValue)
                amount = int.MaxValue - money.money;
            else if (target < int.MinValue)
                amount = int.MinValue - money.money;

            if (amount >= 0)
                money.Add(amount);
            else
                money.Subtract(-amount);

            EntityManager.SetComponentData(m_CitySystem.City, money);
        }

        private bool TryGetMoney(out PlayerMoney money)
        {
            var city = m_CitySystem.City;
            if (city == Entity.Null || !EntityManager.HasComponent<PlayerMoney>(city))
            {
                money = default;
                return false;
            }

            money = EntityManager.GetComponentData<PlayerMoney>(city);
            return true;
        }
    }
}
