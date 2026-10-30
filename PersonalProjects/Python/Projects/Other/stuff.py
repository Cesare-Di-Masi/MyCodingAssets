#!/usr/bin/env python3
"""
Arithmetic Evolution Lab — Genetic Programming Engine
=====================================================
A powerful GP system for evolving mathematical expressions.

Features:
- Multiple selection strategies (tournament, roulette, rank, SUS)
- Advanced mutation operators (subtree, point, hoist, constant perturbation)
- Bloat control (parsimony pressure, depth limiting, operator restrictions)
- Multi-objective optimization (accuracy + simplicity)
- Parallel fitness evaluation
- SQLite persistence
- Rich terminal UI with live updates
- Matplotlib visualization export
"""

from __future__ import annotations

import abc
import copy
import json
import math
import multiprocessing as mp
import os
import random
import sqlite3
import sys
import time
from dataclasses import dataclass, field
from enum import Enum, auto
from functools import lru_cache
from pathlib import Path
from typing import Any, Callable, Optional, TypeAlias

# Optional imports with graceful fallback
try:
    import matplotlib
    matplotlib.use("Agg")  # Non-interactive backend
    import matplotlib.pyplot as plt
    import matplotlib.patches as mpatches
    HAS_MATPLOTLIB = True
except ImportError:
    HAS_MATPLOTLIB = False

try:
    from rich.console import Console
    from rich.layout import Layout
    from rich.live import Live
    from rich.panel import Panel
    from rich.progress import Progress, SpinnerColumn, TextColumn, BarColumn
    from rich.table import Table
    from rich.text import Text
    from rich.tree import Tree
    from rich import box
    HAS_RICH = True
except ImportError:
    HAS_RICH = False


# ═══════════════════════════════════════════════════════════════
# CONFIGURATION
# ═══════════════════════════════════════════════════════════════

@dataclass(frozen=True)
class GPConfig:
    """Configuration for the genetic programming engine."""
    # Population
    population_size: int = 200
    elite_count: int = 5
    
    # Tree structure
    init_method: str = "ramped_half_and_half"  # "full", "grow", "ramped_half_and_half"
    init_min_depth: int = 2
    init_max_depth: int = 4
    max_depth: int = 8
    max_nodes: int = 50
    
    # Operators
    crossover_rate: float = 0.85
    mutation_rate: float = 0.10
    reproduction_rate: float = 0.05
    
    # Mutation subtypes (probabilities must sum to 1.0)
    subtree_mutation_rate: float = 0.5
    point_mutation_rate: float = 0.2
    hoist_mutation_rate: float = 0.1
    constant_perturbation_rate: float = 0.2
    
    # Selection
    selection_method: str = "tournament"  # "tournament", "roulette", "rank", "sus"
    tournament_size: int = 5
    tournament_pressure: float = 0.9  # Probability of picking best in tournament
    
    # Bloat control
    parsimony_coefficient: float = 0.0  # 0 = disabled, >0 = penalize size
    operator_protection: bool = True  # Protect structure during mutation
    
    # Termination
    max_generations: int = 1000
    target_fitness: float = 0.0  # Stop when fitness <= this
    stagnation_limit: int = 50  # Generations without improvement before restart
    
    # Parallelism
    n_workers: int = 0  # 0 = auto-detect, -1 = single process


# ═══════════════════════════════════════════════════════════════
# EXPRESSION TREE
# ═══════════════════════════════════════════════════════════════

class NodeType(Enum):
    CONSTANT = auto()
    VARIABLE = auto()
    UNARY_OP = auto()
    BINARY_OP = auto()


@dataclass
class Node:
    """A node in the expression tree."""
    type: NodeType
    value: Optional[float] = None  # For constants
    var_name: Optional[str] = None  # For variables
    op: Optional[str] = None  # For operators
    children: list[Node] = field(default_factory=list)
    
    def __post_init__(self):
        if self.type == NodeType.BINARY_OP:
            assert len(self.children) == 2, "Binary op needs 2 children"
        elif self.type == NodeType.UNARY_OP:
            assert len(self.children) == 1, "Unary op needs 1 child"
        elif self.type in (NodeType.CONSTANT, NodeType.VARIABLE):
            assert len(self.children) == 0, "Leaves have no children"
    
    def copy(self) -> Node:
        return Node(
            type=self.type,
            value=self.value,
            var_name=self.var_name,
            op=self.op,
            children=[c.copy() for c in self.children]
        )
    
    def depth(self) -> int:
        if not self.children:
            return 1
        return 1 + max(c.depth() for c in self.children)
    
    def node_count(self) -> int:
        return 1 + sum(c.node_count() for c in self.children)
    
    def __eq__(self, other):
        if not isinstance(other, Node):
            return False
        return (self.type == other.type and 
                self.value == other.value and
                self.var_name == other.var_name and
                self.op == other.op and
                self.children == other.children)
    
    def __hash__(self):
        return hash((self.type, self.value, self.var_name, self.op))


# ═══════════════════════════════════════════════════════════════
# FUNCTION SET & TERMINAL SET
# ═══════════════════════════════════════════════════════════════

class UnaryOp:
    """Safe unary operations."""
    NEG = "neg"
    ABS = "abs"
    SQRT = "sqrt"
    LOG = "log"
    EXP = "exp"
    SIN = "sin"
    COS = "cos"
    
    FUNCTIONS: dict[str, Callable[[float], float]] = {
        NEG: lambda x: -x,
        ABS: lambda x: abs(x),
        SQRT: lambda x: math.sqrt(x) if x >= 0 else float('nan'),
        LOG: lambda x: math.log(x) if x > 0 else float('nan'),
        EXP: lambda x: math.exp(min(x, 500)),  # Prevent overflow
        SIN: lambda x: math.sin(x),
        COS: lambda x: math.cos(x),
    }


class BinaryOp:
    """Safe binary operations."""
    ADD = "+"
    SUB = "-"
    MUL = "*"
    DIV = "/"
    POW = "^"
    MOD = "%"
    MAX = "max"
    MIN = "min"
    
    FUNCTIONS: dict[str, Callable[[float, float], float]] = {
        ADD: lambda a, b: a + b,
        SUB: lambda a, b: a - b,
        MUL: lambda a, b: a * b,
        DIV: lambda a, b: a / b if abs(b) > 1e-10 else float('nan'),
        POW: lambda a, b: a ** b if (abs(a) < 100 and abs(b) < 10) else float('nan'),
        MOD: lambda a, b: a % b if abs(b) > 1e-10 else float('nan'),
        MAX: lambda a, b: max(a, b),
        MIN: lambda a, b: min(a, b),
    }
    
    ARITY: dict[str, int] = {op: 2 for op in FUNCTIONS}


# ═══════════════════════════════════════════════════════════════
# TREE OPERATIONS
# ═══════════════════════════════════════════════════════════════

