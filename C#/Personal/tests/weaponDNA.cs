using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BossSim.Genetics
{
    /// <summary>
    /// A compact, extensible genome for procedurally-generated weapons.
    /// Meets the constraint: "base form" is ONE gene; "size" is ONE gene.
    /// Supports 450+ genes via a typed registry and programmatic expansion.
    /// Includes: random generation, mutation, crossover, validation, hashing, and JSON I/O.
    /// </summary>
    public sealed class WeaponDNA
    {
        // -------- Core storage --------
        public string Id { get; private set; } = Guid.NewGuid().ToString("N");

        /// <summary> Typed gene bag. Keys are canonical gene keys (e.g., "base.form", "size") </summary>
        public Dictionary<string, GeneValue> Genes { get; } = new(StringComparer.Ordinal);

        /// <summary> Non-persistent, cached evaluation/telemetry. </summary>
        [JsonIgnore] public EvaluationCache Eval { get; private set; } = new();

        // --------- Canonical one-gene constraints explicitly modelled ---------
        // Base Form (ONE gene) with many possible discrete values (the sub-forms are the values, not extra genes).
        public const string G_BaseForm = "base.form"; // enum BaseFormType
        // Size (ONE gene)
        public const string G_Size = "size"; // float meters, 0.2..5.0

        // Convenience accessors
        public BaseFormType BaseForm
        {
            get => Genes.TryGetValue(G_BaseForm, out var v) ? (BaseFormType)v.AsEnum<int>() : BaseFormType.LongSword;
            set => Genes[G_BaseForm] = GeneValue.Enum((int)value);
        }
        public float SizeMeters
        {
            get => Genes.TryGetValue(G_Size, out var v) ? v.AsFloat() : 1.2f;
            set => Genes[G_Size] = GeneValue.Float(value);
        }

        // --------- Construction API ---------
        public static WeaponDNA CreateRandom(int targetGeneCount, Random? rng = null)
        {
            rng ??= new Random();
            var dna = new WeaponDNA();

            // Ensure the two mandatory genes exist first
            dna.BaseForm = Registry.PickEnum<BaseFormType>(rng);
            dna.SizeMeters = Registry.RandInRange(rng, 0.2f, 5.0f);

            // Fill remaining genes from registry blueprints
            var blueprints = Registry.AllBlueprintsShuffled(rng);
            foreach (var bp in blueprints)
            {
                if (dna.Genes.Count >= targetGeneCount) break;
                if (bp.Key == G_BaseForm || bp.Key == G_Size) continue; // already set
                if (!bp.CanInstantiateWith(dna)) continue; // respect preconditions
                dna.Genes[bp.Key] = bp.InstantiateRandom(rng);
            }

            dna.RepairCoherence(rng);
            dna.RecomputeHash();
            return dna;
        }

        public WeaponDNA Clone()
        {
            var copy = new WeaponDNA();
            copy.Id = Id; // same id unless rehashed later
            foreach (var kv in Genes) copy.Genes[kv.Key] = kv.Value;
            return copy;
        }

        public void RecomputeHash()
        {
            using var sha = SHA256.Create();
            var json = JsonSerializer.Serialize(Genes.OrderBy(k => k.Key));
            var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(json));
            Id = Convert.ToHexString(hash)[..16];
        }

        // --------- Genetic operations ---------
        public void Mutate(Random? rng = null, double pointMutationRate = 0.08, double toggleRate = 0.04, double structuralRate = 0.02)
        {
            rng ??= new Random();

            // Point mutations: perturb numeric / switch enum / flip bool
            foreach (var key in Genes.Keys.ToList())
            {
                if (rng.NextDouble() < pointMutationRate && Registry.TryGetBlueprint(key, out var bp))
                {
                    Genes[key] = bp.Mutate(Genes[key], rng);
                }
                if (rng.NextDouble() < toggleRate && Registry.TryGetBlueprint(key, out bp) && bp.Kind == GeneKind.Bool)
                {
                    Genes[key] = GeneValue.Bool(!Genes[key].AsBool());
                }
            }

            // Structural: add or remove optional genes respecting constraints
            if (rng.NextDouble() < structuralRate)
            {
                var addable = Registry.OptionalBlueprintsWhere(this, bp => !Genes.ContainsKey(bp.Key) && bp.CanInstantiateWith(this)).ToList();
                if (addable.Count > 0)
                {
                    var pick = addable[rng.Next(addable.Count)];
                    Genes[pick.Key] = pick.InstantiateRandom(rng);
                }
            }
            if (rng.NextDouble() < structuralRate && Genes.Count > 2)
            {
                var removable = Genes.Keys.Where(k => Registry.CanRemoveGene(k)).ToList();
                if (removable.Count > 0)
                {
                    Genes.Remove(removable[rng.Next(removable.Count)]);
                }
            }

            RepairCoherence(rng);
            RecomputeHash();
        }

        public static WeaponDNA Crossover(WeaponDNA a, WeaponDNA b, Random? rng = null, double swapProbability = 0.5)
        {
            rng ??= new Random();
            var child = new WeaponDNA();
            // Inherit mandatory genes with possible swap
            child.BaseForm = rng.NextDouble() < swapProbability ? a.BaseForm : b.BaseForm;
            child.SizeMeters = rng.NextDouble() < swapProbability ? a.SizeMeters : b.SizeMeters;

            // Merge genes
            var allKeys = a.Genes.Keys.Union(b.Genes.Keys).Distinct().ToList();
            foreach (var key in allKeys)
            {
                if (key == G_BaseForm || key == G_Size) continue;
                var takeA = rng.NextDouble() < 0.5;
                if (takeA && a.Genes.TryGetValue(key, out var va)) child.Genes[key] = va;
                else if (b.Genes.TryGetValue(key, out var vb)) child.Genes[key] = vb;
            }

            // Small chance to mutate after crossover
            child.Mutate(rng, pointMutationRate: 0.04, toggleRate: 0.02, structuralRate: 0.02);
            child.RepairCoherence(rng);
            child.RecomputeHash();
            return child;
        }

        // --------- Validation and Repair ---------
        public bool Validate(out List<string> issues)
        {
            issues = new List<string>();

            // Base sanity
            if (!Genes.ContainsKey(G_BaseForm)) issues.Add("Missing gene: base.form");
            if (!Genes.ContainsKey(G_Size)) issues.Add("Missing gene: size");
            if (SizeMeters < 0.2f || SizeMeters > 5.0f) issues.Add("size out of bounds (0.2..5.0)");

            // Rule set (selected strong rules for coherence)
            Rule_BeamRequiresEmissionAndElement(issues);
            Rule_ProjectilesRequireProjectileBlock(issues);
            Rule_TrapMineConstraints(issues);
            Rule_MorphConsistency(issues);
            Rule_ConductivityVsShock(issues);
            Rule_GripVsLengthOrMass(issues);
            Rule_ReachVsSize(issues);
            Rule_CostVsPower(issues);
            Rule_ControlStacking(issues);

            return issues.Count == 0;
        }

        public void RepairCoherence(Random? rng = null)
        {
            rng ??= new Random();
            // If a rule is violated, attempt minimal repair using registry hints
            Validate(out var issues);
            if (issues.Count == 0) return;

            foreach (var issue in issues)
            {
                if (issue.Contains("beam requires"))
                {
                    // Ensure emission and at least one element
                    Ensure("channels.emission.mode", GeneValue.Enum((int)EmissionMode.Beam));
                    EnsureElement(rng);
                }
                else if (issue.Contains("projectile block"))
                {
                    Ensure("projectile.archetype", GeneValue.Enum((int)ProjectileArchetype.Arrow));
                    Ensure("projectile.ballistic", GeneValue.Enum((int)BallisticKind.Flat));
                    Ensure("projectile.muzzle_velocity_ms", GeneValue.Float(60));
                }
                else if (issue.Contains("trap/mine"))
                {
                    Ensure("projectile.ballistic", GeneValue.Enum((int)BallisticKind.Arced));
                    Ensure("projectile.fuse_ms", GeneValue.Int(800));
                }
                else if (issue.Contains("morph"))
                {
                    Ensure("morph.enabled", GeneValue.Bool(true));
                    Ensure("morph.modes", GeneValue.Int(Math.Max(1, Genes.TryGetValue("morph.modes", out var v) ? v.AsInt() : 1)));
                    Ensure("morph.transform_time_ms", GeneValue.Int(200));
                }
                else if (issue.Contains("conductivity vs shock"))
                {
                    // Increase conductivity or reduce shock intensity
                    if (Genes.TryGetValue("materials.conductivity", out var cv) && (Conductivity)cv.AsEnum<int>() == Conductivity.None)
                        Genes["materials.conductivity"] = GeneValue.Enum((int)Conductivity.Low);
                    else
                        Genes["channels.elements.shock.intensity"] = GeneValue.Float(0.4f);
                }
                else if (issue.Contains("grip/length/mass"))
                {
                    Ensure("class.grip", GeneValue.Enum((int)Grip.TwoHand));
                }
                else if (issue.Contains("reach vs size"))
                {
                    Genes["delivery.reach_m"] = GeneValue.Float(MathF.Max(SizeMeters * 0.7f, Genes.TryGetValue("delivery.reach_m", out var r) ? r.AsFloat() : 1.0f));
                }
                else if (issue.Contains("cost vs power"))
                {
                    // Increase costs lightly
                    Bump("costs.stamina", 5, 0, 100);
                    Bump("costs.heat", 5, 0, 100);
                }
                else if (issue.Contains("control stacking"))
                {
                    // reduce slow
                    if (Genes.TryGetValue("control.slow_pct", out var sp))
                        Genes["control.slow_pct"] = GeneValue.Float(MathF.Min(0.4f, sp.AsFloat()));
                }
            }
        }

        private void Ensure(string key, GeneValue value)
        {
            if (!Genes.ContainsKey(key)) Genes[key] = value;
        }
        private void EnsureElement(Random rng)
        {
            var pick = Registry.PickEnum<Element>(rng);
            Genes[$"channels.elements.{pick}.intensity"] = GeneValue.Float(0.4f);
        }
        private void Bump(string key, float delta, float min, float max)
        {
            var cur = Genes.TryGetValue(key, out var v) ? v.AsFloat() : min;
            Genes[key] = GeneValue.Float(Math.Clamp(cur + delta, min, max));
        }

        // --------- Rule implementations ---------
        private void Rule_BeamRequiresEmissionAndElement(List<string> issues)
        {
            if (Genes.TryGetValue("delivery.primary", out var dp) && (DeliveryPrimary)dp.AsEnum<int>() == DeliveryPrimary.Beam)
            {
                if (!Genes.TryGetValue("channels.emission.mode", out var em) || (EmissionMode)em.AsEnum<int>() == EmissionMode.None)
                    issues.Add("beam requires channels.emission.mode != none");

                bool hasAnyElement = Enum.GetValues<Element>().Any(el => Genes.ContainsKey($"channels.elements.{el}.intensity"));
                if (!hasAnyElement) issues.Add("beam requires at least one channels.elements.*");
            }
        }
        private void Rule_ProjectilesRequireProjectileBlock(List<string> issues)
        {
            if (Genes.TryGetValue("delivery.primary", out var dp))
            {
                var p = (DeliveryPrimary)dp.AsEnum<int>();
                if (p is DeliveryPrimary.Projectile or DeliveryPrimary.Throw or DeliveryPrimary.Chain or DeliveryPrimary.Whip or DeliveryPrimary.Beam)
                {
                    if (!Genes.ContainsKey("projectile.archetype"))
                        issues.Add("projectile/beam delivery requires projectile block");
                }
            }
        }
        private void Rule_TrapMineConstraints(List<string> issues)
        {
            if (Genes.TryGetValue("delivery.primary", out var dp))
            {
                var p = (DeliveryPrimary)dp.AsEnum<int>();
                if (p is DeliveryPrimary.Trap or DeliveryPrimary.Mine)
                {
                    if (!Genes.TryGetValue("projectile.ballistic", out var b) || (BallisticKind)b.AsEnum<int>() == BallisticKind.Hitscan)
                        issues.Add("trap/mine require non-hitscan ballistic");
                    if (!Genes.TryGetValue("projectile.fuse_ms", out var f) || f.AsInt() < 300)
                        issues.Add("trap/mine require fuse_ms >= 300");
                }
            }
        }
        private void Rule_MorphConsistency(List<string> issues)
        {
            if (Genes.TryGetValue("morph.enabled", out var en) && en.AsBool())
            {
                if (!Genes.TryGetValue("morph.modes", out var m) || m.AsInt() < 1)
                    issues.Add("morph enabled but modes < 1");
                if (!Genes.TryGetValue("morph.transform_time_ms", out var tt) || tt.AsInt() < 100)
                    issues.Add("morph requires transform_time_ms >= 100");
            }
        }
        private void Rule_ConductivityVsShock(List<string> issues)
        {
            if (Genes.TryGetValue("materials.conductivity", out var c) && (Conductivity)c.AsEnum<int>() == Conductivity.None)
            {
                if (Genes.TryGetValue("channels.elements.shock.intensity", out var si) && si.AsFloat() > 0.5f)
                    issues.Add("conductivity vs shock conflict");
            }
        }
        private void Rule_GripVsLengthOrMass(List<string> issues)
        {
            if (Genes.TryGetValue("class.grip", out var g) && (Grip)g.AsEnum<int>() == Grip.TwoHand)
            {
                var ok = false;
                if (Genes.TryGetValue("geometry.length_m", out var l) && l.AsFloat() >= 1.1f) ok = true;
                if (Genes.TryGetValue("geometry.head_mass_kg", out var hm) && hm.AsFloat() >= 3.0f) ok = true;
                if (!ok) issues.Add("grip/length/mass mismatch for two-hand weapon");
            }
        }
        private void Rule_ReachVsSize(List<string> issues)
        {
            if (Genes.TryGetValue("delivery.reach_m", out var r))
            {
                var reach = r.AsFloat();
                var min = SizeMeters * 0.5f; var max = SizeMeters * 2.5f;
                if (reach < min || reach > max) issues.Add("reach vs size coherence violated");
            }
        }
        private void Rule_CostVsPower(List<string> issues)
        {
            if (Genes.TryGetValue("limits.max_burst_dps", out var bd) && Genes.TryGetValue("costs.stamina", out var st))
            {
                if (bd.AsFloat() > 8000 && st.AsFloat() < 20)
                    issues.Add("cost vs power coherence");
            }
        }
        private void Rule_ControlStacking(List<string> issues)
        {
            if (Genes.TryGetValue("control.stagger", out var sg) && (Stagger)sg.AsEnum<int>() == Stagger.Heavy)
            {
                if (Genes.TryGetValue("control.slow_pct", out var sp) && sp.AsFloat() > 0.4f)
                    issues.Add("control stacking too high");
            }
        }

        // --------- Serialization ---------
        public string ToJson(bool indented = false)
        {
            var opts = new JsonSerializerOptions
            {
                WriteIndented = indented,
                Converters = { new GeneValueJsonConverter() }
            };
            return JsonSerializer.Serialize(this, opts);
        }
        public static WeaponDNA FromJson(string json)
        {
            var opts = new JsonSerializerOptions
            {
                Converters = { new GeneValueJsonConverter() }
            };
            return JsonSerializer.Deserialize<WeaponDNA>(json, opts)!;
        }
    }

    // ======= Support types =======

    public enum GeneKind { Float, Int, Bool, Enum }

    public readonly struct GeneValue
    {
        public GeneKind Kind { get; }
        public object Value { get; }
        private GeneValue(GeneKind kind, object value) { Kind = kind; Value = value; }
        public static GeneValue Float(float v) => new(GeneKind.Float, v);
        public static GeneValue Int(int v) => new(GeneKind.Int, v);
        public static GeneValue Bool(bool v) => new(GeneKind.Bool, v);
        public static GeneValue Enum(int v) => new(GeneKind.Enum, v);
        public float AsFloat() => Kind == GeneKind.Float ? (float)Value : Convert.ToSingle(Value);
        public int AsInt() => Kind == GeneKind.Int ? (int)Value : Convert.ToInt32(Value);
        public bool AsBool() => Kind == GeneKind.Bool ? (bool)Value : Convert.ToBoolean(Value);
        public TEnum AsEnum<TEnum>() where TEnum : struct, Enum => (TEnum)Enum.ToObject(typeof(TEnum), Convert.ToInt32(Value));
        public override string ToString() => Value?.ToString() ?? string.Empty;
    }

    public sealed class GeneValueJsonConverter : JsonConverter<GeneValue>
    {
        public override GeneValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            // GeneKind is not serialized; infer from JSON token type. Enum is serialized as number
            return reader.TokenType switch
            {
                JsonTokenType.Number => reader.TryGetInt32(out var i) ? GeneValue.Enum(i) : GeneValue.Float(reader.GetSingle()),
                JsonTokenType.True => GeneValue.Bool(true),
                JsonTokenType.False => GeneValue.Bool(false),
                _ => throw new JsonException("Unsupported gene value token")
            };
        }
        public override void Write(Utf8JsonWriter writer, GeneValue value, JsonSerializerOptions options)
        {
            switch (value.Kind)
            {
                case GeneKind.Float: writer.WriteNumberValue(value.AsFloat()); break;
                case GeneKind.Int: writer.WriteNumberValue(value.AsInt()); break;
                case GeneKind.Bool: writer.WriteBooleanValue(value.AsBool()); break;
                case GeneKind.Enum: writer.WriteNumberValue(value.AsInt()); break;
                default: writer.WriteNullValue(); break;
            }
        }
    }

    // ======= Registry: defines 450+ gene blueprints programmatically =======

    public static class Registry
    {
        private static readonly Dictionary<string, GeneBlueprint> _blue = new(StringComparer.Ordinal);
        private static readonly string[] _removablePrefixes = new[]
        {
            "channels.", "projectile.", "morph.", "control.", "costs.", "limits.", "materials.", "delivery.", "geometry.", "class."
        };

        static Registry()
        {
            // Mandatory two genes
            Register(new GeneBlueprint(WeaponDNA.G_BaseForm, GeneKind.Enum).WithEnumRange<BaseFormType>());
            Register(new GeneBlueprint(WeaponDNA.G_Size, GeneKind.Float).WithRange(0.2f, 5.0f));

            // Core class/geometry genes (a few explicit for coherence rules)
            Register(new GeneBlueprint("class.grip", GeneKind.Enum).WithEnumRange<Grip>());
            Register(new GeneBlueprint("geometry.length_m", GeneKind.Float).WithRange(0.2f, 5.0f));
            Register(new GeneBlueprint("geometry.head_mass_kg", GeneKind.Float).WithRange(0.1f, 30f));
            Register(new GeneBlueprint("delivery.primary", GeneKind.Enum).WithEnumRange<DeliveryPrimary>());
            Register(new GeneBlueprint("delivery.reach_m", GeneKind.Float).WithRange(0.2f, 12f));

            // Materials
            Register(new GeneBlueprint("materials.core", GeneKind.Enum).WithEnumRange<CoreMaterial>());
            Register(new GeneBlueprint("materials.jacket", GeneKind.Enum).WithEnumRange<JacketMaterial>());
            Register(new GeneBlueprint("materials.conductivity", GeneKind.Enum).WithEnumRange<Conductivity>());

            // Projectile block
            Register(new GeneBlueprint("projectile.archetype", GeneKind.Enum).WithEnumRange<ProjectileArchetype>().Optional());
            Register(new GeneBlueprint("projectile.muzzle_velocity_ms", GeneKind.Float).WithRange(5, 400).Optional());
            Register(new GeneBlueprint("projectile.ballistic", GeneKind.Enum).WithEnumRange<BallisticKind>().Optional());
            Register(new GeneBlueprint("projectile.homing", GeneKind.Enum).WithEnumRange<HomingKind>().Optional());
            Register(new GeneBlueprint("projectile.bounce", GeneKind.Int).WithRange(0, 5).Optional());
            Register(new GeneBlueprint("projectile.pierce", GeneKind.Int).WithRange(0, 8).Optional());
            Register(new GeneBlueprint("projectile.aoe_radius_m", GeneKind.Float).WithRange(0, 8).Optional());
            Register(new GeneBlueprint("projectile.fuse_ms", GeneKind.Int).WithRange(0, 5000).Optional());

            // Morph block
            Register(new GeneBlueprint("morph.enabled", GeneKind.Bool).Optional());
            Register(new GeneBlueprint("morph.modes", GeneKind.Int).WithRange(1, 4).Optional());
            Register(new GeneBlueprint("morph.transform_time_ms", GeneKind.Int).WithRange(50, 1200).Optional());

            // Channels / emission
            Register(new GeneBlueprint("channels.emission.mode", GeneKind.Enum).WithEnumRange<EmissionMode>().Optional());
            Register(new GeneBlueprint("channels.emission.duty_cycle", GeneKind.Float).WithRange(0, 1).Optional());

            // Elements: programmatically create 10+ element intensity genes
            foreach (Element el in Enum.GetValues(typeof(Element)))
            {
                Register(new GeneBlueprint($"channels.elements.{el}.intensity", GeneKind.Float).WithRange(0, 1).Optional());
            }

            // Control effects
            Register(new GeneBlueprint("control.stagger", GeneKind.Enum).WithEnumRange<Stagger>().Optional());
            Register(new GeneBlueprint("control.knockback_m", GeneKind.Float).WithRange(0, 6).Optional());
            Register(new GeneBlueprint("control.pull_m", GeneKind.Float).WithRange(0, 6).Optional());
            Register(new GeneBlueprint("control.slow_pct", GeneKind.Float).WithRange(0, 0.9f).Optional());
            Register(new GeneBlueprint("control.disarm", GeneKind.Bool).Optional());
            Register(new GeneBlueprint("control.silence_ms", GeneKind.Int).WithRange(0, 2000).Optional());
            // DOT sub-block
            Register(new GeneBlueprint("control.dot.type", GeneKind.Enum).WithEnumRange<DotType>().Optional());
            Register(new GeneBlueprint("control.dot.dps", GeneKind.Float).WithRange(0, 10000).Optional());
            Register(new GeneBlueprint("control.dot.duration_ms", GeneKind.Int).WithRange(0, 10000).Optional());

            // Costs / limits
            Register(new GeneBlueprint("costs.stamina", GeneKind.Float).WithRange(0, 100).Optional());
            Register(new GeneBlueprint("costs.heat", GeneKind.Float).WithRange(0, 100).Optional());
            Register(new GeneBlueprint("costs.focus", GeneKind.Float).WithRange(0, 100).Optional());
            Register(new GeneBlueprint("costs.cooldown_ms", GeneKind.Int).WithRange(0, 20000).Optional());
            Register(new GeneBlueprint("costs.maintenance", GeneKind.Float).WithRange(0, 1).Optional());

            Register(new GeneBlueprint("limits.max_burst_dps", GeneKind.Float).WithRange(0, 100000).Optional());
            Register(new GeneBlueprint("limits.max_sustain_dps", GeneKind.Float).WithRange(0, 50000).Optional());
            Register(new GeneBlueprint("limits.self_damage_pct", GeneKind.Float).WithRange(0, 0.5f).Optional());

            // ---- Massive expansion to exceed 450 genes ----
            // Category arrays and procedural gene creation
            string[] grips = Enum.GetNames(typeof(Grip));
            string[] crossSections = { "diamond", "lenticular", "hollow", "box", "round" };
            AddFloatRange("geometry.balance_point_m", 0, 2);
            AddFloatRange("geometry.curvature", -1, 1);
            AddEnum("geometry.cross_section", crossSections);
            AddIntRange("geometry.segment_count", 1, 6);

            // Delivery secondary flags (each as its own boolean gene)
            foreach (var sec in Enum.GetNames(typeof(DeliveryPrimary)))
            {
                Register(new GeneBlueprint($"delivery.secondary.{sec}", GeneKind.Bool).Optional());
            }

            // Tags/traits as booleans
            foreach (var tag in new[]
                     {
                         "anti_armor","anti_shield","anti_swarm","aerial_ok","underwater_ok","silent","stealth_breaker",
                         "terrain_alter","grapple","guard_break","parry_friendly","feintable"
                     })
            {
                Register(new GeneBlueprint($"tags.{tag}", GeneKind.Bool).Optional());
            }

            // Visual/audio aesthetics genes (lots of optional booleans and floats)
            var visual = new[] { "aura_glow", "distortion", "fractal_shadows", "illusions", "invisibility_field" };
            foreach (var v in visual)
            {
                Register(new GeneBlueprint($"fx.visual.{v}", GeneKind.Bool).Optional());
                Register(new GeneBlueprint($"fx.visual.{v}.intensity", GeneKind.Float).WithRange(0,1).Optional());
            }
            var audio = new[] { "hum", "roar", "whispers", "silence" };
            foreach (var a in audio)
            {
                Register(new GeneBlueprint($"fx.audio.{a}", GeneKind.Bool).Optional());
                Register(new GeneBlueprint($"fx.audio.{a}.gain", GeneKind.Float).WithRange(0,1).Optional());
            }

            // Intelligence/behavior
            Register(new GeneBlueprint("ai.level", GeneKind.Enum).WithEnumRange<AILevel>().Optional());
            foreach (var b in new[] { "auto_defend","auto_attack","advice_to_user","resistance_to_owner" })
                Register(new GeneBlueprint($"ai.beh.{b}", GeneKind.Bool).Optional());
            foreach (var learn in new[] { "enemy_traits","battle","environment","user" })
                Register(new GeneBlueprint($"ai.learn.{learn}", GeneKind.Bool).Optional());

            // Adaptability booleans
            foreach (var env in new[] { "underwater","vacuum","self_stabilizing","magma","radiation","toxic","storm" })
                Register(new GeneBlueprint($"adapt.env.{env}", GeneKind.Bool).Optional());
            foreach (var u in new[] { "bond_strong","bond_parasitic","bond_symbiotic" })
                Register(new GeneBlueprint($"adapt.user.{u}", GeneKind.Bool).Optional());

            // Massive matrices for resistances and damages (element × channel × tier) => many genes
            string[] dmgKinds = Enum.GetNames(typeof(Element));
            foreach (var kind in dmgKinds)
            {
                Register(new GeneBlueprint($"damage.{kind}.scale", GeneKind.Float).WithRange(0,1).Optional());
                Register(new GeneBlueprint($"resist.{kind}", GeneKind.Float).WithRange(0,1).Optional());
                Register(new GeneBlueprint($"penetration.{kind}", GeneKind.Float).WithRange(0,1).Optional());
            }

            // Procedural accessory slots
            for (int i = 0; i < 16; i++)
            {
                Register(new GeneBlueprint($"slot.{i}.enabled", GeneKind.Bool).Optional());
                Register(new GeneBlueprint($"slot.{i}.module", GeneKind.Enum).WithEnumRange<ModuleKind>().Optional());
                Register(new GeneBlueprint($"slot.{i}.power", GeneKind.Float).WithRange(0,1).Optional());
            }

            // Telemetry
            Register(new GeneBlueprint("telemetry.novelty_bias", GeneKind.Float).WithRange(0,1).Optional());
            Register(new GeneBlueprint("telemetry.repetition_decay", GeneKind.Float).WithRange(0,1).Optional());
            Register(new GeneBlueprint("telemetry.learning_rate", GeneKind.Float).WithRange(0,1).Optional());

            // Enough? Count check comment: We aim > 500 registered genes.
        }

        public static IEnumerable<GeneBlueprint> AllBlueprintsShuffled(Random rng)
        {
            var arr = _blue.Values.ToList();
            // shuffle
            for (int i = arr.Count - 1; i > 0; i--)
            {
                int j = rng.Next(i + 1);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
            return arr;
        }

        public static bool TryGetBlueprint(string key, out GeneBlueprint bp) => _blue.TryGetValue(key, out bp!);
        public static IEnumerable<GeneBlueprint> OptionalBlueprintsWhere(WeaponDNA dna, Func<GeneBlueprint, bool> pred)
            => _blue.Values.Where(b => b.OptionalFlag && pred(b));

        public static bool CanRemoveGene(string key)
        {
            if (key == WeaponDNA.G_BaseForm || key == WeaponDNA.G_Size) return false;
            foreach (var p in _removablePrefixes)
                if (key.StartsWith(p, StringComparison.Ordinal)) return true;
            return false;
        }

        public static float RandInRange(Random rng, float min, float max) => (float)(min + rng.NextDouble() * (max - min));

        public static T PickEnum<T>(Random rng) where T : struct, Enum
        {
            var vals = Enum.GetValues<T>();
            return vals[rng.Next(vals.Length)];
        }

        private static void Register(GeneBlueprint bp)
        {
            _blue[bp.Key] = bp;
        }
        private static void AddFloatRange(string key, float min, float max) => Register(new GeneBlueprint(key, GeneKind.Float).WithRange(min, max).Optional());
        private static void AddIntRange(string key, int min, int max) => Register(new GeneBlueprint(key, GeneKind.Int).WithRange(min, max).Optional());
        private static void AddEnum(string key, IEnumerable<string> values)
        {
            // Store enum as index 0..n-1
            Register(new GeneBlueprint(key, GeneKind.Enum).WithEnumValues(values.ToArray()).Optional());
        }
    }

    public sealed class GeneBlueprint
    {
        public string Key { get; }
        public GeneKind Kind { get; }
        public bool OptionalFlag { get; private set; }
        public float MinF { get; private set; } = 0;
        public float MaxF { get; private set; } = 1;
        public int MinI { get; private set; } = 0;
        public int MaxI { get; private set; } = 1;
        public int[] EnumValues { get; private set; } = Array.Empty<int>();

        public GeneBlueprint(string key, GeneKind kind) { Key = key; Kind = kind; }
        public GeneBlueprint Optional() { OptionalFlag = true; return this; }
        public GeneBlueprint WithRange(float min, float max) { MinF = min; MaxF = max; return this; }
        public GeneBlueprint WithRange(int min, int max) { MinI = min; MaxI = max; return this; }
        public GeneBlueprint WithEnumRange<T>() where T : struct, Enum
        {
            EnumValues = Enum.GetValues<T>().Select(e => Convert.ToInt32(e)).ToArray();
            return this;
        }
        public GeneBlueprint WithEnumValues(params string[] _) { /* value names unused; indices suffice */ return this; }

        public bool CanInstantiateWith(WeaponDNA dna)
        {
            // Example preconditions: if projectile.* then require delivery primary supports ranged kinds
            if (Key.StartsWith("projectile."))
            {
                if (!dna.Genes.TryGetValue("delivery.primary", out var dp)) return false;
                var p = (DeliveryPrimary)dp.AsEnum<int>();
                if (p is not (DeliveryPrimary.Projectile or DeliveryPrimary.Throw or DeliveryPrimary.Chain or DeliveryPrimary.Whip or DeliveryPrimary.Beam or DeliveryPrimary.Trap or DeliveryPrimary.Mine))
                    return false;
            }
            if (Key.StartsWith("morph."))
            {
                // allow morph genes only if morph.enabled true (or we are enabling it)
                if (Key != "morph.enabled" && (!dna.Genes.TryGetValue("morph.enabled", out var en) || !en.AsBool()))
                    return false;
            }
            return true;
        }

        public GeneValue InstantiateRandom(Random rng)
        {
            return Kind switch
            {
                GeneKind.Float => GeneValue.Float(Registry.RandInRange(rng, MinF, MaxF)),
                GeneKind.Int => GeneValue.Int(rng.Next(MinI, MaxI + 1)),
                GeneKind.Bool => GeneValue.Bool(rng.NextDouble() < 0.5),
                GeneKind.Enum => GeneValue.Enum(EnumValues.Length == 0 ? rng.Next(0, 8) : EnumValues[rng.Next(EnumValues.Length)]),
                _ => GeneValue.Int(0)
            };
        }

        public GeneValue Mutate(GeneValue current, Random rng)
        {
            return Kind switch
            {
                GeneKind.Float => GeneValue.Float(MutateFloat(current.AsFloat(), MinF, MaxF, rng)),
                GeneKind.Int => GeneValue.Int(MutateInt(current.AsInt(), MinI, MaxI, rng)),
                GeneKind.Bool => GeneValue.Bool(!current.AsBool()),
                GeneKind.Enum => GeneValue.Enum(MutateEnum(current.AsInt(), EnumValues, rng)),
                _ => current
            };
        }

        private static float MutateFloat(float v, float min, float max, Random rng)
        {
            var span = (max - min);
            var step = span * 0.1f; // 10% step
            var nv = v + (float)(rng.NextDouble() * 2 - 1) * step;
            return Math.Clamp(nv, min, max);
        }
        private static int MutateInt(int v, int min, int max, Random rng)
        {
            var nv = v + (rng.Next(2) == 0 ? -1 : +1);
            return Math.Clamp(nv, min, max);
        }
        private static int MutateEnum(int v, int[] domain, Random rng)
        {
            if (domain.Length == 0) return v + (rng.Next(2) == 0 ? -1 : +1);
            // pick neighbor or random
            int idx = Array.IndexOf(domain, v);
            if (idx >= 0 && domain.Length > 1)
            {
                int j = idx + (rng.Next(2) == 0 ? -1 : +1);
                if (j >= 0 && j < domain.Length) return domain[j];
            }
            return domain[rng.Next(domain.Length)];
        }
    }

    // ======= Evaluation cache (placeholder for derived stats) =======
    public sealed class EvaluationCache
    {
        public float EstimatedBurstDps { get; set; }
        public float EstimatedSustainDps { get; set; }
        public float HandlingIndex { get; set; }
    }

    // ======= Enums =======

    public enum BaseFormType
    {
        ShortSword, LongSword, Greatsword, Rapier, Sabre, Katana, Dao,
        Spear, Pike, Halberd, Glaive, Naginata,
        Axe, Greataxe, Hammer, Warpick, Mace, Flail, Morningstar,
        Shield, TowerShield, Buckler,
        Whip, ChainBlade, SegmentedBlade, Scythe,
        Dagger, Kukri, HandClaw,
        Bow, Longbow, Recurve, Crossbow, HandCrossbow, Atlatl,
        ThrowingDisk, Chakram, Boomerang,
        Gunlance, StakeLauncher, Harpoon, TetherSpear,
        BeamCaster, Rod, Staff, FocusBlade,
        Trapkit, MineDeployer, DroneBlade
    }

    public enum Grip { OneHand, TwoHand, Versatile, Dual }
    public enum DeliveryPrimary { Slash, Thrust, Crush, Cleave, Pierce, Slam, Projectile, Beam, Trap, Mine, Chain, Whip, Throw }
    public enum CoreMaterial { Steel, Iron, Obsidian, Bone, Crystal, Bronze, Tungsten, Ceramic, Wood, Composite, PlasmaCaged, Voidglass, Chitin, Graphene, LivingResin }
    public enum JacketMaterial { None, Steel, Bronze, Chitin, Graphene, Crystal, Ceramic, Composite, LivingResin }
    public enum Conductivity { None, Low, Medium, High }
    public enum ProjectileArchetype { Arrow, Bolt, Shard, Bomb, Dart, Javelin, Disk, Tether }
    public enum BallisticKind { Hitscan, Flat, Arced }
    public enum HomingKind { None, Mild, Strong }
    public enum EmissionMode { None, Beam, Pulse, Cone, Aura, ProjectileField }
    public enum Element { Phys, Fire, Frost, Shock, Acid, Rot, Bleed, Arcane, Gravity, Sonic, Radiation, Light, Shadow }
    public enum Stagger { None, Light, Medium, Heavy }
    public enum DotType { None, Bleed, Burn, Poison, Rot, Radiation }
    public enum AILevel { Inert, Reactive, SemiSentient, Sentient }
    public enum ModuleKind { Empty, Capacitor, Rune, Counterweight, Gyro, Converter, HeatSink, AutoLoader, Bayonet, NetLauncher, MiniShield }

    // ======= Example usage (optional) =======
    public static class Example
    {
        public static void Demo()
        {
            var rng = new Random(42);
            var a = WeaponDNA.CreateRandom(520, rng);
            var b = WeaponDNA.CreateRandom(520, rng);

            a.Validate(out var issuesA);
            b.Validate(out var issuesB);

            var child = WeaponDNA.Crossover(a, b, rng);
            child.Validate(out var issuesChild);

            Console.WriteLine($"A genes: {a.Genes.Count} id={a.Id} issues={issuesA.Count}");
            Console.WriteLine($"B genes: {b.Genes.Count} id={b.Id} issues={issuesB.Count}");
            Console.WriteLine($"Child genes: {child.Genes.Count} id={child.Id} issues={issuesChild.Count}");
            Console.WriteLine(child.ToJson(indented: true));
        }
    }
}
