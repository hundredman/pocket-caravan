-- New imagegen source sheets only. Palette cleanup/native pixel export before review, not engine transformation.
local root=app.params.repo;local folder=root..'/ArtReview/Goryeo-Atmosphere-A7-20261105/';local pc=app.pixelColor
local palette={{48,34,26},{94,63,42},{151,120,93},{223,170,92},{248,229,192},{255,246,221},{170,68,44},{212,143,42},{186,167,150}}
local function nearest(c)
 local best,dist=nil,1e20
 for _,v in ipairs(palette)do local d=(pc.rgbaR(c)-v[1])^2+(pc.rgbaG(c)-v[2])^2+(pc.rgbaB(c)-v[3])^2;if d<dist then best=v;dist=d end end
 return pc.rgba(best[1],best[2],best[3],255)
end
for _,kind in ipairs({'chicken','dog'})do
 local src=app.open(folder..'Raw/'..kind..'.png');local si=src.cels[1].image;local bounds={};local scale=1e20
 for f=0,1 do
  local x0,y0,x1,y1=si.width,si.height,0,0
  for y=0,si.height-1 do for x=math.floor(si.width*f/2),math.floor(si.width*(f+1)/2)-1 do
   if pc.rgbaA(si:getPixel(x,y))>=224 then x0=math.min(x0,x);y0=math.min(y0,y);x1=math.max(x1,x);y1=math.max(y1,y)end
  end end
  local bw,bh=x1-x0+1,y1-y0+1;bounds[f+1]={x0,y0,bw,bh};scale=math.min(scale,(kind=='dog'and 22 or 14)/bw,(kind=='dog'and 16 or 14)/bh)
 end
 local dest=Sprite(32,32,ColorMode.RGB)
 for f=1,2 do
  if f>1 then local frame=dest:newEmptyFrame();dest:newCel(dest.layers[1],frame,Image(32,32,ColorMode.RGB),Point(0,0))end
  local b=bounds[f];local w,h=math.max(1,math.floor(b[3]*scale)),math.max(1,math.floor(b[4]*scale));local im=dest.cels[f].image;local ox,oy=math.floor((32-w)/2),30-h
  for y=0,h-1 do for x=0,w-1 do
   local c=si:getPixel(b[1]+math.min(b[3]-1,math.floor((x+.5)*b[3]/w)),b[2]+math.min(b[4]-1,math.floor((y+.5)*b[4]/h)))
   if pc.rgbaA(c)>=128 then im:drawPixel(ox+x,oy+y,nearest(c))end
  end end
  dest.frames[f].duration=.65
 end
 dest:saveAs(folder..'Native/'..kind..'.aseprite')
 for f=1,2 do
  local single=Sprite(32,32,ColorMode.RGB);single.cels[1].image:drawImage(dest.cels[f].image,Point(0,0));single:saveAs(folder..'Native/'..kind..'_'..(f-1)..'.png');single:close()
 end
 dest:close();src:close()
end
