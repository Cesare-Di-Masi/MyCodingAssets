$: n("[0 1 [2] 3 1 0 ]*2").scale("c:minor").sound("triangle:3").o(4)
.delay(0.5).trans(-12).detune(rand)

$: s( "<hh>*2 <tp>*4 <hh ->*6 <mp ->*4").room(0.2).detune(rand).sound("gm_electric_guitar_muted").o(4)

$: s("lt*16, mt*8").sound("supersaw").trans(-24).o(4).gain(0.6)

$: s("bd*4").bank("korgm1").lpf(perlin.range(500,3000))

$: s("white").lpf(perlin.range(100,1000)).o(4)

$: n("< 3 4!2 - 5*3  8 4>!2*4" ).sound("gm_electric_guitar_jazz").gain(1.2).detune(0.1).fit()
