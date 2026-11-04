-- Native whole-object trial, no production definition/collision change.
local root=app.params.repo;local folder=root..'/ArtReview/LongLog-v2-20261104/';local pc=app.pixelColor
local old=app.open(root..'/ArtReview/E3-E2-Art-20261103/Native/fallen_log.png');local palette,seen={},{}
for p in old.cels[1].image:pixels()do local c=p();if pc.rgbaA(c)==255 and not seen[c]then palette[#palette+1]=c;seen[c]=true end end;old:close()
local function nearest(c)local best,dist=nil,1e20;for _,v in ipairs(palette)do local d=(pc.rgbaR(c)-pc.rgbaR(v))^2+(pc.rgbaG(c)-pc.rgbaG(v))^2+(pc.rgbaB(c)-pc.rgbaB(v))^2;if d<dist then best=v;dist=d end end;return best end
local src=app.open(folder..'Raw/fallen_log.png');local im=src.cels[1].image;local x0,y0,x1,y1=im.width,im.height,0,0
for p in im:pixels()do if pc.rgbaA(p())>=224 then x0=math.min(x0,p.x);y0=math.min(y0,p.y);x1=math.max(x1,p.x);y1=math.max(y1,p.y)end end
local bw,bh=x1-x0+1,y1-y0+1;local scale=math.min(62/bw,25/bh);local w,h=math.floor(bw*scale),math.floor(bh*scale);local ox,oy=math.floor((64-w)/2),47-h
local dest=Sprite(64,48,ColorMode.RGB);local di=dest.cels[1].image
for y=0,h-1 do for x=0,w-1 do local c=im:getPixel(x0+math.min(bw-1,math.floor((x+.5)*bw/w)),y0+math.min(bh-1,math.floor((y+.5)*bh/h)));if pc.rgbaA(c)>=128 then di:drawPixel(ox+x,oy+y,nearest(c))end end end
dest:saveAs(folder..'Native/fallen_log.aseprite');dest:saveAs(folder..'Native/fallen_log.png');dest:close();src:close()
