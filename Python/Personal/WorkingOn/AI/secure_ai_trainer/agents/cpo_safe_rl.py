from omnisafe.algorithms import CPO

def train_cpo(env, cost_limit=10.0):
    agent = CPO(env, cost_limit=cost_limit)
    agent.train(total_timesteps=100_000)
    return agent
