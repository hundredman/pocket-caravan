-- Native cleanup of the seated-pose concept. Keep the complete original face, ears,
-- orange coat and turquoise belt. Redraw only the lower body, with knees folded outward.
local root=app.params.aseprite_mcp_workspace
local spr=app.open(root..'/inputs/goryeo-seat-fennec.png')
assert(spr.width==32 and spr.height==48)
local im=spr.cels[1].image
local pc=app.pixelColor.rgba
local c={o=pc(59,36,22,255),p=pc(59,75,126,255),P=pc(108,118,152,255),
         q=pc(59,61,90,255),f=pc(239,201,140,255),F=pc(244,215,167,255),
         k=pc(110,68,41,255),K=pc(154,122,98,255),a=pc(184,120,40,255),
         A=pc(215,141,44,255),t=pc(243,211,138,255)}
local function span(y,x0,x1,ch)for x=x0,x1 do im:drawPixel(x,y,c[ch])end end
app.transaction(function()
  for y=31,47 do for x=0,24 do im:drawPixel(x,y,0)end end
  for y=38,47 do for x=25,31 do im:drawPixel(x,y,0)end end
  -- Tunic overlaps thighs; bent elbows bring both hands down onto the knees.
  span(31,7,24,'o');span(31,8,13,'t');span(31,14,18,'A');span(31,19,23,'a')
  span(32,6,25,'o');span(32,7,11,'F');span(32,12,19,'t');span(32,20,24,'f')
  span(33,6,25,'o');span(33,7,11,'f');span(33,12,19,'p');span(33,20,24,'f')
  span(34,6,25,'o');span(34,7,13,'P');span(34,14,17,'q');span(34,18,24,'p')
  span(35,7,24,'o');span(35,8,13,'p');span(35,14,17,'o');span(35,18,23,'q')
  span(36,8,13,'o');span(36,9,12,'K');span(36,18,23,'o');span(36,19,22,'K')
  span(37,8,13,'o');span(37,9,12,'k');span(37,18,23,'o');span(37,19,22,'k')
end)
spr:saveCopyAs(root..'/outputs/goryeo-seats/arabia-seated-front.aseprite')
spr:saveCopyAs(root..'/outputs/goryeo-seats/arabia-seated-front.png')
print('Native seated fennec: 32x48, original upper body retained')
