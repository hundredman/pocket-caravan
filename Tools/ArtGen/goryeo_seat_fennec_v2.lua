-- Edit the native v1 Aseprite drawing into an actually lowered sitting posture.
-- Move the whole drawing down four pixels (including the intact ears/face),
-- then redraw the compressed coat/lap, wide folded thighs, shoes and resting tail.
local root=app.params.aseprite_mcp_workspace
local spr=app.open(root..'/inputs/goryeo-seat-fennec-v1.aseprite')
assert(spr.width==32 and spr.height==48)
local cel=spr.cels[1]
local original=cel.image
local im=Image(32,48,ColorMode.RGB)
im:drawImage(original,Point(cel.position.x,cel.position.y+4))
local pc=app.pixelColor.rgba
local c={o=pc(59,36,22,255),p=pc(59,75,126,255),P=pc(108,118,152,255),
 q=pc(59,61,90,255),f=pc(239,201,140,255),F=pc(244,215,167,255),
 k=pc(110,68,41,255),K=pc(154,122,98,255),a=pc(184,120,40,255),
 A=pc(215,141,44,255),t=pc(243,211,138,255),b=pc(47,138,135,255),
 B=pc(50,113,107,255),s=pc(234,217,176,255),r=pc(156,59,44,255),T=pc(122,75,46,255)}
local function span(y,x0,x1,ch)for x=x0,x1 do im:drawPixel(x,y,c[ch])end end
app.transaction(function()
 for y=30,47 do for x=0,31 do im:drawPixel(x,y,0)end end
 span(30,5,26,'o');span(30,6,14,'A');span(30,15,16,'t');span(30,17,25,'a')
 span(31,5,26,'o');span(31,6,10,'f');span(31,11,20,'b');span(31,17,19,'s');span(31,18,18,'r');span(31,21,25,'f')
 span(32,4,27,'o');span(32,5,11,'P');span(32,12,19,'t');span(32,20,26,'P')
 span(33,4,27,'o');span(33,5,12,'p');span(33,13,14,'q');span(33,17,18,'q');span(33,19,26,'p')
 span(34,5,26,'o');span(34,6,12,'P');span(34,13,14,'q');span(34,17,18,'q');span(34,19,25,'P')
 span(35,6,13,'o');span(35,7,12,'q');span(35,18,25,'o');span(35,19,24,'q')
 span(36,6,13,'o');span(36,7,12,'K');span(36,18,25,'o');span(36,19,24,'K')
 span(37,6,13,'o');span(37,7,12,'k');span(37,18,25,'o');span(37,19,24,'k')
 -- The tail sits behind the right thigh, rather than extending the standing leg line.
 span(31,27,29,'o');span(32,27,30,'o');span(32,28,29,'F')
 span(33,28,31,'o');span(33,29,30,'f')
 span(34,28,31,'o');span(34,29,30,'f')
 span(35,27,31,'o');span(35,28,30,'T')
 span(36,27,30,'o');span(36,28,29,'T')
 span(37,26,29,'o');span(37,27,28,'T');span(38,26,28,'o')
 spr.cels[1].image=im
 spr.cels[1].position=Point(0,0)
end)
spr:saveCopyAs(root..'/outputs/goryeo-seats/arabia-seated-v2.aseprite')
spr:saveCopyAs(root..'/outputs/goryeo-seats/arabia-seated-v2.png')
print('Seated v2: four pixels lower head/shoulders, compressed torso and folded knees')