class TreeOps:
    """Operations for creating and manipulating expression trees."""
    
    def __init__(
        self,
        constants: list[float] | None = None,
        variables: list[str] | None = None,
        binary_ops: list[str] | None = None,
        unary_ops: list[str] | None = None,
    ):
        self.constants = constants or [0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10]
        self.variables = variables or ["x"]
        self.binary_ops = binary_ops or list(BinaryOp.FUNCTIONS.keys())
        self.unary_ops = unary_ops or []
        
        # Build terminal and function sets
        self.terminals = (
            [NodeType.CONSTANT] * len(self.constants) +
            [NodeType.VARIABLE] * len(self.variables)
        )
        self.functions = (
            [NodeType.BINARY_OP] * len(self.binary_ops) +
            [NodeType.UNARY_OP] * len(self.unary_ops)
        )
    
    def random_terminal(self) -> Node:
        """Generate a random terminal node."""
        choice = random.choice(self.terminals)
        if choice == NodeType.CONSTANT:
            return Node(type=NodeType.CONSTANT, value=random.choice(self.constants))
        else:
            return Node(type=NodeType.VARIABLE, var_name=random.choice(self.variables))
    
    def random_function(self) -> Node:
        """Generate a random function node (without children)."""
        choice = random.choice(self.functions)
        if choice == NodeType.BINARY_OP:
            return Node(type=NodeType.BINARY_OP, op=random.choice(self.binary_ops))
        else:
            return Node(type=NodeType.UNARY_OP, op=random.choice(self.unary_ops))
    
    def generate_full(self, depth: int) -> Node:
        """Generate a full tree (all branches reach max depth)."""
        if depth <= 1:
            return self.random_terminal()
        
        node = self.random_function()
        arity = 2 if node.type == NodeType.BINARY_OP else 1
        node.children = [self.generate_full(depth - 1) for _ in range(arity)]
        return node
    
    def generate_grow(self, depth: int) -> Node:
        """Generate a grown tree (branches may terminate early)."""
        if depth <= 1 or (depth > 1 and random.random() < 0.3):
            return self.random_terminal()
        
        node = self.random_function()
        arity = 2 if node.type == NodeType.BINARY_OP else 1
        node.children = [self.generate_grow(depth - 1) for _ in range(arity)]
        return node
    
    def generate_ramped_half_and_half(
        self, 
        min_depth: int, 
        max_depth: int, 
        population_size: int
    ) -> list[Node]:
        """Generate population using ramped half-and-half initialization."""
        population = []
        depth_range = range(min_depth, max_depth + 1)
        per_depth = population_size // len(depth_range)
        
        for depth in depth_range:
            for i in range(per_depth):
                if i % 2 == 0:
                    population.append(self.generate_full(depth))
                else:
                    population.append(self.generate_grow(depth))
        
        # Fill remainder
        while len(population) < population_size:
            depth = random.choice(list(depth_range))
            population.append(self.generate_grow(depth))
        
        return population
    
    def evaluate(self, node: Node, env: dict[str, float] | None = None) -> float:
        """Safely evaluate a tree."""
        env = env or {}
        
        if node.type == NodeType.CONSTANT:
            return node.value if node.value is not None else 0.0
        
        if node.type == NodeType.VARIABLE:
            return env.get(node.var_name or "x", 0.0)
        
        if node.type == NodeType.UNARY_OP:
            child_val = self.evaluate(node.children[0], env)
            if not math.isfinite(child_val):
                return float('nan')
            func = UnaryOp.FUNCTIONS.get(node.op or "", lambda x: float('nan'))
            result = func(child_val)
            return result if math.isfinite(result) else float('nan')
        
        if node.type == NodeType.BINARY_OP:
            left_val = self.evaluate(node.children[0], env)
            right_val = self.evaluate(node.children[1], env)
            if not (math.isfinite(left_val) and math.isfinite(right_val)):
                return float('nan')
            func = BinaryOp.FUNCTIONS.get(node.op or "", lambda a, b: float('nan'))
            result = func(left_val, right_val)
            return result if math.isfinite(result) else float('nan')
        
        return float('nan')
    
    def to_string(self, node: Node) -> str:
        """Convert tree to string expression."""
        if node.type == NodeType.CONSTANT:
            return str(node.value)
        if node.type == NodeType.VARIABLE:
            return node.var_name or "x"
        if node.type == NodeType.UNARY_OP:
            child = self.to_string(node.children[0])
            if node.op == "neg":
                return f"-{child}"
            return f"{node.op}({child})"
        if node.type == NodeType.BINARY_OP:
            left = self.to_string(node.children[0])
            right = self.to_string(node.children[1])
            # Add parens for clarity
            if node.op in ("+", "-"):
                return f"({left} {node.op} {right})"
            return f"({left} {node.op} {right})"
        return "?"
    
    def to_lisp(self, node: Node) -> str:
        """Convert tree to Lisp-style S-expression."""
        if node.type == NodeType.CONSTANT:
            return str(node.value)
        if node.type == NodeType.VARIABLE:
            return node.var_name or "x"
        if node.type == NodeType.UNARY_OP:
            return f"({node.op} {self.to_lisp(node.children[0])})"
        if node.type == NodeType.BINARY_OP:
            return f"({node.op} {self.to_lisp(node.children[0])} {self.to_lisp(node.children[1])})"
        return "?"
    
    def get_all_nodes(self, node: Node) -> list[tuple[Node, list[int]]]:
        """Get all nodes with their paths from root."""
        result = [(node, [])]
        for i, child in enumerate(node.children):
            for child_node, path in self.get_all_nodes(child):
                result.append((child_node, [i] + path))
        return result
    
    def get_node_at_path(self, node: Node, path: list[int]) -> Node:
        """Get node at specified path."""
        current = node
        for idx in path:
            current = current.children[idx]
        return current
    
    def set_node_at_path(self, node: Node, path: list[int], new_node: Node) -> Node:
        """Return a new tree with node at path replaced."""
        if not path:
            return new_node.copy()
        
        clone = node.copy()
        current = clone
        for idx in path[:-1]:
            current = current.children[idx]
        current.children[path[-1]] = new_node.copy()
        return clone
    
    def get_subtrees(self, node: Node, max_depth: int = 3) -> list[Node]:
        """Get all subtrees up to max_depth."""
        result = []
        all_nodes = self.get_all_nodes(node)
        for subtree, _ in all_nodes:
            if subtree.depth() <= max_depth:
                result.append(subtree)
        return result


# ═══════════════════════════════════════════════════════════════
# GENETIC OPERATORS
# ═══════════════════════════════════════════════════════════════

class GeneticOperators:
    """Genetic operators for GP."""
    
    def __init__(self, tree_ops: TreeOps, config: GPConfig):
        self.ops = tree_ops
        self.config = config
    
    def subtree_crossover(self, parent1: Node, parent2: Node) -> tuple[Node, Node]:
        """Swap random subtrees between two parents."""
        nodes1 = self.ops.get_all_nodes(parent1)
        nodes2 = self.ops.get_all_nodes(parent2)
        
        # Filter to internal nodes only for more meaningful crossovers
        internal1 = [(n, p) for n, p in nodes1 if n.children]
        internal2 = [(n, p) for n, p in nodes2 if n.children]
        
        if not internal1 or not internal2:
            return parent1.copy(), parent2.copy()
        
        _, path1 = random.choice(internal1)
        _, path2 = random.choice(internal2)
        
        subtree2 = self.ops.get_node_at_path(parent2, path2)
        subtree1 = self.ops.get_node_at_path(parent1, path1)
        
        child1 = self.ops.set_node_at_path(parent1, path1, subtree2)
        child2 = self.ops.set_node_at_path(parent2, path2, subtree1)
        
        return child1, child2
    
    def subtree_mutation(self, individual: Node) -> Node:
        """Replace a random subtree with a new random tree."""
        nodes = self.ops.get_all_nodes(individual)
        _, path = random.choice(nodes)
        
        max_new_depth = min(3, self.config.max_depth - len(path))
        if max_new_depth < 1:
            max_new_depth = 1
        
        new_subtree = self.ops.generate_grow(max_new_depth + 1)
        return self.ops.set_node_at_path(individual, path, new_subtree)
    
    def point_mutation(self, individual: Node) -> Node:
        """Change a single node's value/operator."""
        nodes = self.ops.get_all_nodes(individual)
        node, path = random.choice(nodes)
        
        if node.type == NodeType.CONSTANT:
            # Perturb constant
            new_value = node.value + random.gauss(0, 1)
            new_node = Node(type=NodeType.CONSTANT, value=new_value)
        elif node.type == NodeType.VARIABLE:
            # Change variable
            new_var = random.choice(self.ops.variables)
            new_node = Node(type=NodeType.VARIABLE, var_name=new_var)
        elif node.type == NodeType.BINARY_OP:
            # Change operator
            new_op = random.choice(self.ops.binary_ops)
            new_node = Node(type=NodeType.BINARY_OP, op=new_op, 
                          children=[c.copy() for c in node.children])
        elif node.type == NodeType.UNARY_OP:
            new_op = random.choice(self.ops.unary_ops) if self.ops.unary_ops else "neg"
            new_node = Node(type=NodeType.UNARY_OP, op=new_op,
                          children=[node.children[0].copy()])
        else:
            return individual.copy()
        
        return self.ops.set_node_at_path(individual, path, new_node)
    
    def hoist_mutation(self, individual: Node) -> Node:
        """Replace tree with a random subtree of itself (reduces size)."""
        subtrees = [n for n, _ in self.ops.get_all_nodes(individual) if n.children]
        if not subtrees:
            return individual.copy()
        return random.choice(subtrees).copy()
    
    def constant_perturbation(self, individual: Node) -> Node:
        """Slightly perturb all constants in the tree."""
        def perturb(node: Node) -> Node:
            if node.type == NodeType.CONSTANT:
                perturbation = random.gauss(0, 0.5)
                return Node(type=NodeType.CONSTANT, value=node.value + perturbation)
            return Node(
                type=node.type,
                value=node.value,
                var_name=node.var_name,
                op=node.op,
                children=[perturb(c) for c in node.children]
            )
        return perturb(individual)
    
    def mutate(self, individual: Node) -> Node:
        """Apply mutation based on configured probabilities."""
        r = random.random()
        cfg = self.config
        
        if r < cfg.subtree_mutation_rate:
            result = self.subtree_mutation(individual)
        elif r < cfg.subtree_mutation_rate + cfg.point_mutation_rate:
            result = self.point_mutation(individual)
        elif r < cfg.subtree_mutation_rate + cfg.point_mutation_rate + cfg.hoist_mutation_rate:
            result = self.hoist_mutation(individual)
        else:
            result = self.constant_perturbation(individual)
        
        # Enforce depth limit
        if result.depth() > cfg.max_depth:
            return individual.copy()
        if result.node_count() > cfg.max_nodes:
            return individual.copy()
        
        return result


