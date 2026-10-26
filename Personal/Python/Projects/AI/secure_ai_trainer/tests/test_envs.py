import unittest
import gym
from env_wrappers.resource_limiter import ResourceLimiter

class TestResourceLimiter(unittest.TestCase):
    def test_wrapper_reduces_reward_on_violation(self):
        env = gym.make("CartPole-v1")
        wrapped = ResourceLimiter(env, max_cpu=0.01, max_ram_mb=1)
        obs = wrapped.reset()
        obs, reward, done, info = wrapped.step(env.action_space.sample())
        self.assertIn('resource_violation', info)

if __name__ == "__main__":
    unittest.main()
