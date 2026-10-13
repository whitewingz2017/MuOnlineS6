using Client.Main.Objects.Effects;
static void Check(bool valid, string name) { if (!valid) throw new Exception(name); }
Check(!GreaterFortitudeRules.ShouldEmit(true, 9.99f), "VITALITY must wait past integer tick nine");
Check(GreaterFortitudeRules.ShouldEmit(true, 10f), "VITALITY impact at attack tick ten");
Check(!GreaterFortitudeRules.ShouldEmit(false, 10f), "Other actions must not use VITALITY timing");
Check(GreaterFortitudeRules.ShouldEmit(false, 15f), "Normal impact at tick fifteen");
float velocity = 50f;
float distance = 0f;
for (int i = 0; i <= 20; i++) distance += GreaterFortitudeRules.TakeVelocity(ref velocity);
Check(distance == 2100f && velocity == 155f, "Flight must use speed before acceleration");
float brightness = .5f;
GreaterFortitudeRules.FadeTrail(10, ref brightness);
Check(brightness == .5f, "Grey trail must not fade at tick ten");
GreaterFortitudeRules.FadeTrail(9, ref brightness);
Check(MathF.Abs(brightness - .5f / 1.2f) < .000001f, "Grey trail fade below tick ten");
Console.WriteLine("PASS: MuMain attack-counter impact, action gate, movement order, full travel and grey trail fade.");
