-- Preserve native canvas, alpha, silhouette and palette. Only isolated interior color noise changes.
local source=assert(app.params.source)
local output=assert(app.params.output)
local sprite=app.open(source)
local image=sprite.cels[1].image
local pc=app.pixelColor
local strength=app.params.strength or 'micro'
local maxDelta=strength=='micro' and 32 or 44
local minimum=strength=='micro' and 4 or 3
local maximumFraction=strength=='micro' and 0.02 or 0.04
local candidates={}
local opaque=0
local function distance(a,b)
 local r=pc.rgbaR(a)-pc.rgbaR(b)
 local g=pc.rgbaG(a)-pc.rgbaG(b)
 local bl=pc.rgbaB(a)-pc.rgbaB(b)
 return r*r+g*g+bl*bl
end
for y=0,image.height-1 do
 for x=0,image.width-1 do
  local center=image:getPixel(x,y)
  if pc.rgbaA(center)==255 then
   opaque=opaque+1
   if x>0 and y>0 and x<image.width-1 and y<image.height-1 then
    local counts={};local interior=true
    for dy=-1,1 do for dx=-1,1 do
     if dx~=0 or dy~=0 then
      local value=image:getPixel(x+dx,y+dy)
      if pc.rgbaA(value)~=255 then interior=false else counts[value]=(counts[value] or 0)+1 end
     end
    end end
    if interior and (counts[center] or 0)<=1 then
     local best=nil;local bestCount=0;local bestDist=math.huge
     for color,count in pairs(counts) do
      local diff=distance(center,color)
      if color~=center and count>=minimum and diff<=maxDelta*maxDelta and
       (count>bestCount or (count==bestCount and diff<bestDist) or (count==bestCount and diff==bestDist and (best==nil or color<best))) then
       best=color;bestCount=count;bestDist=diff
      end
     end
     if best then table.insert(candidates,{x=x,y=y,color=best,count=bestCount,distance=bestDist}) end
    end
   end
  end
 end
end
table.sort(candidates,function(a,b)
 if a.count~=b.count then return a.count>b.count end
 if a.distance~=b.distance then return a.distance<b.distance end
 if a.y~=b.y then return a.y<b.y end
 return a.x<b.x
end)
local limit=math.floor(opaque*maximumFraction)
local changed=math.min(#candidates,limit)
for i=1,changed do local c=candidates[i];image:drawPixel(c.x,c.y,c.color) end
sprite:saveAs(output..'.aseprite')
sprite:saveCopyAs(output..'.png')
print('CLEANUP '..strength..' '..changed..'/'..opaque)
sprite:close()
