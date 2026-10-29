-- Purpose-built curved connectors; fixed lighting, no rotated approved sprites.
local root=app.params.root
local out=root..'/ArtReview/MountainRange-S2'
local pc=app.pixelColor
local colors={}
for _,h in ipairs({'2e241d','77746c','a29b87','c4bca7','52673c','7d9156','a2b775'})do
 table.insert(colors,pc.rgba(tonumber(h:sub(1,2),16),tonumber(h:sub(3,4),16),tonumber(h:sub(5,6),16),255))
end
local function nearest(c)
 local best,dist=colors[1],1e20
 for _,p in ipairs(colors)do
  local d=(pc.rgbaR(p)-pc.rgbaR(c))^2+(pc.rgbaG(p)-pc.rgbaG(c))^2+(pc.rgbaB(p)-pc.rgbaB(c))^2
  if d<dist then best,dist=p,d end
 end
 return best
end
local function save(image,path)
 local s=Sprite(image.width,image.height,ColorMode.RGB);s.cels[1].image=image
 s.layers[1].name='Authored curve; original S1 ports; 64px plan-view displacement'
 s:saveAs(path..'.aseprite');s:saveCopyAs(path..'.png');s:close()
end
local function bounds(im,x)
 local a,b=im.height,-1
 for y=0,im.height-1 do if pc.rgbaA(im:getPixel(x,y))>0 then a=math.min(a,y);b=math.max(b,y)end end
 assert(b>a,'Empty port column '..x);return a,b
end
local portSource=app.open(root..'/ArtReview/Approved/MountainRange-S1/port-template.png')
local port=portSource.cels[1].image
for _,spec in ipairs({{'bend-north',64,0},{'bend-south',0,64}})do
 local name,leftOffset,rightOffset=table.unpack(spec)
 local src=app.open(out..'/AI/'..name..'.png');local image=src.cels[1].image
 local x0,y0,x1,y1=image.width,image.height,-1,-1
 for y=0,image.height-1 do for x=0,image.width-1 do if pc.rgbaA(image:getPixel(x,y))>=128 then
  x0=math.min(x0,x);y0=math.min(y0,y);x1=math.max(x1,x);y1=math.max(y1,y)
 end end end
 local scale=math.min(432/(x1-x0+1),240/(y1-y0+1))
 local w,h=math.floor((x1-x0+1)*scale+.5),math.floor((y1-y0+1)*scale+.5)
 local base=Image(448,256,ColorMode.RGB)
 -- Align the new southward source before authoring ports, avoiding raised needles.
 local bottomAnchor=name=='bend-south' and 227 or 251
 for y=0,h-1 do for x=0,w-1 do
  local c=image:getPixel(x0+math.floor((x+.5)/w*(x1-x0+1)),y0+math.floor((y+.5)/h*(y1-y0+1)))
  if pc.rgbaA(c)>=128 then base:drawPixel(math.floor((448-w)/2)+x,bottomAnchor-h+y,nearest(c))end
 end end
 save(base,out..'/'..name..'-before-ports')
 local master=Image(base)
 for i,boundary in ipairs({128,320})do
  local offset=i==1 and leftOffset or rightOffset
  for dx=-24,23 do
   local x=boundary+dx;local px=math.max(0,math.min(15,dx+8))
   local t=(dx>=-8 and dx<=7)and 1 or (dx<-8 and (dx+24)/16 or (23-dx)/16)
   if t>0 then
    local c,d=bounds(port,px);c=c+offset;d=d+offset
    local a,b=bounds(base,x)
    local top=math.floor(a*(1-t)+c*t+.5);local bottom=math.floor(b*(1-t)+d*t+.5)
    for y=0,255 do master:drawPixel(x,y,0)end
    if t==1 then
     for y=0,191 do master:drawPixel(x,y+offset,port:getPixel(px,y))end
    else
     for y=top,bottom do
      local f=(y-top)/math.max(1,bottom-top)
      local p=base:getPixel(x,math.floor(a+f*(b-a)+.5))
      local q=port:getPixel(px,math.floor(c-offset+f*(d-c)+.5))
      if pc.rgbaA(p)>0 and pc.rgbaA(q)>0 then
       master:drawPixel(x,y,nearest(pc.rgba(math.floor(pc.rgbaR(p)*(1-t)+pc.rgbaR(q)*t+.5),math.floor(pc.rgbaG(p)*(1-t)+pc.rgbaG(q)*t+.5),math.floor(pc.rgbaB(p)*(1-t)+pc.rgbaB(q)*t+.5),255)))
      elseif(t<.5 and pc.rgbaA(p)>0)or(t>=.5 and pc.rgbaA(q)>0)then master:drawPixel(x,y,t<.5 and p or q)end
     end
    end
   end
  end
 end
 save(master,out..'/'..name..'-master')
 local connector=Image(192,256,ColorMode.RGB)
 for y=0,255 do for x=0,191 do connector:drawPixel(x,y,master:getPixel(x+128,y))end end
 save(connector,out..'/Native/'..name)
 print(name..' master '..w..'x'..h..'; connector192x256; ports '..leftOffset..'/'..rightOffset)
 src:close()
end
portSource:close()
