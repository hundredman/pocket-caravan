-- Native 80x48 terrain hole: independent authored silhouette, not traced external art.
local root=assert(app.params.root)
local pc=app.pixelColor
local function rgb(h)return pc.rgba(tonumber(h:sub(1,2),16),tonumber(h:sub(3,4),16),tonumber(h:sub(5,6),16),255)end
local P={edge=rgb('4f694c'),grass=rgb('6a8b56'),earth=rgb('ad9277'),face=rgb('8d775f'),water=rgb('7eafb3'),shadow=rgb('56858e'),ripple=rgb('a4cac6')}
local points={{22,5},{37,5},{43,8},{55,8},{59,12},{67,12},{73,18},{73,25},{68,29},{69,34},{63,39},{51,39},{46,43},{31,43},{26,39},{17,39},{11,34},{11,30},{6,26},{6,20},{11,15},{17,15},{18,10}}
local function outer(x,y)
 local inside=false;local j=#points
 for i=1,#points do
  local a,b=points[i],points[j]
  if ((a[2]>y)~=(b[2]>y))and x<(b[1]-a[1])*(y-a[2])/(b[2]-a[2])+a[1]then inside=not inside end
  j=i
 end
 return inside
end
local function wet(x,y)return outer(x,y)and outer(x-1,y)and outer(x+1,y)and outer(x,y+1)and outer(x,y-4)end
local s=Sprite(80,48,ColorMode.RGB);s.layers[1].name='Terrain cut and far earth face';local land=s.cels[1].image
local water=s:newLayer();water.name='Flat water and narrow bank contact shadow';local surface=Image(80,48,ColorMode.RGB);s:newCel(water,1,surface,Point(0,0));surface=s.cels[#s.cels].image
local mask=Sprite(80,48,ColorMode.RGB);mask.layers[1].name='Validation only water';local mi=mask.cels[1].image
for y=0,47 do for x=0,79 do
 if outer(x,y)then
  if wet(x,y)then
   local shadow=not wet(x,y-1)or not wet(x,y-2)
   surface:drawPixel(x,y,shadow and P.shadow or P.water);mi:drawPixel(x,y,pc.rgba(255,255,255,255))
  else
   local edge=not outer(x-1,y)or not outer(x+1,y)or not outer(x,y+1)or not outer(x,y-1)
   local c=edge and P.edge or P.earth
   if not edge and not outer(x,y-2)then c=P.grass elseif not edge and not outer(x,y-3)then c=P.face end
   land:drawPixel(x,y,c)
  end
 end
end end
-- Broken horizontal marks, no continuous halo or nested depth patch.
for _,p in ipairs({{22,22,4},{48,31,5}})do for dx=0,p[3]-1 do if wet(p[1]+dx,p[2])then surface:drawPixel(p[1]+dx,p[2],P.ripple)end end end
s:saveAs(root..'/pond.aseprite');s:saveCopyAs(root..'/pond-source.png');s:close()
mask:saveCopyAs(root..'/water-mask.png');mask:close()
-- Complete unused concave module, retained for manifest completeness only.
local c=Sprite(16,16,ColorMode.RGB);local im=c.cels[1].image
for y=0,15 do for x=0,15 do local n=x+y;im:drawPixel(x,y,n<3 and P.edge or n<5 and P.earth or n<7 and P.shadow or P.water)end end
c:saveAs(root..'/inner-corner.aseprite');c:saveCopyAs(root..'/inner-corner.png');c:close()