# ═══════════════════════════════════════════════════════════════
# SELECTION METHODS
# ═══════════════════════════════════════════════════════════════

class Selection:
    """Selection methods for GP."""
    
    @staticmethod
    def tournament(
        population: list[tuple[Node, float]], 
        tournament_size: int,
        pressure: float = 0.9
    ) -> Node:
        """Tournament selection with adjustable pressure."""
        candidates = random.sample(population, min(tournament_size, len(population)))
        candidates.sort(key=lambda x: x[1])  # Lower fitness = better
        
        # Probabilistic selection of best
        for individual, fitness in candidates:
            if random.random() < pressure:
                return individual.copy()
        return candidates[0][0].copy()
    
    @staticmethod
    def roulette(population: list[tuple[Node, float]], minimize: bool = True) -> Node:
        """Roulette wheel selection."""
        if minimize:
            # Convert to maximization by inverting
            max_fit = max(f for _, f in population)
            weights = [max_fit - f + 1e-6 for _, f in population]
        else:
            weights = [f for _, f in population]
        
        total = sum(weights)
        if total <= 0:
            return random.choice(population)[0].copy()
        
        r = random.random() * total
        cumulative = 0
        for (individual, _), weight in zip(population, weights):
            cumulative += weight
            if cumulative >= r:
                return individual.copy()
        return population[-1][0].copy()
    
    @staticmethod
    def rank(population: list[tuple[Node, float]], minimize: bool = True) -> Node:
        """Rank-based selection."""
        sorted_pop = sorted(population, key=lambda x: x[1], reverse=not minimize)
        n = len(sorted_pop)
        weights = [i + 1 for i in range(n)]
        total = sum(weights)
        
        r = random.random() * total
        cumulative = 0
        for (individual, _), weight in zip(sorted_pop, weights):
            cumulative += weight
            if cumulative >= r:
                return individual.copy()
        return sorted_pop[-1][0].copy()
    
    @staticmethod
    def sus(population: list[tuple[Node, float]], n: int, minimize: bool = True) -> list[Node]:
        """Stochastic Universal Sampling - select n individuals at once."""
        if minimize:
            max_fit = max(f for _, f in population)
            weights = [max_fit - f + 1e-6 for _, f in population]
        else:
            weights = [f for _, f in population]
        
        total = sum(weights)
        if total <= 0:
            return [random.choice(population)[0].copy() for _ in range(n)]
        
        step = total / n
        start = random.random() * step
        points = [start + i * step for i in range(n)]
        
        selected = []
        cumulative = 0
        pop_iter = iter(zip(population, weights))
        (individual, _), weight = next(pop_iter)
        
        for point in points:
            while cumulative + weight < point:
                try:
                    (individual, _), weight = next(pop_iter)
                except StopIteration:
                    break
                cumulative += weight
            selected.append(individual.copy())
            cumulative += weight
        
        return selected


# ═══════════════════════════════════════════════════════════════
# FITNESS FUNCTIONS
# ═══════════════════════════════════════════════════════════════

class FitnessFunction(abc.ABC):
    """Abstract base class for fitness functions."""
    
    @abc.abstractmethod
    def evaluate(self, tree: Node, tree_ops: TreeOps) -> float:
        """Return fitness value (lower is better)."""
        pass
    
    @abc.abstractmethod
    def is_solution(self, fitness: float) -> bool:
        """Check if this fitness represents a solution."""
        pass


class TargetFitness(FitnessFunction):
    """Fitness for hitting a specific target number."""
    
    def __init__(self, target: float, numbers: list[float] | None = None):
        self.target = target
        self.numbers = numbers
    
    def evaluate(self, tree: Node, tree_ops: TreeOps) -> float:
        if self.numbers:
            # Create environment with the numbers
            env = {f"x{i}": v for i, v in enumerate(self.numbers)}
            env["x"] = self.numbers[0] if self.numbers else 0
        else:
            env = {"x": 0}
        
        result = tree_ops.evaluate(tree, env)
        if not math.isfinite(result):
            return 1e9
        
        distance = abs(result - self.target)
        return distance
    
    def is_solution(self, fitness: float) -> bool:
        return fitness < 1e-9


class RegressionFitness(FitnessFunction):
    """Fitness for symbolic regression (fitting data points)."""
    
    def __init__(self, data: list[tuple[dict[str, float], float]]):
        self.data = data
    
    def evaluate(self, tree: Node, tree_ops: TreeOps) -> float:
        total_error = 0.0
        valid_count = 0
        
        for env, target in self.data:
            result = tree_ops.evaluate(tree, env)
            if math.isfinite(result):
                total_error += (result - target) ** 2
                valid_count += 1
            else:
                total_error += 1e6
        
        if valid_count == 0:
            return 1e9
        
        mse = total_error / len(self.data)
        return mse
    
    def is_solution(self, fitness: float) -> bool:
        return fitness < 1e-6


class MultiObjectiveFitness(FitnessFunction):
    """Combined fitness with parsimony pressure."""
    
    def __init__(self, base_fitness: FitnessFunction, parsimony_coeff: float = 0.01):
        self.base_fitness = base_fitness
        self.parsimony_coeff = parsimony_coeff
    
    def evaluate(self, tree: Node, tree_ops: TreeOps) -> float:
        base = self.base_fitness.evaluate(tree, tree_ops)
        size_penalty = tree.node_count() * self.parsimony_coeff
        return base + size_penalty
    
    def is_solution(self, fitness: float) -> bool:
        return self.base_fitness.is_solution(fitness)


# ═══════════════════════════════════════════════════════════════
# GP ENGINE
# ═══════════════════════════════════════════════════════════════

@dataclass
class EvolutionResult:
    """Result of evolution run."""
    best_individual: Node
    best_fitness: float
    generations: int
    evaluations: int
    solution_found: bool
    history: list[dict[str, Any]]
    final_population: list[tuple[Node, float]]
    time_elapsed: float


