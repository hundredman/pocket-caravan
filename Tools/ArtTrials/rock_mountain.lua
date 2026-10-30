-- Whole-object prototypes, preview only. No terrain or existing asset mutations.
local root=app.params.root
local out=root..'/ArtReview/RockMountain-v1'
local pc=app.pixelColor
local palette={}
for _,h in ipairs({'2e241d','77746c','a29b87','c4bca7','52673c','7d9156','a2b775'})do
 table.insert(palette,pc.rgba(tonumber(h:sub(1,2),16),tonumber(h:sub(3,4),16),tonumber(h:sub(5,6),16),255))
end
for _,spec in ipairs({{'mountain',192,160,176,142},{'ridge',256,112,244,92},{'foothill',128,80,116,64}})do
 local name,cw,ch,maxw,maxh=table.unpack(spec)
 local src=app.open(out..'/AI/'..name..'.png');local im=src.cels[1].image
 local x0,y0,x1,y1=im.width,im.height,-1,-1
 for y=0,im.height-1 do for x=0,im.width-1 do if pc.rgbaA(im:getPixel(x,y))>=128 then
  x0=math.min(x0,x);y0=math.min(y0,y);x1=math.max(x1,x);y1=math.max(y1,y)
 end end end
 assert(x1>x0 and y1>y0)
 local scale=math.min(maxw/(x1-x0+1),maxh/(y1-y0+1))
 local w,h=math.floor((x1-x0+1)*scale+.5),math.floor((y1-y0+1)*scale+.5)
 local sprite=Sprite(cw,ch,ColorMode.RGB);local dst=sprite.cels[1].image
 sprite.layers[1].name='Whole '..name..'; shared existing moss-rock palette'
 for y=0,h-1 do for x=0,w-1 do
  local c=im:getPixel(x0+math.floor((x+.5)/w*(x1-x0+1)),y0+math.floor((y+.5)/h*(y1-y0+1)))
  if pc.rgbaA(c)>=128 then
   local best,dist=palette[1],1e20
   for _,p in ipairs(palette)do
    local d=(pc.rgbaR(c)-pc.rgbaR(p))^2+(pc.rgbaG(c)-pc.rgbaG(p))^2+(pc.rgbaB(c)-pc.rgbaB(p))^2
    if d<dist then best,dist=p,d end
   end
   dst:drawPixel(math.floor((cw-w)/2)+x,ch-5-h+y,best)
  end
 end end
 sprite:saveAs(out..'/Native/'..name..'.aseprite');sprite:saveCopyAs(out..'/Native/'..name..'.png')
 sprite:close();src:close();print(name..' '..w..'x'..h..' on '..cw..'x'..ch)
end
