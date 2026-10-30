import gym
import psutil

class ResourceLimiter(gym.Wrapper):
    def __init__(self, env, max_cpu: float, max_ram_mb: float):
        super().__init__(env)
        self.max_cpu = max_cpu
        self.max_ram_mb = max_ram_mb

    def step(self, action):
        obs, reward, done, info = self.env.step(action)
        proc = psutil.Process()
        cpu = proc.cpu_percent(interval=None)
        ram = proc.memory_info().rss / (1024 * 1024)
        if cpu > self.max_cpu * 100 or ram > self.max_ram_mb:
            reward *= 0.5
            info['resource_violation'] = True
        return obs, reward, done, info
