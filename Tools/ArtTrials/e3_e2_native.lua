-- Offline native-art preparation only; no production textures/maps are written.
local root=app.params.repo
local folder=root..'/ArtReview/E3-E2-Art-20261103/'
local pc=app.pixelColor
local function color(r,g,b,a)return pc.rgba(r,g,b,a or 255)end
local wood={{46,34,27},{76,49,33},{107,71,43},{145,97,57},{180,129,75},{211,163,105},{236,199,143},{248,222,173}}
local icons={{48,34,26},{93,64,38},{137,101,60},{173,134,83},{206,169,112},{233,207,160},{248,234,202},{255,249,231},{185,66,32},{219,108,43}}
local function nearest(c,palette)
 local r,g,b=pc.rgbaR(c),pc.rgbaG(c),pc.rgbaB(c);local best,dist=nil,1e20
 for _,v in ipairs(palette)do local d=(r-v[1])^2+(g-v[2])^2+(b-v[3])^2;if d<dist then best=v;dist=d end end
 return color(best[1],best[2],best[3])
end
local function save(s,name)
 s:saveAs(folder..'Native/'..name..'.aseprite');s:saveAs(folder..'Native/'..name..'.png');s:close()
end
local function object(name,w,h,palette)
 local src=app.open(folder..'Raw/'..name..'.png');local im=src.cels[1].image
 local minx,miny,maxx,maxy=im.width,im.height,0,0
 for it in im:pixels()do if pc.rgbaA(it())>=224 then minx=math.min(minx,it.x);miny=math.min(miny,it.y);maxx=math.max(maxx,it.x);maxy=math.max(maxy,it.y)end end
 local bw,bh=maxx-minx+1,maxy-miny+1;local scale=math.min((w-2)/bw,(h-2)/bh)
 local dw,dh=math.max(1,math.floor(bw*scale)),math.max(1,math.floor(bh*scale))
 local ox,oy=math.floor((w-dw)/2),h-1-dh
 local out=Sprite(w,h,ColorMode.RGB);local dest=out.cels[1].image
 for y=0,dh-1 do for x=0,dw-1 do
  local sx=minx+math.min(bw-1,math.floor((x+.5)*bw/dw));local sy=miny+math.min(bh-1,math.floor((y+.5)*bh/dh));local c=im:getPixel(sx,sy)
  if pc.rgbaA(c)>=128 then dest:drawPixel(ox+x,oy+y,nearest(c,palette))end
 end end
 save(out,name);src:close()
end
object('fallen_log',48,48,wood)
object('transit_spice',16,16,icons)
object('transit_wool',16,16,icons)
local function flood(name)
 local raw=folder..'Raw/'..name..'.png';local f=io.open(raw,'rb');if not f then return end;f:close()
 local src=app.open(raw);local im=src.cels[1].image;local w,h=80,48
 local mask,sample={},{}
 for y=0,h-1 do for x=0,w-1 do local c=im:getPixel(math.min(im.width-1,math.floor((x+.5)*im.width/w)),math.min(im.height-1,math.floor((y+.5)*im.height/h)));local i=y*w+x+1;sample[i]=c;mask[i]=pc.rgbaA(c)>=128 or x>=64 end end
 if name=='river_flood_low' then
  local high=app.open(folder..'Native/river_flood_high.png');local hi=high.cels[1].image
  for y=0,h-1 do for x=0,w-1 do local i=y*w+x+1;local c=sample[i];if pc.rgbaR(c)>pc.rgbaG(c)*1.15 or pc.rgbaA(hi:getPixel(x,y))==0 then mask[i]=false end;if x>=64 then mask[i]=true end end end
  high:close()
 end
 -- Remove isolated transparent holes left by generation; retain outside transparency.
 local outside,queue={},{}
 local function push(x,y)if x<0 or y<0 or x>=w or y>=h then return end;local i=y*w+x+1;if not mask[i]and not outside[i]then outside[i]=true;queue[#queue+1]={x,y}end end
 for x=0,w-1 do push(x,0);push(x,h-1)end;for y=0,h-1 do push(0,y);push(w-1,y)end
 local n=1;while n<=#queue do local p=queue[n];n=n+1;push(p[1]-1,p[2]);push(p[1]+1,p[2]);push(p[1],p[2]-1);push(p[1],p[2]+1)end
 for i=1,w*h do if not mask[i]and not outside[i]then mask[i]=true end end
 local out=Sprite(w,h,ColorMode.RGB);local dest=out.cels[1].image
 for y=0,h-1 do for x=0,w-1 do local i=y*w+x+1;if mask[i]then
  local edge=x<w-1 and ((x>0 and not mask[i-1])or(y>0 and not mask[i-w])or(y<h-1 and not mask[i+w]))
  dest:drawPixel(x,y,edge and color(88,143,140)or color(127,185,175))
 end end end
 -- Keep only compact horizontal highlights from the generated source, not gradients.
 for y=1,h-1 do local x=0;while x<w do local start=x;local function bright(xx)local c=sample[y*w+xx+1];return mask[y*w+xx+1]and pc.rgbaR(c)>145 and pc.rgbaG(c)>195 and pc.rgbaB(c)>185 end
  if bright(x)then while x<w and bright(x)do x=x+1 end;local count=x-start;if count>=2 and count<=8 then for xx=start,x-1 do dest:drawPixel(xx,y,color(172,214,202))end end else x=x+1 end
 end end
 save(out,name);src:close()
end
flood('river_flood_high');flood('river_flood_low')