class GPEngine:
    """Main genetic programming engine."""
    
    def __init__(
        self,
        config: GPConfig | None = None,
        constants: list[float] | None = None,
        variables: list[str] | None = None,
        binary_ops: list[str] | None = None,
        unary_ops: list[str] | None = None,
    ):
        self.config = config or GPConfig()
        self.tree_ops = TreeOps(constants, variables, binary_ops, unary_ops)
        self.genetic_ops = GeneticOperators(self.tree_ops, self.config)
        self.selection = Selection()
        
        # Statistics
        self.evaluation_count = 0
        self.generation_count = 0
        self.history: list[dict[str, Any]] = []
        
        # Callbacks
        self.on_generation: Callable[[int, Node, float, list[tuple[Node, float]]], None] | None = None
    
    def _evaluate_individual(
        self, 
        individual: Node, 
        fitness_fn: FitnessFunction
    ) -> float:
        """Evaluate a single individual."""
        self.evaluation_count += 1
        return fitness_fn.evaluate(individual, self.tree_ops)
    
    def _evaluate_population(
        self,
        population: list[Node],
        fitness_fn: FitnessFunction,
        parallel: bool = False
    ) -> list[tuple[Node, float]]:
        """Evaluate entire population, optionally in parallel."""
        if parallel and self.config.n_workers != -1:
            n_workers = self.config.n_workers or mp.cpu_count()
            if n_workers > 1 and len(population) > 10:
                return self._evaluate_parallel(population, fitness_fn, n_workers)
        
        return [(ind, self._evaluate_individual(ind, fitness_fn)) for ind in population]
    
    def _evaluate_parallel(
        self,
        population: list[Node],
        fitness_fn: FitnessFunction,
        n_workers: int
    ) -> list[tuple[Node, float]]:
        """Evaluate population using multiprocessing."""
        # Note: This requires fitness_fn to be picklable
        with mp.Pool(n_workers) as pool:
            # Simplified - in practice you'd need to serialize the tree
            results = pool.map(
                lambda ind: (ind, fitness_fn.evaluate(ind, self.tree_ops)),
                population
            )
        self.evaluation_count += len(population)
        return results
    
    def _select(self, scored_pop: list[tuple[Node, float]]) -> Node:
        """Select one individual using configured method."""
        method = self.config.selection_method
        
        if method == "tournament":
            return self.selection.tournament(
                scored_pop, 
                self.config.tournament_size,
                self.config.tournament_pressure
            )
        elif method == "roulette":
            return self.selection.roulette(scored_pop)
        elif method == "rank":
            return self.selection.rank(scored_pop)
        elif method == "sus":
            selected = self.selection.sus(scored_pop, 1)
            return selected[0]
        else:
            return self.selection.tournament(scored_pop, self.config.tournament_size)
    
    def _create_offspring(
        self,
        scored_pop: list[tuple[Node, float]]
    ) -> list[Node]:
        """Create next generation from scored population."""
        cfg = self.config
        offspring = []
        
        while len(offspring) < cfg.population_size - cfg.elite_count:
            r = random.random()
            
            if r < cfg.crossover_rate:
                # Crossover
                parent1 = self._select(scored_pop)
                parent2 = self._select(scored_pop)
                child1, child2 = self.genetic_ops.subtree_crossover(parent1, parent2)
                offspring.append(child1)
                if len(offspring) < cfg.population_size - cfg.elite_count:
                    offspring.append(child2)
            elif r < cfg.crossover_rate + cfg.mutation_rate:
                # Mutation
                parent = self._select(scored_pop)
                child = self.genetic_ops.mutate(parent)
                offspring.append(child)
            else:
                # Reproduction
                parent = self._select(scored_pop)
                offspring.append(parent.copy())
        
        return offspring[:cfg.population_size - cfg.elite_count]
    
    def evolve(
        self,
        fitness_fn: FitnessFunction,
        initial_population: list[Node] | None = None,
        callback: Callable | None = None,
        verbose: bool = True
    ) -> EvolutionResult:
        """Run the evolutionary process."""
        start_time = time.time()
        cfg = self.config
        
        # Initialize population
        if initial_population:
            population = initial_population[:cfg.population_size]
            while len(population) < cfg.population_size:
                population.append(self.tree_ops.generate_grow(cfg.init_max_depth))
        elif cfg.init_method == "ramped_half_and_half":
            population = self.tree_ops.generate_ramped_half_and_half(
                cfg.init_min_depth, cfg.init_max_depth, cfg.population_size
            )
        elif cfg.init_method == "full":
            population = [self.tree_ops.generate_full(cfg.init_max_depth) 
                         for _ in range(cfg.population_size)]
        else:
            population = [self.tree_ops.generate_grow(cfg.init_max_depth) 
                         for _ in range(cfg.population_size)]
        
        # Main evolution loop
        best_ever: tuple[Node, float] | None = None
        stagnation_count = 0
        solved = False
        
        for gen in range(cfg.max_generations):
            self.generation_count = gen + 1
            
            # Evaluate
            scored = self._evaluate_population(population, fitness_fn)
            scored.sort(key=lambda x: x[1])
            
            # Track best
            current_best = scored[0]
            if best_ever is None or current_best[1] < best_ever[1]:
                best_ever = current_best
                stagnation_count = 0
            else:
                stagnation_count += 1
            
            # Record history
            gen_stats = {
                "generation": gen + 1,
                "best_fitness": current_best[1],
                "avg_fitness": sum(f for _, f in scored) / len(scored),
                "worst_fitness": scored[-1][1],
                "avg_depth": sum(t.depth() for t, _ in scored) / len(scored),
                "avg_size": sum(t.node_count() for t, _ in scored) / len(scored),
                "best_size": current_best[0].node_count(),
                "best_depth": current_best[0].depth(),
                "evaluations": self.evaluation_count,
                "diversity": self._calculate_diversity(scored),
            }
            self.history.append(gen_stats)
            
            # Callback
            if callback:
                callback(gen + 1, current_best[0], current_best[1], scored)
            
            # Check termination
            if fitness_fn.is_solution(current_best[1]):
                solved = True
                if verbose:
                    self._print_generation(gen_stats, solved=True)
                break
            
            # Stagnation handling
            if stagnation_count >= cfg.stagnation_limit:
                if verbose:
                    print(f"\n  🔄 Stagnation detected - injecting diversity")
                # Inject random individuals
                n_inject = cfg.population_size // 4
                for i in range(n_inject):
                    new_ind = self.tree_ops.generate_grow(cfg.init_max_depth)
                    scored[i] = (new_ind, self._evaluate_individual(new_ind, fitness_fn))
                scored.sort(key=lambda x: x[1])
                stagnation_count = 0
            
            # Create next generation
            elite = [(ind.copy(), fit) for ind, fit in scored[:cfg.elite_count]]
            offspring = self._create_offspring(scored)
            population = [ind for ind, _ in elite] + offspring
            
            if verbose and (gen + 1) % 10 == 0:
                self._print_generation(gen_stats)
        
        # Final evaluation
        if best_ever is None:
            scored = self._evaluate_population(population, fitness_fn)
            scored.sort(key=lambda x: x[1])
            best_ever = scored[0]
        
        elapsed = time.time() - start_time
        
        return EvolutionResult(
            best_individual=best_ever[0],
            best_fitness=best_ever[1],
            generations=self.generation_count,
            evaluations=self.evaluation_count,
            solution_found=solved,
            history=self.history,
            final_population=scored if 'scored' in dir() else [],
            time_elapsed=elapsed
        )
    
    def _calculate_diversity(self, scored: list[tuple[Node, float]]) -> float:
        """Calculate population diversity based on structural differences."""
        if len(scored) < 2:
            return 0.0
        
        # Sample-based diversity for efficiency
        sample_size = min(20, len(scored))
        sample = random.sample(scored, sample_size)
        
        unique_sizes = set(ind.node_count() for ind, _ in sample)
        unique_depths = set(ind.depth() for ind, _ in sample)
        unique_ops = set()
        
        for ind, _ in sample:
            for node, _ in self.tree_ops.get_all_nodes(ind):
                if node.op:
                    unique_ops.add(node.op)
        
        return len(unique_sizes) + len(unique_depths) + len(unique_ops)
    
    def _print_generation(self, stats: dict, solved: bool = False):
        """Print generation statistics."""
        prefix = "  ✅" if solved else "  📊"
        print(f"{prefix} Gen {stats['generation']:>4d} | "
              f"Best: {stats['best_fitness']:.6f} | "
              f"Avg: {stats['avg_fitness']:.2f} | "
              f"Size: {stats['best_size']:>3d} | "
              f"Depth: {stats['best_depth']:>2d} | "
              f"Diversity: {stats['diversity']:.0f}")


# ═══════════════════════════════════════════════════════════════
# FACT DISCOVERY ENGINE
# ═══════════════════════════════════════════════════════════════

@dataclass
class Fact:
    a: float
    op: str
    b: float
    result: float
    discovered_at: float = field(default_factory=time.time)


