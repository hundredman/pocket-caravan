-- Whole generated plant, native32 canvas. Only transparent padding is omitted.
local root=app.params.root
local pc=app.pixelColor
local function C(h)return pc.rgba(tonumber(h:sub(1,2),16),tonumber(h:sub(3,4),16),tonumber(h:sub(5,6),16),255)end
local palette={}
for _,h in ipairs({"69452f","b77c3e","e2ac52","f7d17b","7f8745","9caa54","bbc571"})do table.insert(palette,C(h))end
local src=app.open(root.."/ripe-rice-ai.png");local im=src.cels[1].image
local x0,y0,x1,y1=im.width,im.height,-1,-1
for y=0,im.height-1 do for x=0,im.width-1 do
 if pc.rgbaA(im:getPixel(x,y))>=128 then x0=math.min(x0,x);x1=math.max(x1,x);y0=math.min(y0,y);y1=math.max(y1,y)end
end end
assert(x1>x0 and y1>y0)
local scale=math.min(22/(x1-x0+1),28/(y1-y0+1))
local w,h=math.floor((x1-x0+1)*scale+.5),math.floor((y1-y0+1)*scale+.5)
local s=Sprite(32,32,ColorMode.RGB);s.layers[1].name="Growing ripe rice; whole AI design on native grid"
local dst=s.cels[1].image
for y=0,h-1 do for x=0,w-1 do
 local c=im:getPixel(x0+math.min(x1-x0,math.floor((x+.5)/w*(x1-x0+1))),y0+math.min(y1-y0,math.floor((y+.5)/h*(y1-y0+1))))
 if pc.rgbaA(c)>=128 then
  local best,d=palette[1],1e10
  for _,p in ipairs(palette)do
   local v=(pc.rgbaR(p)-pc.rgbaR(c))^2+(pc.rgbaG(p)-pc.rgbaG(c))^2+(pc.rgbaB(p)-pc.rgbaB(c))^2
   if v<d then best,d=p,v end
  end
  dst:drawPixel(math.floor((32-w)/2)+x,31-h+y,best)
 end
end end
s:saveAs(root.."/rice-ripe.aseprite");s:saveCopyAs(root.."/rice-ripe.png")
s:close();src:close();print("RIPE_RICE_NATIVE "..w.."x"..h)
