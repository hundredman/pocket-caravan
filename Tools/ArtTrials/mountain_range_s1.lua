-- Stage 1: NEW modular master, never extract pieces from approved whole mountains.
local root=app.params.root
local out=root..'/ArtReview/MountainRange-S1'
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
 local s=Sprite(image.width,image.height,ColorMode.RGB)
 s.cels[1].image=image;s.layers[1].name='Authored modular ridge; fixed 16px seam ports'
 s:saveAs(path..'.aseprite');s:saveCopyAs(path..'.png');s:close()
end
local src=app.open(out..'/AI/master-ai.png');local image=src.cels[1].image
local x0,y0,x1,y1=image.width,image.height,-1,-1
for y=0,image.height-1 do for x=0,image.width-1 do if pc.rgbaA(image:getPixel(x,y))>=128 then
 x0=math.min(x0,x);y0=math.min(y0,y);x1=math.max(x1,x);y1=math.max(y1,y)
end end end
local scale=math.min(496/(x1-x0+1),176/(y1-y0+1))
local w,h=math.floor((x1-x0+1)*scale+.5),math.floor((y1-y0+1)*scale+.5)
local base=Image(512,192,ColorMode.RGB)
for y=0,h-1 do for x=0,w-1 do
 local c=image:getPixel(x0+math.floor((x+.5)/w*(x1-x0+1)),y0+math.floor((y+.5)/h*(y1-y0+1)))
 if pc.rgbaA(c)>=128 then base:drawPixel(math.floor((512-w)/2)+x,187-h+y,nearest(c))end
end end
save(base,out..'/master-before-ports')
local function bounds(im,x)
 local lo,hi=im.height,-1
 for y=0,im.height-1 do if pc.rgbaA(im:getPixel(x,y))>0 then lo=math.min(lo,y);hi=math.max(hi,y)end end
 assert(hi>lo,'Empty connecting cross-section at '..x)
 return lo,hi
end
-- Fixed, native-size port spans a natural rock section of the new master.
-- Each exit uses its left half and every entrance its right half.
local port=Image(16,192,ColorMode.RGB)
-- Use the low saddle, not the high summit, so joins do not grow narrow spikes.
for y=0,191 do for x=0,15 do port:drawPixel(x,y,base:getPixel(296+x,y))end end
save(port,out..'/port-template')
local master=Image(base)
-- Boundaries follow natural low saddles, keeping the broad central summit intact.
for _,boundary in ipairs({128,320,384})do
 for dx=-24,23 do
  local x=boundary+dx
  local portX=math.max(0,math.min(15,dx+8))
  local t=(dx>=-8 and dx<=7)and 1 or (dx<-8 and (dx+24)/16 or (23-dx)/16)
  if t==1 then
   for y=0,191 do master:drawPixel(x,y,port:getPixel(portX,y))end
  elseif t>0 then
   local a,b=bounds(base,x);local c,d=bounds(port,portX)
   local top=math.floor(a*(1-t)+c*t+.5);local bottom=math.floor(b*(1-t)+d*t+.5)
   for y=0,191 do master:drawPixel(x,y,0)end
   for y=top,bottom do
    local f=(y-top)/math.max(1,bottom-top)
    local p=base:getPixel(x,math.floor(a+f*(b-a)+.5))
    local q=port:getPixel(portX,math.floor(c+f*(d-c)+.5))
    if pc.rgbaA(p)>0 and pc.rgbaA(q)>0 then
     master:drawPixel(x,y,nearest(pc.rgba(math.floor(pc.rgbaR(p)*(1-t)+pc.rgbaR(q)*t+.5),math.floor(pc.rgbaG(p)*(1-t)+pc.rgbaG(q)*t+.5),math.floor(pc.rgbaB(p)*(1-t)+pc.rgbaB(q)*t+.5),255)))
    elseif (t<.5 and pc.rgbaA(p)>0)or(t>=.5 and pc.rgbaA(q)>0)then master:drawPixel(x,y,t<.5 and p or q)end
   end
  end
 end
end
save(master,out..'/master')
local cuts={0,128,320,384,512}
for i,name in ipairs({'west-cap','body-a','body-b','east-cap'})do
 local piece=Image(cuts[i+1]-cuts[i],192,ColorMode.RGB)
 for y=0,191 do for x=0,piece.width-1 do piece:drawPixel(x,y,master:getPixel(cuts[i]+x,y))end end
 save(piece,out..'/Native/'..name)
end
src:close();print('RIDGE_S1 '..w..'x'..h..' master; 4 purpose-made modules, 16px ports')
