-- Native authored NS strips and four 90-degree turns; no approved sprite rotation.
local root=app.params.root
local out=root..'/ArtReview/MountainRange-S2b'
local pc=app.pixelColor
local palette={}
for _,h in ipairs({'2e241d','77746c','a29b87','c4bca7','52673c','7d9156','a2b775'})do
 table.insert(palette,pc.rgba(tonumber(h:sub(1,2),16),tonumber(h:sub(3,4),16),tonumber(h:sub(5,6),16),255))
end
local function nearest(c)
 local best,dist=palette[1],1e20
 for _,p in ipairs(palette)do
  local d=(pc.rgbaR(c)-pc.rgbaR(p))^2+(pc.rgbaG(c)-pc.rgbaG(p))^2+(pc.rgbaB(c)-pc.rgbaB(p))^2
  if d<dist then best,dist=p,d end
 end
 return best
end
local function save(im,path)
 local s=Sprite(im.width,im.height,ColorMode.RGB);s.cels[1].image=im
 s.layers[1].name='Authored S2b geometry; fixed upper-left light and native ports'
 s:saveAs(path..'.aseprite');s:saveCopyAs(path..'.png');s:close()
end
local function native(name,w,h,maxw,maxh,dx)
 local src=app.open(out..'/AI/'..name..'.png');local im=src.cels[1].image
 local x0,y0,x1,y1=im.width,im.height,-1,-1
 for y=0,im.height-1 do for x=0,im.width-1 do if pc.rgbaA(im:getPixel(x,y))>=128 then
  x0=math.min(x0,x);y0=math.min(y0,y);x1=math.max(x1,x);y1=math.max(y1,y)
 end end end
 local scale=math.min(maxw/(x1-x0+1),maxh/(y1-y0+1))
 local iw,ih=math.floor((x1-x0+1)*scale+.5),math.floor((y1-y0+1)*scale+.5)
 local result=Image(w,h,ColorMode.RGB)
 for y=0,ih-1 do for x=0,iw-1 do
  local c=im:getPixel(x0+math.floor((x+.5)/iw*(x1-x0+1)),y0+math.floor((y+.5)/ih*(y1-y0+1)))
  local px=math.floor((w-iw)/2)+x+(dx or 0)
  if pc.rgbaA(c)>=128 and px>=0 and px<w then result:drawPixel(px,math.floor((h-ih)/2)+y,nearest(c))end
 end end
 src:close();print(name..' native footprint '..iw..'x'..ih);return result
end
local function region(im,x0,y0,w,h)
 local result=Image(w,h,ColorMode.RGB)
 for y=0,h-1 do for x=0,w-1 do result:drawPixel(x,y,im:getPixel(x+x0,y+y0))end end
 return result
end
local function span(values)
 local a,b=#values,-1
 for i,c in ipairs(values)do if pc.rgbaA(c)>0 then a=math.min(a,i-1);b=math.max(b,i-1)end end
 return a,b
end
local function join(im,side,offset,template)
 local vertical=side=='W'or side=='E'
 local forward=side=='W'or side=='N'
 local across=vertical and im.height or im.width
 local along=vertical and im.width or im.height
 local base=Image(im)
 local function line(pos)
  local v={}
  for k=0,across-1 do v[k+1]=vertical and base:getPixel(pos,k)or base:getPixel(k,pos)end
  return v
 end
 for depth=0,39 do
  local pos=forward and depth or along-1-depth
  local pindex=forward and math.min(15,8+depth)or math.max(0,7-depth)
  local target={}
  for k=0,across-1 do
   local c=0
   if k>=offset and k<offset+192 then c=vertical and template:getPixel(pindex,k-offset)or template:getPixel(k-offset,pindex)end
   target[k+1]=c
  end
  local a0=line(pos);local a,b=span(a0)
  if b<=a then
   for delta=1,along-1 do
    local p=forward and math.min(along-1,pos+delta)or math.max(0,pos-delta)
    a0=line(p);a,b=span(a0);if b>a then break end
   end
  end
  local c,d=span(target);assert(b>a and d>c,'Missing connecting cross-section')
  local t=depth<8 and 1 or (40-depth)/32
  local result={};for k=1,across do result[k]=0 end
  if t==1 then result=target else
   local lo=math.floor(a*(1-t)+c*t+.5);local hi=math.floor(b*(1-t)+d*t+.5)
   for k=lo,hi do
    local f=(k-lo)/math.max(1,hi-lo)
    local p=a0[math.floor(a+f*(b-a)+.5)+1];local q=target[math.floor(c+f*(d-c)+.5)+1]
    if pc.rgbaA(p)>0 and pc.rgbaA(q)>0 then
     result[k+1]=nearest(pc.rgba(math.floor(pc.rgbaR(p)*(1-t)+pc.rgbaR(q)*t+.5),math.floor(pc.rgbaG(p)*(1-t)+pc.rgbaG(q)*t+.5),math.floor(pc.rgbaB(p)*(1-t)+pc.rgbaB(q)*t+.5),255))
    elseif(t<.5 and pc.rgbaA(p)>0)or(t>=.5 and pc.rgbaA(q)>0)then result[k+1]=t<.5 and p or q end
   end
  end
  for k=0,across-1 do
   if vertical then im:drawPixel(pos,k,result[k+1])else im:drawPixel(k,pos,result[k+1])end
  end
 end
end
local ns= native('north-south-master',192,448,176,432)
save(ns,out..'/north-south-before-ports')
-- A low shoulder matches the arms; the broad summit is not a connecting port.
local nsPort=region(ns,0,296,192,16);save(nsPort,out..'/north-south-port')
local nsMaster=Image(192,448,ColorMode.RGB)
for _,r in ipairs({{'north-cap',0,128},{'north-south-body',128,192},{'south-cap',320,128}})do
 local name,y,h=table.unpack(r);local im=region(ns,0,y,192,h)
 if name~='north-cap'then join(im,'N',0,nsPort)end
 if name~='south-cap'then join(im,'S',0,nsPort)end
 save(im,out..'/Native/'..name);nsMaster:drawImage(im,Point(0,y))
end
save(nsMaster,out..'/north-south-master')
local ewSource=app.open(root..'/ArtReview/Approved/MountainRange-S1/port-template.png')
local ewPort=ewSource.cels[1].image
for _,spec in ipairs({{'turn-w-n','W',64,'N',64},{'turn-w-s','W',0,'S',64},{'turn-e-n','E',64,'N',0},{'turn-e-s','E',0,'S',0}})do
 local name,ew,ey,nsSide,nx=table.unpack(spec)
 local f=io.open(out..'/AI/'..name..'.png','rb')
 if f then
  f:close()
  -- A larger authored master keeps the rock-face thickness consistent with S1.
  -- Continuation arms extend beyond the exported corner role; no closed ends are reused.
  local source=native(name,384,384,376,376)
  save(source,out..'/'..name..'-source-master')
  local sx=ew=='W'and 128 or 0;local sy=nsSide=='N'and 128 or 0
  local im=region(source,sx,sy,256,256);save(im,out..'/'..name..'-before-ports')
  join(im,ew,ey,ewPort);join(im,nsSide,nx,nsPort)
  save(im,out..'/Native/'..name)
 else error('Required source missing: '..name)end
end
ewSource:close();print('S2b native modules authored with unchanged EW and new NS ports')
