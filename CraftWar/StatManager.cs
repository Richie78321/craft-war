using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CraftWar
{
    public class StatManager
    {
        public int defaultHealth;
        private int _health;
        public int health
        {
            get
            {
                return _health;
            }
            set
            {
                if (value <= 0)
                {
                    OnDeath.Invoke(this, null);
                }

                _health = value;
            }
        }
        public EventHandler OnDeath;

        public StatManager(int health, EventHandler OnDeathAction = null)
        {
            defaultHealth = health;
            _health = health;

            if (OnDeathAction == null)
            {
                OnDeath = new EventHandler((sender, e) => { });
            }
            else
            {
                OnDeath = OnDeathAction;
            }
        }
    }
}