class FactDiscoveryEngine:
    """Engine for discovering arithmetic facts through random search."""
    
    def __init__(self, number_range: tuple[int, int] = (1, 12)):
        self.min_num, self.max_num = number_range
        self.total_facts = self._compute_total_facts()
        self.discovered: dict[str, Fact] = {}
        self.attempts = 0
    
    def _compute_total_facts(self) -> int:
        """Compute total possible clean facts in range."""
        count = 0
        for a in range(self.min_num, self.max_num + 1):
            for b in range(self.min_num, self.max_num + 1):
                count += 3  # +, -, *
                if b != 0 and a % b == 0:
                    count += 1  # Clean division
        return count
    
    def _is_valid_fact(self, a: float, op: str, b: float) -> bool:
        """Check if this is a valid clean fact."""
        if op == "/" and (b == 0 or a % b != 0):
            return False
        return True
    
    def _compute(self, a: float, op: str, b: float) -> float:
        """Compute the result of an operation."""
        ops = {
            "+": lambda x, y: x + y,
            "-": lambda x, y: x - y,
            "*": lambda x, y: x * y,
            "/": lambda x, y: x / y,
        }
        return ops.get(op, lambda x, y: float('nan'))(a, b)
    
    def discover_batch(self, batch_size: int) -> list[Fact]:
        """Discover facts in a batch."""
        newly_found = []
        ops = ["+", "-", "*", "/"]
        
        for _ in range(batch_size):
            self.attempts += 1
            a = random.randint(self.min_num, self.max_num)
            b = random.randint(self.min_num, self.max_num)
            op = random.choice(ops)
            
            if not self._is_valid_fact(a, op, b):
                continue
            
            key = f"{a}{op}{b}"
            if key in self.discovered:
                continue
            
            result = self._compute(a, op, b)
            fact = Fact(a=a, op=op, b=b, result=result)
            self.discovered[key] = fact
            newly_found.append(fact)
        
        return newly_found
    
    def get_coverage(self) -> float:
        """Get percentage of facts discovered."""
        return (len(self.discovered) / self.total_facts) * 100 if self.total_facts > 0 else 0
    
    def get_stats(self) -> dict:
        """Get discovery statistics."""
        op_counts = {op: 0 for op in ["+", "-", "*", "/"]}
        for fact in self.discovered.values():
            op_counts[fact.op] += 1
        
        return {
            "attempts": self.attempts,
            "discovered": len(self.discovered),
            "total": self.total_facts,
            "coverage": self.get_coverage(),
            "hit_rate": (len(self.discovered) / self.attempts * 100) if self.attempts > 0 else 0,
            "by_operator": op_counts,
            "complete": len(self.discovered) >= self.total_facts,
        }
    
    def get_recent(self, n: int = 10) -> list[Fact]:
        """Get most recently discovered facts."""
        return sorted(self.discovered.values(), key=lambda f: f.discovered_at, reverse=True)[:n]


# ═══════════════════════════════════════════════════════════════
# PERSISTENCE (SQLite)
# ═══════════════════════════════════════════════════════════════

class Persistence:
    """SQLite-based persistence for facts and solved puzzles."""
    
    def __init__(self, db_path: str | Path = "evolution_lab.db"):
        self.db_path = Path(db_path)
        self._init_db()
    
    def _init_db(self):
        """Initialize database schema."""
        with sqlite3.connect(self.db_path) as conn:
            conn.execute("""
                CREATE TABLE IF NOT EXISTS facts (
                    a REAL, op TEXT, b REAL, result REAL,
                    discovered_at REAL,
                    PRIMARY KEY (a, op, b)
                )
            """)
            conn.execute("""
                CREATE TABLE IF NOT EXISTS solved_puzzles (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    target REAL,
                    numbers TEXT,
                    expression TEXT,
                    lisp_expression TEXT,
                    fitness REAL,
                    generation INTEGER,
                    tree_size INTEGER,
                    tree_depth INTEGER,
                    solved_at REAL
                )
            """)
            conn.execute("""
                CREATE TABLE IF NOT EXISTS evolution_runs (
                    id INTEGER PRIMARY KEY AUTOINCREMENT,
                    config TEXT,
                    best_fitness REAL,
                    generations INTEGER,
                    evaluations INTEGER,
                    solution_found INTEGER,
                    time_elapsed REAL,
                    started_at REAL
                )
            """)
            conn.commit()
    
    def save_fact(self, fact: Fact):
        """Save a single fact."""
        with sqlite3.connect(self.db_path) as conn:
            conn.execute(
                "INSERT OR IGNORE INTO facts VALUES (?, ?, ?, ?, ?)",
                (fact.a, fact.op, fact.b, fact.result, fact.discovered_at)
            )
            conn.commit()
    
    def save_facts(self, facts: list[Fact]):
        """Save multiple facts."""
        with sqlite3.connect(self.db_path) as conn:
            conn.executemany(
                "INSERT OR IGNORE INTO facts VALUES (?, ?, ?, ?, ?)",
                [(f.a, f.op, f.b, f.result, f.discovered_at) for f in facts]
            )
            conn.commit()
    
    def load_facts(self) -> list[Fact]:
        """Load all saved facts."""
        with sqlite3.connect(self.db_path) as conn:
            rows = conn.execute("SELECT a, op, b, result, discovered_at FROM facts").fetchall()
        return [Fact(a=r[0], op=r[1], b=r[2], result=r[3], discovered_at=r[4]) for r in rows]
    
    def save_solved_puzzle(
        self,
        target: float,
        numbers: list[float],
        expression: str,
        lisp_expression: str,
        fitness: float,
        generation: int,
        tree: Node
    ):
        """Save a solved puzzle."""
        with sqlite3.connect(self.db_path) as conn:
            conn.execute(
                """INSERT INTO solved_puzzles 
                   (target, numbers, expression, lisp_expression, fitness, generation, 
                    tree_size, tree_depth, solved_at) 
                   VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)""",
                (target, json.dumps(numbers), expression, lisp_expression, fitness,
                 generation, tree.node_count(), tree.depth(), time.time())
            )
            conn.commit()
    
    def load_solved_puzzles(self, limit: int = 50) -> list[dict]:
        """Load solved puzzles."""
        with sqlite3.connect(self.db_path) as conn:
            rows = conn.execute(
                "SELECT * FROM solved_puzzles ORDER BY solved_at DESC LIMIT ?",
                (limit,)
            ).fetchall()
        
        return [
            {
                "id": r[0], "target": r[1], "numbers": json.loads(r[2]),
                "expression": r[3], "lisp": r[4], "fitness": r[5],
                "generation": r[6], "size": r[7], "depth": r[8], "solved_at": r[9]
            }
            for r in rows
        ]
    
    def save_evolution_run(
        self,
        config: GPConfig,
        result: EvolutionResult
    ):
        """Save evolution run statistics."""
        with sqlite3.connect(self.db_path) as conn:
            conn.execute(
                """INSERT INTO evolution_runs 
                   (config, best_fitness, generations, evaluations, solution_found, time_elapsed, started_at)
                   VALUES (?, ?, ?, ?, ?, ?, ?)""",
                (json.dumps(vars(config)), result.best_fitness, result.generations,
                 result.evaluations, int(result.solution_found), result.time_elapsed, time.time())
            )
            conn.commit()
    
    def get_fact_stats(self) -> dict:
        """Get fact discovery statistics from DB."""
        with sqlite3.connect(self.db_path) as conn:
            total = conn.execute("SELECT COUNT(*) FROM facts").fetchone()[0]
            op_stats = {}
            for op in ["+", "-", "*", "/"]:
                count = conn.execute(
                    "SELECT COUNT(*) FROM facts WHERE op = ?", (op,)
                ).fetchone()[0]
                op_stats[op] = count
        return {"total": total, "by_operator": op_stats}


# ═══════════════════════════════════════════════════════════════
# VISUALIZATION
# ═══════════════════════════════════════════════════════════════

