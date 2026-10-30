import resource

def limit_resources(max_cpu_seconds: int, max_ram_bytes: int):
    resource.setrlimit(resource.RLIMIT_CPU, (max_cpu_seconds, max_cpu_seconds))
    resource.setrlimit(resource.RLIMIT_AS, (max_ram_bytes, max_ram_bytes))
