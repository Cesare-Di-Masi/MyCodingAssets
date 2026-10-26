from torch.utils.tensorboard import SummaryWriter

class Monitor:
    def __init__(self, log_dir):
        self.writer = SummaryWriter(log_dir)

    def log(self, step: int, reward: float, cost: float):
        self.writer.add_scalar('Reward/episode', reward, step)
        self.writer.add_scalar('Cost/episode', cost, step)
        self.writer.flush()
