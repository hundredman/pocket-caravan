-- Native Aseprite edit of the existing pond. Broad flat planes, not texture:
-- grass lip, a cut earth bank, wet contact, shallows and deeper water.
local root=assert(app.params.root)
local pc=app.pixelColor
local function rgb(h)return pc.rgba(tonumber(h:sub(1,2),16),tonumber(h:sub(3,4),16),tonumber(h:sub(5,6),16),255)end
local P={grass=rgb('7a9c66'),grassShade=rgb('6a8b56'),grassLight=rgb('91ad77'),
 earth=rgb('b99887'),face=rgb('9d826e'),wet=rgb('6f705c'),
 shallow=rgb('98b9a9'),water=rgb('7fb9af'),deep=rgb('619a98')}
local s=app.open(assert(app.params.basis));s.layers[1].name='Previous oval reference (hidden)';s.layers[1].isVisible=false
local bank=s:newLayer();bank.name='Grass lip and cut bank'
s:newCel(bank,1,Image(80,48,ColorMode.RGB),Point(0,0));local land=s.cels[#s.cels].image
local water=s:newLayer();water.name='Shallows and depth planes'
s:newCel(water,1,Image(80,48,ColorMode.RGB),Point(0,0));local surface=s.cels[#s.cels].image
local function outer(x,y)
 local v=(y-24)/19.5;local center=39.5+1.8*v-1.2*(v*v-.3)
 return ((x-center)/(35*(1-.035*v)))^2+v*v<=1
end
local function distance(test,x,y,r)
 local d=r+1
 for dy=-r,r do for dx=-r,r do
  local n=math.abs(dx)+math.abs(dy)
  if n>0 and n<=r and not test(x+dx,y+dy) then d=math.min(d,n)end
 end end
 return d
end
local function wet(x,y)
 if not outer(x,y)then return false end
 return distance(outer,x,y,7)>=4-1.5*((y-24)/19.5)
end
local mask=Sprite(80,48,ColorMode.RGB);mask.layers[1].name='Validation-only water footprint';local mi=mask.cels[1].image
for y=0,47 do for x=0,79 do
 if outer(x,y)then
  local d=distance(outer,x,y,7);local v=(y-24)/19.5
  if wet(x,y)then
   local shore=distance(wet,x,y,5);local c=P.water
   if shore<=3 then c=P.shallow end
   -- Depth merges into the far-bank shadow instead of forming concentric rings.
   if ((x-40)/27)^2+((y-18.5)/12.5)^2<1 and shore>2 then c=P.deep end
   if v<-.2 and shore<=2 then c=P.deep end -- far bank casts a contact shadow
   surface:drawPixel(x,y,c);mi:drawPixel(x,y,pc.rgba(255,255,255,255))
  else
   local c=P.earth
   if d<=1 then c=(v>.25 and x<58)and P.grassLight or P.grassShade
   elseif d>=4-1.5*v-1 then c=P.wet
   elseif v<.1 or x>52 then c=P.face end
   land:drawPixel(x,y,c)
  end
 end
end end
-- Small grass lips break the uniform outline without adding side spurs.
for _,p in ipairs({{22,8},{28,7},{56,10},{12,26},{24,40},{55,40}})do
 for dx=0,2 do
  local x,y=p[1]+dx,p[2]+(dx==1 and -1 or 0)
  if outer(x,y)and not wet(x,y)then land:drawPixel(x,y,dx==1 and P.grassLight or P.grassShade)end
 end
end
s:saveAs(root..'/pond.aseprite');s:saveCopyAs(root..'/pond-source.png');s:close()
mask:saveCopyAs(root..'/water-mask.png');mask:close()
-- A true concave water module for the same style. Not used in the convex preset.
local c=Sprite(16,16,ColorMode.RGB);c.layers[1].name='Concave bank module, same palette'
local im=c.cels[1].image
for y=0,15 do for x=0,15 do
 local d=math.sqrt(x*x+y*y)
 local v=d<3 and P.grassShade or d<5 and P.face or d<6 and P.wet or d<9 and P.shallow or P.water
 im:drawPixel(x,y,v)
end end
c:saveAs(root..'/inner-corner.aseprite');c:saveCopyAs(root..'/inner-corner.png');c:close()
print('Sunken pond: native asymmetrical footprint, grass/earth bank, three water planes')
