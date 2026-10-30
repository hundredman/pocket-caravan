-- Preview-only: new whole canopy cluster, complete v3 plants at front edge.
local root=app.params.root
local out=root..'/ArtReview/RiceField-v5'
local pc=app.pixelColor
local colors={}
for _,h in ipairs({'b77c3e','e2ac52','f7d17b','7f8745','9caa54','bbc571'})do
 table.insert(colors,pc.rgba(tonumber(h:sub(1,2),16),tonumber(h:sub(3,4),16),tonumber(h:sub(5,6),16),255))
end
local src=app.open(out..'/canopy-ai.png');local im=src.cels[1].image
local x0,y0,x1,y1=im.width,im.height,-1,-1
for y=0,im.height-1 do for x=0,im.width-1 do if pc.rgbaA(im:getPixel(x,y))>=128 then
 x0=math.min(x0,x);y0=math.min(y0,y);x1=math.max(x1,x);y1=math.max(y1,y)
end end end
local scale=math.min(60/(x1-x0+1),40/(y1-y0+1))
local w,h=math.floor((x1-x0+1)*scale+.5),math.floor((y1-y0+1)*scale+.5)
local canopy=Image(64,48,ColorMode.RGB)
for y=0,h-1 do for x=0,w-1 do
 local c=im:getPixel(x0+math.floor((x+.5)/w*(x1-x0+1)),y0+math.floor((y+.5)/h*(y1-y0+1)))
 if pc.rgbaA(c)>=128 then
  local best,dist=colors[1],1e20
  for _,p in ipairs(colors)do
   local d=(pc.rgbaR(p)-pc.rgbaR(c))^2+(pc.rgbaG(p)-pc.rgbaG(c))^2+(pc.rgbaB(p)-pc.rgbaB(c))^2
   if d<dist then best,dist=p,d end
  end
  canopy:drawPixel(math.floor((64-w)/2)+x,47-h+y,best)
 end
end end
local function save(image,path)
 local s=Sprite(image.width,image.height,ColorMode.RGB)
 s.cels[1].image=image;s.layers[1].name='Whole rice canopy assembly; native pixel grid'
 s:saveAs(path..'.aseprite');s:saveCopyAs(path..'.png');s:close()
end
save(canopy,out..'/canopy')
local plant=app.open(root..'/ArtReview/RipeRice-v3/rice-ripe.png')
local p=plant.cels[1].image
local function field(width,height)
 local image=Image(width+64,height+64,ColorMode.RGB)
 for row=0,height//16-1 do for col=0,width//32-1 do
  image:drawImage(canopy,Point(32+col*32+16+(row%2)*8-32,48+row*16+15-48))
 end end
 -- Preserve upright stems in the nearest row, hiding interior long stalks.
 for col=0,width//16-1 do image:drawImage(p,Point(32+col*16+12-16,48+height-1-32))end
 return image
end
local master=field(192,192);save(master,out..'/field-master')
save(field(128,64),out..'/small-plot')
local old=Image(192,128,ColorMode.RGB)
for row=0,4 do for col=0,8 do old:drawImage(p,Point(32+col*14+7+(row%2)*4-16,48+row*14+13-32))end end
save(old,out..'/previous-small-plot')
local boundsX={0,96,160,256};local boundsY={0,112,176,256}
local names={{'north-west','north','north-east'},{'west','center','east'},{'south-west','south','south-east'}}
for gy=1,3 do for gx=1,3 do
 local piece=Image(boundsX[gx+1]-boundsX[gx],boundsY[gy+1]-boundsY[gy],ColorMode.RGB)
 for y=0,piece.height-1 do for x=0,piece.width-1 do
  piece:drawPixel(x,y,master:getPixel(boundsX[gx]+x,boundsY[gy]+y))
 end end
 save(piece,out..'/Native/'..names[gy][gx])
end end
for _,size in ipairs({{128,128},{192,192},{320,128},{128,320},{256,192}})do
 save(field(size[1],size[2]),out..'/Reference/'..size[1]..'x'..size[2])
end
src:close();plant:close();print('RICE_CANOPY_V5 native '..w..'x'..h..'; 9 modules and small plot')
