-- Offline whole-object preparation. Match approved stall scale; never edit production art.
local root=app.params.repo;local out=root..'/ArtReview/SpecialtyMarkets-Task5-20261104/'
local pc=app.pixelColor;local palette,seen={},{}
for _,n in ipairs({'grain_stall','wares_counter'})do
 local s=app.open(root..'/Assets/PocketCaravan/Art/Goryeo/Markets/'..n..'.png')
 for p in s.cels[1].image:pixels()do local c=p();if pc.rgbaA(c)==255 and not seen[c]then palette[#palette+1]=c;seen[c]=true end end;s:close()
end
for _,v in ipairs({{48,56,61},{105,130,135},{182,201,194},{235,236,209},{243,238,216},{49,52,90},{77,80,136},{109,117,169},{39,83,75},{59,118,101},{100,155,124},{128,50,37},{178,72,45},{211,116,65}})do palette[#palette+1]=pc.rgba(v[1],v[2],v[3],255)end
local function nearest(c)
 local best,dist=nil,1e20
 for _,v in ipairs(palette)do local d=(pc.rgbaR(c)-pc.rgbaR(v))^2+(pc.rgbaG(c)-pc.rgbaG(v))^2+(pc.rgbaB(c)-pc.rgbaB(v))^2;if d<dist then best=v;dist=d end end;return best
end
for _,v in ipairs({{'fish_rack',32,25,25},{'salt_stall',48,39,44},{'cloth_stall',48,39,44},{'spice_stall',32,20,25}})do
 local name,h,maxh,bottom=v[1],v[2],v[3],v[4]
 local s=app.open(out..'Raw/'..name..'.png');local im=s.cels[1].image;local x0,y0,x1,y1=im.width,im.height,0,0
 for p in im:pixels()do if pc.rgbaA(p())>=224 then x0=math.min(x0,p.x);y0=math.min(y0,p.y);x1=math.max(x1,p.x);y1=math.max(y1,p.y)end end
 local bw,bh=x1-x0+1,y1-y0+1;local scale=math.min(40/bw,maxh/bh);local dw,dh=math.max(1,math.floor(bw*scale)),math.max(1,math.floor(bh*scale))
 local dest=Sprite(48,h,ColorMode.RGB);local di=dest.cels[1].image;local ox,oy=math.floor((48-dw)/2),bottom-dh
 for y=0,dh-1 do for x=0,dw-1 do local c=im:getPixel(x0+math.min(bw-1,math.floor((x+.5)*bw/dw)),y0+math.min(bh-1,math.floor((y+.5)*bh/dh)));if pc.rgbaA(c)>=128 then di:drawPixel(ox+x,oy+y,nearest(c))end end end
 dest:saveAs(out..'Native/'..name..'.aseprite');dest:saveAs(out..'Native/'..name..'.png');dest:close();s:close()
end
