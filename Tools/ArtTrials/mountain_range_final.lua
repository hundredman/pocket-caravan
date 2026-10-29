-- Final mountain authoring: new junctions, pass, valley and high spring/fall.
local root=app.params.root
local out=root..'/ArtReview/MountainRange-Final'
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
   if k>=offset and k<offset+(vertical and template.height or template.width) then c=vertical and template:getPixel(pindex,k-offset)or template:getPixel(k-offset,pindex)end
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
local rockPalette={table.unpack(palette)}
local function waterPalette(enabled)
 palette={table.unpack(rockPalette)}
 if enabled then for _,h in ipairs({'7fb9af','7a9c66','6a8b56','84675e','ffffff','78a099'})do
  table.insert(palette,pc.rgba(tonumber(h:sub(1,2),16),tonumber(h:sub(3,4),16),tonumber(h:sub(5,6),16),255))
 end end
end
local ewSource=app.open(root..'/ArtReview/Approved/MountainRange-S1/port-template.png')
local nsSource=app.open(root..'/ArtReview/MountainRange-S2b/north-south-port.png')
local ewPort=ewSource.cels[1].image;local nsPort=nsSource.cels[1].image
local waterPort=Image(64,16,ColorMode.RGB)
local sources={}
for i,n in ipairs({'floor_0_12','water_0_7','water_2_7','floor_0_12'})do
 local s=app.open(root..'/Assets/PocketCaravan/Art/NinjaAdventure/Tiles/'..n..'_goryeo_jade.png')
 table.insert(sources,s);waterPort:drawImage(s.cels[1].image,Point((i-1)*16,0))
end
save(waterPort,out..'/water-port')
local groundPort=Image(64,16,ColorMode.RGB)
for y=0,15 do for x=0,63 do groundPort:drawPixel(x,y,waterPort:getPixel(x,(y+8)%16))end end
save(groundPort,out..'/water-ground-port')
for _,name in ipairs({'river-straight','river-outlet'})do
 local im=Image(64,64,ColorMode.RGB)
 for y=0,63 do for x=0,63 do
  local phase=(name=='river-outlet'and y>=32)and 0 or 8
  im:drawPixel(x,y,waterPort:getPixel(x,(y+phase)%16))
 end end
 save(im,out..'/Native/'..name)
end
for _,s in ipairs(sources)do s:close()end
local specs={
 {'branch-w-e-n',512,384,64,128,384,256,{{'W',64},{'E',64},{'N',96}}},
 {'branch-w-e-s',512,384,64,0,384,256,{{'W',0},{'E',0},{'S',96}}},
 {'branch-n-s-w',384,512,128,64,256,384,{{'N',64},{'S',64},{'W',96}}},
 {'branch-n-s-e',384,512,0,64,256,384,{{'N',0},{'S',0},{'E',96}}},
 {'mountain-pass',256,256,0,0,256,256,{{'W',0},{'E',0}}}
}
for _,spec in ipairs(specs)do
 local name,sw,sh,sx,sy,w,h,ports=table.unpack(spec)
 local f=io.open(out..'/AI/'..name..'.png','rb')
 if f then
  f:close();waterPalette(false)
  local source=native(name,sw,sh,sw-8,sh-8);save(source,out..'/'..name..'-source-master')
  local im=region(source,sx,sy,w,h)
  if name=='mountain-pass'then
   -- Move both complete, disconnected walls outward in an authored source master.
   -- Do not erase a rectangular corridor through their inner silhouettes.
   local walls=Image(320,256,ColorMode.RGB)
   for y=0,255 do for x=0,255 do
    local c=im:getPixel(x,y)
    if pc.rgbaA(c)>0 then
     local dx=x<128 and 8 or 40
     walls:drawPixel(x+dx,y,c)
    end
   end end
   save(walls,out..'/mountain-pass-whole-walls-master')
   im=region(walls,32,0,256,256)
  end
  save(im,out..'/'..name..'-before-ports')
  for _,p in ipairs(ports)do join(im,p[1],p[2],(p[1]=='W'or p[1]=='E')and ewPort or nsPort)end
  save(im,out..'/Native/'..name)
 else error('Required source missing: '..name)end
end
local rockColors={};for _,c in ipairs(rockPalette)do rockColors[c]=true end
local function streamJoin(im,side,preserveRocks)
 local localStream=region(im,96,0,64,im.height)
 join(localStream,side,0,waterPort)
 for y=0,im.height-1 do for x=0,63 do
  if not(preserveRocks and rockColors[im:getPixel(x+96,y)])then
   im:drawPixel(x+96,y,localStream:getPixel(x,y))
  end
 end end
end
local waterColors={}
for _,h in ipairs({'7fb9af','78a099','ffffff'})do
 waterColors[pc.rgba(tonumber(h:sub(1,2),16),tonumber(h:sub(3,4),16),tonumber(h:sub(5,6),16),255)]=true
end
local function waterSpan(im,y,x0,x1)
 local lo,hi=x1+1,-1
 for x=x0,x1 do if waterColors[im:getPixel(x,y)]then lo=math.min(lo,x);hi=math.max(hi,x)end end
 return lo,hi
end
local function valleyHead(im)
 -- A 16px lead-in lets the existing bank tiles widen into the original creek.
 -- Only open ground/water is authored; every original rock pixel stays intact.
 local finish
 for y=16,80 do local a,b=waterSpan(im,y,80,175);if b-a>=12 then finish=y+10;break end end
 assert(finish,'No original valley water entrance')
 local endLeft,endRight=waterSpan(im,finish,80,175)
 local original=Image(im);local grass=waterPort:getPixel(0,0)
 for y=0,finish-1 do
  local row=(y+8)%16;local a,b=waterSpan(waterPort,row,0,63)
  local t=math.max(0,math.min(1,(y-7)/math.max(1,finish-8)))
  t=t*t*(3-2*t)
  local left=(96+a)*(1-t)+endLeft*t;local right=(96+b)*(1-t)+endRight*t
  for x=80,175 do
   if not rockColors[original:getPixel(x,y)]then
    local sx=math.floor(a+(x-left)*(b-a)/math.max(1,right-left)+.5)
    local c=(sx>=0 and sx<64)and waterPort:getPixel(sx,row)or grass
    im:drawPixel(x,y,c)
   end
  end
 end
 print('Valley lead-in ends at original water row '..finish)
end
for _,name in ipairs({'stream-valley','spring-waterfall'})do
 local f=io.open(out..'/AI/'..name..'.png','rb')
 if f then
  f:close();waterPalette(true)
  local im
  if name=='stream-valley'then
   local source=native(name,384,256,376,248);save(source,out..'/'..name..'-source-master')
   im=Image(256,272,ColorMode.RGB);im:drawImage(region(source,64,0,256,256),Point(0,16))
  else im=native(name,256,256,248,248)end
  save(im,out..'/'..name..'-before-ports')
  local ridgeOffset=name=='stream-valley'and 16 or 0
  join(im,'W',ridgeOffset,ewPort);join(im,'E',ridgeOffset,ewPort)
  if name=='stream-valley'then valleyHead(im)end
  streamJoin(im,'S',name=='stream-valley');save(im,out..'/Native/'..name)
 else error('Required source missing: '..name)end
end
ewSource:close();nsSource:close();print('Mountain final native authoring complete for available sources')
