-- Preserve each complete generated design. Trim transparent margins only,
-- fit uniformly, sample NEAREST on the existing native canvas, then use the
-- established C7 architectural draft palette. No facade reconstruction by code.
local root=app.params.root
local pc=app.pixelColor
local function rgb(s)return pc.rgba(tonumber(s:sub(1,2),16),tonumber(s:sub(3,4),16),tonumber(s:sub(5,6),16),255)end
local palette={}
for _,h in ipairs({"34271f","40566a","516877","8198a4","b0c1c6","69452f","9b653c","c18b52",
 "bfa679","ddc696","f2dfb3","b77c3e","e2ac52","f7d17b","7d7970","a59d89","c5bba3",
 "49664c","6a8877","82a192","86513b","a66b49","c68d60","ece7d6","c1b99c","302f31",
 "5b432f","84603d","b17b47","d1a46c"})do table.insert(palette,rgb(h))end
local memo={}
local function nearest(c)
 if memo[c]then return memo[c]end
 local r,g,b=pc.rgbaR(c),pc.rgbaG(c),pc.rgbaB(c);local best,d=palette[1],1e10
 for _,p in ipairs(palette)do
  local n=(pc.rgbaR(p)-r)^2+(pc.rgbaG(p)-g)^2+(pc.rgbaB(p)-b)^2
  if n<d then best,d=p,n end
 end
 memo[c]=best;return best
end
for _,a in ipairs(dofile(root.."/Source/specs.lua"))do
 if app.fs.isFile(root.."/AI/"..a.name..".png")then
  local src=app.open(root.."/AI/"..a.name..".png");local im=src.cels[1].image
  local x0,y0,x1,y1=im.width,im.height,-1,-1
  for y=0,im.height-1 do for x=0,im.width-1 do
   if pc.rgbaA(im:getPixel(x,y))>=128 then x0=math.min(x0,x);x1=math.max(x1,x);y0=math.min(y0,y);y1=math.max(y1,y)end
  end end
  assert(x1>x0 and y1>y0,"No opaque asset: "..a.name)
  local ratio=math.min((a.w-4)/(x1-x0+1),(a.h-2)/(y1-y0+1))
  local w,h=math.floor((x1-x0+1)*ratio+.5),math.floor((y1-y0+1)*ratio+.5)
  local ox,oy=math.floor((a.w-w)/2),a.h-1-h
  local native=Sprite(a.w,a.h,ColorMode.RGB);native.layers[1].name="Whole generated design; native NEAREST; C7 draft palette"
  local output=native.cels[1].image
  for y=0,h-1 do for x=0,w-1 do
   local sx=x0+math.min(x1-x0,math.floor((x+.5)/w*(x1-x0+1)))
   local sy=y0+math.min(y1-y0,math.floor((y+.5)/h*(y1-y0+1)))
   local c=im:getPixel(sx,sy)
   -- The boat is an object, not a water tile: omit the generated blue wake.
   local wake=a.name=="boat"and pc.rgbaB(c)>pc.rgbaR(c)+10 and pc.rgbaG(c)>pc.rgbaR(c)+10
   if pc.rgbaA(c)>=128 and not wake then output:drawPixel(ox+x,oy+y,nearest(c))end
  end end
  native:saveAs(root.."/Native/"..a.name..".aseprite");native:saveCopyAs(root.."/Native/"..a.name..".png")
  native:close();src:close()
  print("NATIVE "..a.name.." "..w.."x"..h)
 end
end