class Visualization:
    """Visualization utilities for GP results."""
    
    @staticmethod
    def plot_evolution_history(
        history: list[dict],
        save_path: str | Path = "evolution_history.png",
        show: bool = False
    ):
        """Plot evolution history with multiple metrics."""
        if not HAS_MATPLOTLIB:
            print("⚠️  matplotlib not installed - skipping plot")
            return
        
        fig, axes = plt.subplots(2, 2, figsize=(14, 10))
        fig.suptitle("Genetic Programming Evolution History", fontsize=14, fontweight='bold')
        
        generations = [h["generation"] for h in history]
        
        # Fitness over generations
        ax1 = axes[0, 0]
        ax1.semilogy(generations, [h["best_fitness"] for h in history], 
                     'b-', linewidth=1.5, label='Best')
        ax1.semilogy(generations, [h["avg_fitness"] for h in history], 
                     'b--', alpha=0.5, label='Average')
        ax1.set_xlabel("Generation")
        ax1.set_ylabel("Fitness (log scale)")
        ax1.set_title("Fitness Over Generations")
        ax1.legend()
        ax1.grid(True, alpha=0.3)
        
        # Tree size over generations
        ax2 = axes[0, 1]
        ax2.plot(generations, [h["best_size"] for h in history], 
                 'g-', linewidth=1.5, label='Best Size')
        ax2.plot(generations, [h["avg_size"] for h in history], 
                 'g--', alpha=0.5, label='Average Size')
        ax2.set_xlabel("Generation")
        ax2.set_ylabel("Node Count")
        ax2.set_title("Tree Size Over Generations")
        ax2.legend()
        ax2.grid(True, alpha=0.3)
        
        # Tree depth over generations
        ax3 = axes[1, 0]
        ax3.plot(generations, [h["best_depth"] for h in history], 
                 'r-', linewidth=1.5, label='Best Depth')
        ax3.plot(generations, [h["avg_depth"] for h in history], 
                 'r--', alpha=0.5, label='Average Depth')
        ax3.set_xlabel("Generation")
        ax3.set_ylabel("Depth")
        ax3.set_title("Tree Depth Over Generations")
        ax3.legend()
        ax3.grid(True, alpha=0.3)
        
        # Diversity over generations
        ax4 = axes[1, 1]
        ax4.plot(generations, [h["diversity"] for h in history], 
                 'm-', linewidth=1.5)
        ax4.set_xlabel("Generation")
        ax4.set_ylabel("Diversity Score")
        ax4.set_title("Population Diversity")
        ax4.grid(True, alpha=0.3)
        
        plt.tight_layout()
        plt.savefig(save_path, dpi=150, bbox_inches='tight')
        if show:
            plt.show()
        plt.close()
        print(f"📊 Evolution plot saved to {save_path}")
    
    @staticmethod
    def plot_tree(
        tree: Node,
        save_path: str | Path = "best_tree.png",
        show: bool = False
    ):
        """Visualize expression tree as a graph."""
        if not HAS_MATPLOTLIB:
            print("⚠️  matplotlib not installed - skipping tree plot")
            return
        
        fig, ax = plt.subplots(figsize=(12, 8))
        
        def get_positions(node: Node, x: float = 0, y: float = 0, 
                         width: float = 10, depth: int = 0) -> dict:
            positions = {(id(node)): (x, y)}
            if node.children:
                child_width = width / len(node.children)
                for i, child in enumerate(node.children):
                    child_x = x - width/2 + child_width * (i + 0.5)
                    child_y = y - 1.5
                    positions.update(get_positions(child, child_x, child_y, child_width, depth + 1))
            return positions
        
        def draw_edges(node: Node, positions: dict):
            for child in node.children:
                parent_pos = positions[id(node)]
                child_pos = positions[id(child)]
                ax.plot([parent_pos[0], child_pos[0]], 
                       [parent_pos[1], child_pos[1]], 
                       'k-', linewidth=1.5, alpha=0.5)
                draw_edges(child, positions)
        
        def draw_nodes(node: Node, positions: dict, tree_ops: TreeOps):
            pos = positions[id(node)]
            
            if node.type == NodeType.CONSTANT:
                color = '#E8F5E9'
                text = f"{node.value:.1f}" if node.value != int(node.value) else str(int(node.value))
            elif node.type == NodeType.VARIABLE:
                color = '#E3F2FD'
                text = node.var_name
            elif node.type == NodeType.UNARY_OP:
                color = '#FFF3E0'
                text = node.op
            else:
                color = '#FCE4EC'
                text = node.op
            
            circle = plt.Circle(pos, 0.4, color=color, ec='black', linewidth=1.5)
            ax.add_patch(circle)
            ax.text(pos[0], pos[1], text, ha='center', va='center', 
                   fontsize=10, fontweight='bold')
            
            for child in node.children:
                draw_nodes(child, positions, tree_ops)
        
        positions = get_positions(tree)
        draw_edges(tree, positions)
        draw_nodes(tree, positions, TreeOps())
        
        ax.set_xlim(-6, 6)
        ax.set_ylim(positions.values().__iter__().__next__()[1] - 2, 2)
        ax.set_aspect('equal')
        ax.axis('off')
        ax.set_title(f"Expression Tree (Size: {tree.node_count()}, Depth: {tree.depth()})", 
                     fontsize=12, fontweight='bold')
        
        plt.tight_layout()
        plt.savefig(save_path, dpi=150, bbox_inches='tight')
        if show:
            plt.show()
        plt.close()
        print(f"🌳 Tree plot saved to {save_path}")
    
    @staticmethod
    def plot_fact_discovery(
        history: list[tuple[int, int]],  # (attempts, discovered)
        total_facts: int,
        save_path: str | Path = "fact_discovery.png",
        show: bool = False
    ):
        """Plot fact discovery progress."""
        if not HAS_MATPLOTLIB:
            print("⚠️  matplotlib not installed - skipping plot")
            return
        
        fig, (ax1, ax2) = plt.subplots(1, 2, figsize=(14, 5))
        fig.suptitle("Fact Discovery Progress", fontsize=14, fontweight='bold')
        
        attempts = [h[0] for h in history]
        discovered = [h[1] for h in history]
        coverage = [d / total_facts * 100 for d in discovered]
        
        ax1.plot(attempts, discovered, 'b-', linewidth=1.5)
        ax1.axhline(y=total_facts, color='r', linestyle='--', label=f'Total ({total_facts})')
        ax1.set_xlabel("Attempts")
        ax1.set_ylabel("Facts Discovered")
        ax1.set_title("Discovery Count")
        ax1.legend()
        ax1.grid(True, alpha=0.3)
        
        ax2.plot(attempts, coverage, 'g-', linewidth=1.5)
        ax2.axhline(y=100, color='r', linestyle='--', label='100%')
        ax2.set_xlabel("Attempts")
        ax2.set_ylabel("Coverage (%)")
        ax2.set_title("Coverage Percentage")
        ax2.legend()
        ax2.grid(True, alpha=0.3)
        
        plt.tight_layout()
        plt.savefig(save_path, dpi=150, bbox_inches='tight')
        if show:
            plt.show()
        plt.close()
        print(f"📈 Discovery plot saved to {save_path}")


# ═══════════════════════════════════════════════════════════════
# RICH TERMINAL UI
# ═══════════════════════════════════════════════════════════════

class RichUI:
    """Rich terminal UI for the evolution lab."""
    
    def __init__(self):
        if not HAS_RICH:
            self.console = None
            return
        self.console = Console()
    
    def print_header(self):
        """Print application header."""
        if not self.console:
            print("\n" + "="*60)
            print("  🧪 ARITHMETIC EVOLUTION LAB")
            print("  Genetic Programming Engine for Expression Discovery")
            print("="*60 + "\n")
            return
        
        header = Text()
        header.append("🧪 ", style="bold")
        header.append("ARITHMETIC EVOLUTION LAB\n", style="bold magenta")
        header.append("Genetic Programming Engine for Expression Discovery", style="dim")
        
        panel = Panel(header, box=box.DOUBLE, border_style="cyan")
        self.console.print(panel)
    
    def print_tree(self, tree: Node, tree_ops: TreeOps, title: str = "Expression Tree"):
        """Print tree as rich tree visualization."""
        if not self.console:
            print(f"\n{title}:")
            print(f"  {tree_ops.to_string(tree)}")
            return
        
        rich_tree = Tree(f"📌 {title}")
        
        def add_node(parent, node):
            if node.type == NodeType.CONSTANT:
                label = f"[green]{node.value}[/green]"
            elif node.type == NodeType.VARIABLE:
                label = f"[blue]{node.var_name}[/blue]"
            elif node.type == NodeType.UNARY_OP:
                label = f"[yellow]{node.op}[/yellow]"
            else:
                label = f"[red bold]{node.op}[/red bold]"
            
            branch = parent.add(label)
            for child in node.children:
                add_node(branch, child)
        
        add_node(rich_tree, tree)
        self.console.print(rich_tree)
        self.console.print(f"   → {tree_ops.to_string(tree)}", style="dim")
    
    def print_stats_table(self, stats: dict, title: str = "Statistics"):
        """Print statistics as a table."""
        if not self.console:
            print(f"\n{title}:")
            for k, v in stats.items():
                print(f"  {k}: {v}")
            return
        
        table = Table(title=title, box=box.ROUNDED, show_header=False)
        table.add_column("Metric", style="cyan")
        table.add_column("Value", style="green bold")
        
        for k, v in stats.items():
            table.add_row(str(k), str(v))
        
        self.console.print(table)
    
    def print_fact(self, fact: Fact, index: int):
        """Print a single fact."""
        if not self.console:
            print(f"  #{index}: {fact.a} {fact.op} {fact.b} = {fact.result:.4f}")
            return
        
        self.console.print(
            f"  [dim]#{index}[/dim] {fact.a} [yellow]{fact.op}[/yellow] {fact.b} = [green]{fact.result:.4f}[/green]"
        )
    
    def print_progress(self, current: int, total: int, prefix: str = ""):
        """Print progress bar."""
        if not self.console:
            pct = current / total * 100 if total > 0 else 0
            print(f"\r{prefix}{pct:.1f}% ({current}/{total})", end="", flush=True)
            return
        
        # Simple inline progress
        pct = current / total * 100 if total > 0 else 0
        bar_len = 30
        filled = int(bar_len * current / total) if total > 0 else 0
        bar = "█" * filled + "░" * (bar_len - filled)
        self.console.print(f"\r{prefix}[cyan]{bar}[/cyan] {pct:.1f}%", end="")


