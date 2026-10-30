-- Author whole new AI sprites at native size; never transform existing approved sprites.
local root=app.params.root
local out=root..'/ArtReview/A7d-20261030/Headwaters'
local pc=app.pixelColor
local rock,water={},{}
for _,h in ipairs({'2e241d','77746c','a29b87','c4bca7','52673c','7d9156','a2b775'})do
 table.insert(rock,pc.rgba(tonumber(h:sub(1,2),16),tonumber(h:sub(3,4),16),tonumber(h:sub(5,6),16),255))
end
for _,h in ipairs({'7fb9af','78a099','edf6e7'})do
 table.insert(water,pc.rgba(tonumber(h:sub(1,2),16),tonumber(h:sub(3,4),16),tonumber(h:sub(5,6),16),255))
end
for _,spec in ipairs({{'compact-spring-west',160},{'compact-spring-east',160},{'compact-spring-west-small',128},{'compact-spring-east-small',128},{'compact-spring-west-tiny',96}})do
 local name,N=spec[1],spec[2];local rawname=name:gsub('%-small$',''):gsub('%-tiny$','')
 local src=app.open(out..'/AI/'..rawname..'.png');local im=src.cels[1].image
 local x0,y0,x1,y1=im.width,im.height,-1,-1
 for y=0,im.height-1 do for x=0,im.width-1 do if pc.rgbaA(im:getPixel(x,y))>=128 then x0=math.min(x0,x);y0=math.min(y0,y);x1=math.max(x1,x);y1=math.max(y1,y)end end end
 local scale=math.min(N/(x1-x0+1),N/(y1-y0+1));local w,h=math.floor((x1-x0+1)*scale+.5),math.floor((y1-y0+1)*scale+.5)
 local s=Sprite(N,N,ColorMode.RGB);local dst=s.cels[1].image;s.layers[1].name='Whole compact high spring; native 160x160'
 for y=0,h-1 do for x=0,w-1 do
  local c=im:getPixel(x0+math.floor((x+.5)/w*(x1-x0+1)),y0+math.floor((y+.5)/h*(y1-y0+1)))
  if pc.rgbaA(c)>=128 then
   local r,g,b=pc.rgbaR(c),pc.rgbaG(c),pc.rgbaB(c)
   local palette=((g>r+8 and b>r+8)or(r>215 and g>220 and b>210))and water or rock
   local best,dist=palette[1],1e20
   for _,p in ipairs(palette)do local d=(r-pc.rgbaR(p))^2+(g-pc.rgbaG(p))^2+(b-pc.rgbaB(p))^2;if d<dist then best,dist=p,d end end
   dst:drawPixel(math.floor((N-w)/2)+x,N-h+y,best)
  end
 end end
 s:saveAs(out..'/Native/'..name..'.aseprite');s:saveCopyAs(out..'/Native/'..name..'.png');s:close();src:close()
end