# ═══════════════════════════════════════════════════════════════
# MAIN CLI APPLICATION
# ═══════════════════════════════════════════════════════════════

class EvolutionLab:
    """Main application class."""
    
    PRESETS = [
        {"name": "Classic 24", "numbers": [1, 2, 3, 4, 5, 6], "target": 24},
        {"name": "Countdown 952", "numbers": [25, 50, 75, 100, 3, 7], "target": 952},
        {"name": "Prime 61", "numbers": [2, 3, 5, 7, 11, 13], "target": 61},
        {"name": "Powers of 3", "numbers": [1, 3, 9, 27, 15, 21], "target": 333},
        {"name": "Big Target", "numbers": [2, 5, 7, 8, 9, 10], "target": 999},
    ]
    
    def __init__(self):
        self.persistence = Persistence()
        self.ui = RichUI()
        self.viz = Visualization()
    
    def run_evolution(
        self,
        target: float,
        numbers: list[float],
        config: GPConfig | None = None,
        save_plots: bool = True
    ) -> EvolutionResult:
        """Run evolution for a target number."""
        cfg = config or GPConfig(
            population_size=300,
            max_generations=500,
            elite_count=8,
            tournament_size=6,
            stagnation_limit=40,
        )
        
        # Create variable names for numbers
        variables = [f"x{i}" for i in range(len(numbers))]
        
        # Create engine with specific numbers as constants
        engine = GPEngine(
            config=cfg,
            constants=numbers,
            variables=variables,
            binary_ops=["+", "-", "*", "/"],
            unary_ops=[]
        )
        
        fitness_fn = TargetFitness(target, numbers)
        
        self.ui.print_header()
        print(f"\n🎯 Target: {target}")
        print(f"🔢 Numbers: {numbers}")
        print(f"⚙️  Config: Pop={cfg.population_size}, Elite={cfg.elite_count}, "
              f"MaxDepth={cfg.max_depth}, MutRate={cfg.mutation_rate}\n")
        
        result = engine.evolve(fitness_fn, verbose=True)
        
        # Print results
        print("\n" + "="*60)
        print("  RESULTS")
        print("="*60)
        
        tree_ops = engine.tree_ops
        expr_str = tree_ops.to_string(result.best_individual)
        lisp_str = tree_ops.to_lisp(result.best_individual)
        eval_result = tree_ops.evaluate(result.best_individual, 
                                        {f"x{i}": v for i, v in enumerate(numbers)})
        
        print(f"\n  Best Expression: {expr_str}")
        print(f"  Lisp Form:       {lisp_str}")
        print(f"  Evaluates to:    {eval_result}")
        print(f"  Distance:        {result.best_fitness:.10f}")
        print(f"  Tree Size:       {result.best_individual.node_count()} nodes")
        print(f"  Tree Depth:      {result.best_individual.depth()}")
        print(f"  Generations:     {result.generations}")
        print(f"  Evaluations:     {result.evaluations}")
        print(f"  Time:            {result.time_elapsed:.2f}s")
        print(f"  Solution Found:  {'✅ YES' if result.solution_found else '❌ NO'}")
        
        if result.solution_found:
            self.persistence.save_solved_puzzle(
                target=target,
                numbers=numbers,
                expression=expr_str,
                lisp_expression=lisp_str,
                fitness=result.best_fitness,
                generation=result.generations,
                tree=result.best_individual
            )
            print("  💾 Saved to puzzle log")
        
        # Visualize
        if save_plots and result.history:
            self.viz.plot_evolution_history(result.history)
            self.viz.plot_tree(result.best_individual)
        
        self.ui.print_tree(result.best_individual, tree_ops, "Best Expression Tree")
        
        self.persistence.save_evolution_run(cfg, result)
        
        return result
    
    def run_symbolic_regression(
        self,
        data: list[tuple[dict[str, float], float]],
        config: GPConfig | None = None,
        save_plots: bool = True
    ) -> EvolutionResult:
        """Run symbolic regression to fit data."""
        cfg = config or GPConfig(
            population_size=500,
            max_generations=1000,
            elite_count=10,
            tournament_size=7,
            max_depth=6,
            stagnation_limit=60,
        )
        
        # Extract variables from data
        variables = list(data[0][0].keys()) if data else ["x"]
        
        engine = GPEngine(
            config=cfg,
            constants=[-5, -2, -1, -0.5, 0, 0.5, 1, 2, 3, 5, 10],
            variables=variables,
            binary_ops=["+", "-", "*", "/"],
            unary_ops=["neg", "abs"]
        )
        
        fitness_fn = RegressionFitness(data)
        
        self.ui.print_header()
        print(f"\n📊 Symbolic Regression")
        print(f"   Data points: {len(data)}")
        print(f"   Variables: {variables}")
        print(f"   Config: Pop={cfg.population_size}, Gen={cfg.max_generations}\n")
        
        result = engine.evolve(fitness_fn, verbose=True)
        
        print("\n" + "="*60)
        tree_ops = engine.tree_ops
        expr_str = tree_ops.to_string(result.best_individual)
        
        print(f"\n  Discovered Formula: {expr_str}")
        print(f"  MSE: {result.best_fitness:.10f}")
        print(f"  Solution Found: {'✅ YES' if result.solution_found else '❌ NO'}")
        
        if save_plots and result.history:
            self.viz.plot_evolution_history(result.history, "regression_history.png")
            self.viz.plot_tree(result.best_individual, "regression_tree.png")
        
        return result
    
    def run_fact_discovery(
        self,
        number_range: tuple[int, int] = (1, 12),
        batch_size: int = 1000,
        max_attempts: int = 100_000
    ):
        """Run fact discovery mode."""
        self.ui.print_header()
        
        engine = FactDiscoveryEngine(number_range)
        
        # Load existing facts
        existing = self.persistence.load_facts()
        for fact in existing:
            key = f"{fact.a}{fact.op}{fact.b}"
            engine.discovered[key] = fact
        
        print(f"\n📚 Fact Discovery Mode")
        print(f"   Range: {number_range[0]}-{number_range[1]}")
        print(f"   Total possible facts: {engine.total_facts}")
        print(f"   Already known: {len(engine.discovered)}")
        print(f"   Batch size: {batch_size}\n")
        
        history = []
        save_interval = 10000
        
        while engine.attempts < max_attempts and not engine.get_stats()["complete"]:
            newly_found = engine.discover_batch(batch_size)
            
            if newly_found:
                self.persistence.save_facts(newly_found)
            
            history.append((engine.attempts, len(engine.discovered)))
            
            # Progress update
            stats = engine.get_stats()
            if engine.attempts % (batch_size * 10) == 0:
                print(f"\r  📊 Attempts: {stats['attempts']:>8,} | "
                      f"Discovered: {stats['discovered']:>4}/{stats['total']} | "
                      f"Coverage: {stats['coverage']:.1f}% | "
                      f"Hit Rate: {stats['hit_rate']:.2f}%", end="", flush=True)
        
        print("\n\n" + "="*60)
        stats = engine.get_stats()
        print("  FACT DISCOVERY COMPLETE" if stats["complete"] else "  FACT DISCOVERY PAUSED")
        print("="*60)
        print(f"\n  Total attempts: {stats['attempts']:,}")
        print(f"  Facts discovered: {stats['discovered']}/{stats['total']}")
        print(f"  Coverage: {stats['coverage']:.1f}%")
        print(f"  By operator: {stats['by_operator']}")
        
        # Recent discoveries
        print("\n  Most Recent Discoveries:")
        for i, fact in enumerate(engine.get_recent(10), 1):
            self.ui.print_fact(fact, i)
        
        # Plot
        if history:
            self.viz.plot_fact_discovery(history, engine.total_facts)
    
    def show_solved_puzzles(self):
        """Show solved puzzle log."""
        puzzles = self.persistence.load_solved_puzzles()
        
        self.ui.print_header()
        print(f"\n🏆 SOLVED PUZZLE LOG ({len(puzzles)} puzzles)\n")
        
        if not puzzles:
            print("  No puzzles solved yet. Run evolution to find solutions!")
            return
        
        for p in puzzles:
            print(f"  🎯 Target {p['target']} ← {p['expression']}")
            print(f"     Numbers: [{', '.join(map(str, p['numbers']))}]")
            print(f"     Solved in generation {p['generation']} (size: {p['size']}, depth: {p['depth']})")
            print()
    
    def benchmark(self, runs: int = 5):
        """Run benchmark tests."""
        self.ui.print_header()
        print(f"\n⚡ BENCHMARK MODE ({runs} runs per preset)\n")
        
        results = []
        
        for preset in self.PRESLES[:3]:  # First 3 presets
            print(f"\n{'='*40}")
            print(f"  {preset['name']}")
            print(f"{'='*40}")
            
            times = []
            generations = []
            solved = 0
            
            for i in range(runs):
                cfg = GPConfig(
                    population_size=200,
                    max_generations=300,
                    elite_count=5,
                )
                
                engine = GPEngine(
                    config=cfg,
                    constants=preset['numbers'],
                    binary_ops=["+", "-", "*", "/"]
                )
                
                fitness_fn = TargetFitness(preset['target'], preset['numbers'])
                result = engine.evolve(fitness_fn, verbose=False)
                
                times.append(result.time_elapsed)
                generations.append(result.generations)
                if result.solution_found:
                    solved += 1
                
                print(f"    Run {i+1}: {result.time_elapsed:.2f}s, "
                      f"{result.generations} gens, "
                      f"{'✅' if result.solution_found else '❌'}")
            
            avg_time = sum(times) / len(times)
            avg_gens = sum(generations) / len(generations)
            success_rate = solved / runs * 100
            
            results.append({
                "name": preset['name'],
                "avg_time": avg_time,
                "avg_gens": avg_gens,
                "success_rate": success_rate
            })
            
            print(f"\n  Summary: {avg_time:.2f}s avg, {avg_gens:.0f} gens avg, "
                  f"{success_rate:.0f}% success")
        
        print("\n" + "="*60)
        print("  BENCHMARK SUMMARY")
        print("="*60)
        self.ui.print_stats_table(
            {r["name"]: f"{r['avg_time']:.2f}s / {r['success_rate']:.0f}%" 
             for r in results},
            "Results (avg time / success rate)"
        )


# ═══════════════════════════════════════════════════════════════
# CLI ENTRY POINT
# ═══════════════════════════════════════════════════════════════

def main():
    """Main entry point."""
    import argparse
    
    parser = argparse.ArgumentParser(
        description="Arithmetic Evolution Lab - Genetic Programming Engine",
        formatter_class=argparse.RawDescriptionHelpFormatter,
        epilog="""
Examples:
  python evolution_lab.py evolve --target 24 --numbers 1 2 3 4 5 6
  python evolution_lab.py evolve --preset countdown
  python evolution_lab.py discover --range 1 20
  python evolution_lab.py regression --formula "x**2 + 2*x + 1"
  python evolution_lab.py log
  python evolution_lab.py benchmark
  python evolution_lab.py interactive
        """
    )
    
    subparsers = parser.add_subparsers(dest="command", help="Command to run")
    
    # Evolve command
    evolve_parser = subparsers.add_parser("evolve", help="Evolve expression for target")
    evolve_parser.add_argument("--target", type=float, required=True, help="Target number")
    evolve_parser.add_argument("--numbers", type=float, nargs="+", required=True, 
                               help="Available numbers")
    evolve_parser.add_argument("--population", type=int, default=300, help="Population size")
    evolve_parser.add_argument("--generations", type=int, default=500, help="Max generations")
    evolve_parser.add_argument("--no-plots", action="store_true", help="Skip plot generation")
    
    # Discover command
    discover_parser = subparsers.add_parser("discover", help="Run fact discovery")
    discover_parser.add_argument("--range", type=int, nargs=2, default=[1, 12],
                                 metavar=("MIN", "MAX"), help="Number range")
    discover_parser.add_argument("--max-attempts", type=int, default=100000,
                                 help="Maximum attempts")
    discover_parser.add_argument("--batch-size", type=int, default=1000,
                                 help="Batch size per tick")
    
    # Regression command
    reg_parser = subparsers.add_parser("regression", help="Symbolic regression")
    reg_parser.add_argument("--formula", type=str, required=True,
                           help="Formula to discover (e.g., 'x**2 + 2*x + 1')")
    reg_parser.add_argument("--points", type=int, default=50, help="Number of data points")
    reg_parser.add_argument("--x-range", type=float, nargs=2, default=[-10, 10],
                           metavar=("MIN", "MAX"), help="X value range")
    
    # Log command
    subparsers.add_parser("log", help="Show solved puzzle log")
    
    # Benchmark command
    bench_parser = subparsers.add_parser("benchmark", help="Run benchmarks")
    bench_parser.add_argument("--runs", type=int, default=5, help="Runs per preset")
    
    # Interactive command
    subparsers.add_parser("interactive", help="Run interactive mode")
    
    # Reset command
    subparsers.add_parser("reset", help="Reset database")
    
    args = parser.parse_args()
    
    if not args.command:
        parser.print_help()
        return
    
    lab = EvolutionLab()
    
    if args.command == "evolve":
        config = GPConfig(
            population_size=args.population,
            max_generations=args.generations,
        )
        lab.run_evolution(args.target, args.numbers, config, save_plots=not args.no_plots)
    
    elif args.command == "discover":
        lab.run_fact_discovery(tuple(args.range), args.batch_size, args.max_attempts)
    
    elif args.command == "regression":
        # Generate data from formula
        x_min, x_max = args.x_range
        data = []
        for i in range(args.points):
            x = x_min + (x_max - x_min) * i / (args.points - 1)
            y = eval(args.formula, {"x": x, "__builtins__": {}})
            data.append(({"x": x}, y))
        lab.run_symbolic_regression(data)
    
    elif args.command == "log":
        lab.show_solved_puzzles()
    
    elif args.command == "benchmark":
        lab.benchmark(args.runs)
    
    elif args.command == "interactive":
        run_interactive(lab)
    
    elif args.command == "reset":
        os.remove("evolution_lab.db") if os.path.exists("evolution_lab.db") else None
        print("🗑️  Database reset complete")


def run_interactive(lab: EvolutionLab):
    """Run interactive mode."""
    lab.ui.print_header()
    
    print("\n  Available presets:")
    for i, p in enumerate(lab.PRESETS, 1):
        print(f"    {i}. {p['name']}: target={p['target']}, numbers={p['numbers']}")
    
    print("\n  Commands: 'preset N', 'custom TARGET NUMBERS', 'discover', 'log', 'quit'")
    
    while True:
        try:
            choice = input("\n  > ").strip().lower()
        except (EOFError, KeyboardInterrupt):
            print("\n  👋 Goodbye!")
            break
        
        if not choice or choice == "quit" or choice == "q":
            print("  👋 Goodbye!")
            break
        
        elif choice == "discover":
            lab.run_fact_discovery()
        
        elif choice == "log":
            lab.show_solved_puzzles()
        
        elif choice.startswith("preset"):
            parts = choice.split()
            if len(parts) < 2:
                print("  Usage: preset N")
                continue
            try:
                idx = int(parts[1]) - 1
                preset = lab.PRESETS[idx]
                lab.run_evolution(preset["target"], preset["numbers"])
            except (ValueError, IndexError):
                print("  Invalid preset number")
        
        elif choice.startswith("custom"):
            parts = choice.split()
            if len(parts) < 3:
                print("  Usage: custom TARGET NUM1 NUM2 ...")
                continue
            try:
                target = float(parts[1])
                numbers = [float(x) for x in parts[2:]]
                lab.run_evolution(target, numbers)
            except ValueError:
                print("  Invalid numbers")
        
        else:
            print("  Unknown command. Try 'preset N', 'discover', 'log', or 'quit'")


if __name__ == "__main__":
    main()
    